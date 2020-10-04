//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
//
//
//********************************************************

//********************************************************
// *** INCLUDES
//********************************************************
#include <Arduino.h>

// Custom
#include "Config.h"
#include "Display.h"
#include "Communications.h"

// Third-party
#include "src/Adafruit_GFX/Adafruit_GFX.h"
#include "src/Adafruit_NeoPixel/Adafruit_NeoPixel.h"
#include "src/Adafruit_SSD1306/Adafruit_SSD1306.h"
#include "src/ButtonEvents/ButtonEvents.h"
#include "src/Rotary/Rotary.h"
#include "src/TimerOne/TimerOne.h"

//********************************************************
// *** VARIABLES
//*******************************************************
// State
DeviceSettings g_Settings;
SessionInfo g_SessionInfo;
SessionData g_Sessions[SessionIndex::INDEX_MAX];
uint8_t g_ModeState[DisplayMode::MODE_MAX];
uint8_t g_DisplayDirty;
bool g_DisplayAsleep;

// Encoder Button
ButtonEvents g_EncoderButton;
volatile ButtonEvent g_ButtonEvent;

// Rotary Encoder
Rotary g_Encoder(PIN_ENCODER_OUTB, PIN_ENCODER_OUTA);
int8_t g_PreviousSteps;
volatile int8_t g_EncoderSteps;

// Time & Sleep
uint32_t g_Now;
uint32_t g_LastMessage;
uint32_t g_LastActivity;
uint32_t g_LastPixelUpdate;
uint32_t g_LastSteps;

// Lighting
Adafruit_NeoPixel g_Pixels(PIXELS_COUNT, PIN_PIXELS, NEO_GRB + NEO_KHZ800);

//********************************************************
// *** INTERRUPTS
//********************************************************
void timerIsr()
{
    uint8_t encoderDir = g_Encoder.process();
    if (encoderDir == DIR_CW)
        g_EncoderSteps++;
    else if (encoderDir == DIR_CCW)
        g_EncoderSteps--;

    if (g_ButtonEvent == none && g_EncoderButton.update())
    {
        g_ButtonEvent = g_EncoderButton.event();
    }
}

//********************************************************
// *** MAIN
//********************************************************
//---------------------------------------------------------
//---------------------------------------------------------
void setup()
{
    ResetState();

    // --- Comms
    Communications::Initialize();

    //--- Pixels
    g_Pixels.setBrightness(PIXELS_BRIGHTNESS);
    g_Pixels.begin();
    g_Pixels.show();

    // --- Display
    Display::Initialize();
    Display::SplashScreen();

    // --- Encoder
    pinMode(PIN_ENCODER_SWITCH, INPUT_PULLUP);
    g_EncoderButton.attach(PIN_ENCODER_SWITCH);
    g_EncoderButton.debounceTime(15);
    g_Encoder.begin(true);
    Timer1.initialize(1000);
    Timer1.attachInterrupt(timerIsr);
}

//---------------------------------------------------------
//---------------------------------------------------------
void loop()
{
    uint32_t last = g_Now;
    g_Now = millis();

    Command command = Communications::Read();
    // Returns the type of message we recieved, update oled if we recieved data that impacts what is currently on display
    // This should really depend on a few things, like setings of continious scroll, vs new item index vs count, etc.
    // for now lets be safe and check for any command that impacts a stored value, we can fine tune this later
    g_DisplayDirty = (command >= Command::SETTINGS || command <= Command::VOLUME_ALT_CHANGE);

    if (ProcessEncoderRotation())
    {
        g_LastActivity = g_Now;
        g_DisplayDirty = true;
    }

    if (ProcessEncoderButton())
    {
        g_LastActivity = g_Now;
        g_DisplayDirty = true;
    }

    if (ProcessSleep())
    {
        g_DisplayDirty = true;
    }

    if (g_DisplayDirty || ProcessDisplayScroll())
    {
        UpdateDisplay();
    }

    Display::UpdateTimers(g_Now - last);
    g_DisplayDirty = false;

    // Update Lighting at 30Hz
    if (g_Now >= (g_LastPixelUpdate + 33))
    {
        g_LastPixelUpdate = g_Now;
        UpdateLighting();
    }

    // Reset / Disconnect if no serial activity.
    if ((g_SessionInfo.mode != DisplayMode::MODE_SPLASH) && (g_LastMessage + DEVICE_RESET_AFTER_INACTIVTY < g_Now))
        ResetState();
}

//---------------------------------------------------------
//---------------------------------------------------------
void ResetState()
{
    // State
    g_Settings = DeviceSettings();
    g_SessionInfo = SessionInfo();
    g_Sessions[SessionIndex::INDEX_PREVIOUS] = SessionData();
    g_Sessions[SessionIndex::INDEX_CURRENT] = SessionData();
    g_Sessions[SessionIndex::INDEX_ALTERNATE] = SessionData();
    g_Sessions[SessionIndex::INDEX_NEXT] = SessionData();
    g_ModeState[DisplayMode::MODE_SPLASH] = STATE_LOGO;
    g_ModeState[DisplayMode::MODE_OUTPUT] = STATE_EDIT;
    g_ModeState[DisplayMode::MODE_INPUT] = STATE_EDIT;
    g_ModeState[DisplayMode::MODE_APPLICATION] = STATE_NAVIGATE;
    g_ModeState[DisplayMode::MODE_GAME] = STATE_SELECT_A;
    g_DisplayDirty = true;
    g_DisplayAsleep = false;

    // Encoder Button
    g_ButtonEvent = ButtonEvent::none;

    // Rotary Encoder
    g_PreviousSteps = 0;
    g_EncoderSteps = 0;

    // Time & Sleep
    g_Now = millis();
    g_LastSteps = 0;
    g_LastMessage = 0;
    g_LastActivity = g_Now;
    g_LastPixelUpdate = 0;
}

//---------------------------------------------------------
// \brief Encoder acceleration algorithm (Exponential - speed squared)
// \param encoderDelta - step difference since last check
// \param deltaTime - time difference since last check (ms)
// \param volume - curent volume
// \returns new adjusted volume
//---------------------------------------------------------
int8_t ComputeAcceleratedVolume(int8_t encoderDelta, uint32_t deltaTime, int16_t volume)
{
    if (encoderDelta == 0)
        return volume;

    // Test the top bit (negative bit) for direction changed
    bool dirChanged = (g_PreviousSteps & 0x80) != (encoderDelta & 0x80);

    uint32_t step = 1;
    if (!dirChanged)
    {
        // Compute acceleration using fixed point maths.
        SQ15x16 speed = (SQ15x16)encoderDelta * 1000 / deltaTime;
        SQ15x16 accelerationDivisor = max((1 - (SQ15x16)g_Settings.accelerationPercentage / 100) * ROTARY_ACCELERATION_DIVISOR_MAX, 1);
        SQ15x16 fstep = 1 + absFixed(speed * speed / accelerationDivisor);
        step = fstep.getInteger();
    }

    g_PreviousSteps = encoderDelta;

    if (encoderDelta > 0)
        volume += step;
    else
        volume -= step;

    return constrain(volume, 0, 100);
}

void PreviousSession(void)
{
    if (!CanScrollLeft())
        return;

    if (g_SessionInfo.current == 0)
        g_SessionInfo.current = g_SessionInfo.sessions[GetIndexForMode(g_SessionInfo.mode)];

    g_SessionInfo.current--;
    g_Sessions[SessionIndex::INDEX_NEXT] = g_Sessions[SessionIndex::INDEX_CURRENT];
    g_Sessions[SessionIndex::INDEX_CURRENT] = g_Sessions[SessionIndex::INDEX_PREVIOUS];
    Communications::Write(Command::SESSION_INFO);
}

void NextSession(void)
{
    if (!CanScrollRight())
        return;

    g_SessionInfo.current = (g_SessionInfo.current + 1) % g_SessionInfo.sessions[GetIndexForMode(g_SessionInfo.mode)];
    g_Sessions[SessionIndex::INDEX_PREVIOUS] = g_Sessions[SessionIndex::INDEX_CURRENT];
    g_Sessions[SessionIndex::INDEX_CURRENT] = g_Sessions[SessionIndex::INDEX_NEXT];
    Communications::Write(Command::SESSION_INFO);
}

inline bool CanScrollLeft(void)
{
    if (!g_Settings.continuousScroll && g_SessionInfo.current == 0)
        return false;
    return true;
}

inline bool CanScrollRight(void)
{
    if (!g_Settings.continuousScroll && g_SessionInfo.current < g_SessionInfo.sessions[GetIndexForMode(g_SessionInfo.mode)] - 1)
        return false;
    return true;
}

inline uint8_t GetIndexForMode(DisplayMode mode)
{
    if (mode == DisplayMode::MODE_SPLASH)
        return DisplayMode::MODE_OUTPUT;
    if (mode == DisplayMode::MODE_GAME)
        return DisplayMode::MODE_APPLICATION;
    return mode - DisplayMode::MODE_OUTPUT;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderRotation()
{
    int8_t encoderSteps = 0;
    cli();
    encoderSteps = g_EncoderSteps;
    g_EncoderSteps = 0;
    sei();

    if (encoderSteps == 0)
        return false;

    uint32_t deltaTime = g_Now - g_LastSteps;
    g_LastSteps = g_Now;

    if (g_DisplayAsleep || g_SessionInfo.mode == DisplayMode::MODE_SPLASH)
        return true;

    if (g_ModeState[g_SessionInfo.mode] == STATE_EDIT)
    {
        if (g_SessionInfo.mode != DisplayMode::MODE_GAME)
        {
            g_Sessions[SessionIndex::INDEX_CURRENT].data.volume = ComputeAcceleratedVolume(encoderSteps, deltaTime, g_Sessions[SessionIndex::INDEX_CURRENT].data.volume);
            Communications::Write(Command::VOLUME_CURR_CHANGE);
        }
        else
        {
            // NOTES: Game mode works by selecting 2 sessions, to make things simpler for all "NAVIGATE" logic, CURRENT_SESSION/INDEX_CURRENT sould always be what we work with
            // and when we "select" a session for A, we copy it into ALTERNATE_SESSION/INDEX_ALTERNATE. We could simplify this logic by swapping INDEX_CURRENT & INDEX_ALTERNATE after B is selected,
            // but that just makes for a very messy logic for the App to keep PREVIOUS/NEXT/CURRENT logic in order. So lets just reverse it here so A = INDEX_ALTERNATE, B = INDEX_CURRENT
            g_Sessions[SessionIndex::INDEX_ALTERNATE].data.volume = ComputeAcceleratedVolume(encoderSteps, deltaTime, g_Sessions[SessionIndex::INDEX_ALTERNATE].data.volume);
            Communications::Write(Command::VOLUME_ALT_CHANGE);

            if (g_Sessions[SessionIndex::INDEX_ALTERNATE].data.id != g_Sessions[SessionIndex::INDEX_CURRENT].data.id)
            {
                g_Sessions[SessionIndex::INDEX_CURRENT].data.volume = 100 - g_Sessions[SessionIndex::INDEX_ALTERNATE].data.volume;
                Communications::Write(Command::VOLUME_CURR_CHANGE);
            }
        }
    }
    else
    {
        if (encoderSteps > 0)
            NextSession();
        else
            PreviousSession();
        Display::ResetTimers();
    }

    return true;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderButton()
{
    ButtonEvent readButtonEvent = none;
    cli();
    readButtonEvent = g_ButtonEvent;
    g_ButtonEvent = none;
    sei();

    if (readButtonEvent == ButtonEvent::tap)
    {
        if (g_DisplayAsleep)
            return true;

        g_ModeState[g_SessionInfo.mode] = (g_ModeState[g_SessionInfo.mode] + 1) % (g_SessionInfo.mode != DisplayMode::MODE_GAME ? 2 : 3);

        if (g_SessionInfo.mode == DisplayMode::MODE_INPUT || g_SessionInfo.mode == DisplayMode::MODE_OUTPUT)
        {
            for (uint8_t i = 0; i < SessionIndex::INDEX_MAX; i++)
                g_Sessions[i].data.isDefault = false;
            g_Sessions[SessionIndex::INDEX_CURRENT].data.isDefault = true;
            Communications::Write(Command::VOLUME_CURR_CHANGE);
        }
        else if (g_SessionInfo.mode == DisplayMode::MODE_GAME && g_ModeState[g_SessionInfo.mode] == STATE_SELECT_B)
        {
            g_Sessions[SessionIndex::INDEX_ALTERNATE] = g_Sessions[SessionIndex::INDEX_CURRENT];
        }

        Display::ResetTimers();
        return true;
    }
    else if (readButtonEvent == ButtonEvent::doubleTap)
    {
        if (g_SessionInfo.mode == DisplayMode::MODE_SPLASH)
            return false;

        if (g_SessionInfo.mode != DisplayMode::MODE_GAME)
        {
            g_Sessions[SessionIndex::INDEX_CURRENT].data.isMuted = true;
            Communications::Write(Command::VOLUME_CURR_CHANGE);
        }
        else
        {
            g_Sessions[SessionIndex::INDEX_CURRENT].data.volume = 50;
            g_Sessions[SessionIndex::INDEX_ALTERNATE].data.volume = 50;
            Communications::Write(Command::VOLUME_CURR_CHANGE);
        }
        return true;
    }
    else if (readButtonEvent == ButtonEvent::hold)
    {
        if (g_DisplayAsleep)
            return true;

        if (g_SessionInfo.mode == DisplayMode::MODE_SPLASH)
            return false;

        // TODO: So this is tricky as we need to wait for data from the pc at this point. Need a temp waiting for data screen or something
        g_SessionInfo.mode = (DisplayMode)((g_SessionInfo.mode + 1) % DisplayMode::MODE_MAX);
        if (g_SessionInfo.mode == DisplayMode::MODE_SPLASH)
            g_SessionInfo.mode = (DisplayMode)(g_SessionInfo.mode + 1);
        g_SessionInfo.current = 0;
        // TODO: Also need to handle 0 data from PC for this mode

        Communications::Write(Command::SESSION_INFO);
        Display::ResetTimers();
        return true;
    }

    return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessSleep()
{
    if (g_Settings.sleepAfterSeconds == 0)
        return false;

    bool lastState = g_DisplayAsleep;
    uint32_t activityTimeDelta = g_Now - g_LastActivity;
    if (activityTimeDelta > g_Settings.sleepAfterSeconds * 1000)
        g_DisplayAsleep = true;
    else if (activityTimeDelta < g_Settings.sleepAfterSeconds * 1000)
        g_DisplayAsleep = false;

    return lastState != g_DisplayAsleep;
}

bool ProcessDisplayScroll()
{
    if (g_SessionInfo.mode != DisplayMode::MODE_GAME)
    {
        return strlen(g_Sessions[SessionIndex::INDEX_CURRENT].name) > DISPLAY_CHAR_MAX_X2;
    }
    else
    {
        if (g_ModeState[g_SessionInfo.mode] == STATE_EDIT)
            return strlen(g_Sessions[SessionIndex::INDEX_CURRENT].name) > DISPLAY_GAME_EDIT_CHAR_MAX;
        return strlen(g_Sessions[SessionIndex::INDEX_CURRENT].name) > DISPLAY_CHAR_MAX_X2;
    }
    return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateDisplay()
{
    if (g_DisplayAsleep)
    {
        Display::Sleep();
        return;
    }

    if (g_SessionInfo.mode == DisplayMode::MODE_SPLASH)
    {
        if (g_ModeState[g_SessionInfo.mode] == STATE_LOGO)
            Display::SplashScreen();
        else
            Display::InfoScreen();
    }
    else if (g_SessionInfo.mode == DisplayMode::MODE_INPUT || g_SessionInfo.mode == DisplayMode::MODE_OUTPUT)
    {
        if (g_ModeState[g_SessionInfo.mode] == STATE_NAVIGATE)
        {
            Display::DeviceSelectScreen(&g_Sessions[SessionIndex::INDEX_CURRENT], CanScrollLeft(), CanScrollRight(), g_SessionInfo.mode);
        }
        else
        {
            Display::DeviceEditScreen(&g_Sessions[SessionIndex::INDEX_CURRENT], g_SessionInfo.mode == DisplayMode::MODE_INPUT ? "IN" : "OUT", g_SessionInfo.mode);
        }
    }
    else if (g_SessionInfo.mode == DisplayMode::MODE_APPLICATION)
    {
        if (g_ModeState[g_SessionInfo.mode] == STATE_NAVIGATE)
        {
            Display::ApplicationSelectScreen(&g_Sessions[SessionIndex::INDEX_CURRENT], CanScrollLeft(), CanScrollRight(), g_SessionInfo.mode);
        }
        else
        {
            Display::ApplicationEditScreen(&g_Sessions[SessionIndex::INDEX_CURRENT], g_SessionInfo.mode);
        }
    }
    else if (g_SessionInfo.mode == DisplayMode::MODE_GAME)
    {
        if (g_ModeState[g_SessionInfo.mode] != STATE_EDIT)
        {
            Display::GameSelectScreen(&g_Sessions[SessionIndex::INDEX_CURRENT], g_ModeState[g_SessionInfo.mode] == STATE_SELECT_A ? 'A' : 'B', CanScrollLeft(), CanScrollRight(), g_SessionInfo.mode);
        }
        else
        {
            Display::ApplicationEditScreen(&g_Sessions[SessionIndex::INDEX_CURRENT], g_SessionInfo.mode);
        }
    }
}
