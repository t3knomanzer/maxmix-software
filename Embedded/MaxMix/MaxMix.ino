//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
// 
//
//
// The AVR 328P chip has 2kB of SRAM. 
// Program uses 825 bytes, leaving 1223 bytes for items. Each item is 42 bytes.
// Therefore we can store a maximum (1223 / 42 bytes) of 29 items.
// We currently this to 8 maximum items for safety.
//********************************************************


//********************************************************
// *** INCLUDES
//********************************************************
#include <Arduino.h>

// Custom
#include "Config.h"
#include "Structs.h"
#include "Display.h"

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
// Serial Communication
uint8_t receiveIndex = 0;
uint8_t sendIndex = 0;
uint8_t receiveBuffer[RECEIVE_BUFFER_SIZE];
uint8_t decodeBuffer[DECODE_BUFFER_SIZE];
uint8_t sendBuffer[SEND_BUFFER_SIZE];
uint8_t encodeBuffer[ENCODE_BUFFER_SIZE];

// State
uint8_t mode = MODE_SPLASH;
uint8_t stateSplash = STATE_SPLASH_LOGO;
uint8_t stateOutput = STATE_OUTPUT_EDIT;
uint8_t stateInput = STATE_INPUT_EDIT;
uint8_t stateApplication = STATE_APPLICATION_NAVIGATE;
uint8_t stateGame = STATE_GAME_SELECT_A;
uint8_t stateDisplay = STATE_DISPLAY_AWAKE;
uint8_t isDirty = true;

// Items
Item devicesOutput[DEVICE_OUTPUT_MAX_COUNT];
Item devicesInput[DEVICE_INPUT_MAX_COUNT];
Item sessions[SESSION_MAX_COUNT];
uint8_t devicesOutputCount = 0;
uint8_t devicesInputCount = 0;
uint8_t sessionCount = 0;

int8_t itemIndexOutput = 0;
int8_t itemIndexInput = 0;
int8_t itemIndexApp = 0;
int8_t itemIndexGameA = 0;
int8_t itemIndexGameB = 0;

uint32_t defaultOutputEndpointId;
uint32_t defaultInputEndpointId;

// Settings
Settings settings;

// Encoder Button
ButtonEvents encoderButton;
volatile ButtonEvent buttonEvent = none;

// Rotary Encoder
Rotary encoderRotary(PIN_ENCODER_OUTB, PIN_ENCODER_OUTA);
uint32_t encoderLastTransition = 0;
int8_t prevDir = 0;
volatile int8_t steps = 0;

// Time & Sleep
uint32_t now = 0;
uint32_t last = 0;
uint32_t lastActivityTime = 0;
uint32_t lastLightingUpdate = 0;

// Lighting
Adafruit_NeoPixel pixels(PIXELS_COUNT, PIN_PIXELS, NEO_GRB + NEO_KHZ800);

//********************************************************
// *** INTERRUPTS
//********************************************************
void timerIsr()
{
  uint8_t encoderDir = encoderRotary.process();
  if(encoderDir == DIR_CW)
    steps++;
  else if(encoderDir == DIR_CCW)
    steps--;

  if(buttonEvent == none && encoderButton.update())
  {
    buttonEvent = encoderButton.event();
  }
}

//********************************************************
// *** MAIN
//********************************************************
//---------------------------------------------------------
//---------------------------------------------------------
void setup()
{
  // --- Comms
  Serial.begin(BAUD_RATE);

  //--- Pixels
  pixels.setBrightness(PIXELS_BRIGHTNESS);
  pixels.begin();
  pixels.show();

  // --- Display
  Display::Initialize();
  Display::SplashScreen();

  // --- Encoder
  pinMode(PIN_ENCODER_SWITCH, INPUT_PULLUP);
  encoderButton.attach(PIN_ENCODER_SWITCH);
  encoderButton.debounceTime(15);
  encoderRotary.begin(true);
  Timer1.initialize(1000);
  Timer1.attachInterrupt(timerIsr);
}

//---------------------------------------------------------
//---------------------------------------------------------
void loop()
{
  last = now;
  now = millis();

  if(ReceiveData(receiveBuffer, &receiveIndex, MSG_PACKET_DELIMITER, RECEIVE_BUFFER_SIZE))
  {
    if(DecodePackage(receiveBuffer, receiveIndex, decodeBuffer))
    {
      uint8_t revision = GetRevisionFromPackage(decodeBuffer);
      SendAcknowledgment(sendBuffer, encodeBuffer, revision);

      if(ProcessPackage())
      {
        UpdateActivityTime();
        isDirty = true;
      }
    }      
    ClearReceive();
  }

  if(ProcessEncoderRotation() || ProcessEncoderButton())
  {
      UpdateActivityTime();
      isDirty = true;
  }

  if(ProcessSleep())
    isDirty = true;

  // Check for buffer overflow
  if(receiveIndex == RECEIVE_BUFFER_SIZE)
    ClearReceive();

  ClearSend();
  encoderButton.update();

  if(isDirty || ProcessDisplayScroll())
  {
    UpdateDisplay();
  }

  // Update Lighting at 30Hz
  if(now >= (lastLightingUpdate + 33))
  {
    lastLightingUpdate = now;
    UpdateLighting();
  }

  Display::UpdateTimers(now - last);
  isDirty = false;
}

//********************************************************
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
//---------------------------------------------------------
void ClearReceive()
{
  receiveIndex = 0;  
}

//---------------------------------------------------------
//---------------------------------------------------------
void ClearSend()
{
  sendIndex = 0;
}

//---------------------------------------------------------
//---------------------------------------------------------
void ResetState()
{
  mode = MODE_SPLASH;
  stateSplash = STATE_SPLASH_LOGO;
  stateOutput = STATE_OUTPUT_EDIT;
  stateInput = STATE_INPUT_EDIT;
  stateApplication = STATE_APPLICATION_NAVIGATE;
  stateGame = STATE_GAME_SELECT_A;
  stateDisplay = STATE_DISPLAY_AWAKE;

  itemIndexOutput = 0;
  itemIndexApp = 0;
  itemIndexGameA = 0;
  itemIndexGameB = 0;
  sessionCount = 0;
  devicesOutputCount = 0;
}


//---------------------------------------------------------
// \brief Handles incoming commands.
// \returns true if screen update is required.
//---------------------------------------------------------
bool ProcessPackage()
{
  uint8_t command = GetCommandFromPackage(decodeBuffer);  
  if(command == MSG_COMMAND_HANDSHAKE_REQUEST)
  {
    ResetState();
    return true;
  }
  else if(command == MSG_COMMAND_ADD)
  {
    
    if(mode == MODE_SPLASH)
    {
      mode = MODE_OUTPUT;
    }

    uint32_t id = GetIdFromPackage(decodeBuffer);
    bool isDevice = GetIsDeviceFromAddPackage(decodeBuffer); 

    int8_t index;  
    if(isDevice)
    {
      uint8_t deviceFlow = GetDeviceFlowFromAddPackage(decodeBuffer);
      
      Item* buffer;
      uint8_t* count;
      if(deviceFlow == DEVICE_FLOW_INPUT)
      {
        if(devicesInputCount == DEVICE_INPUT_MAX_COUNT)
          return false;
        buffer = devicesInput;
        count = &devicesInputCount;
      }
      else
      {
        if(devicesOutputCount == DEVICE_OUTPUT_MAX_COUNT)
          return false;
        buffer = devicesOutput;
        count = &devicesOutputCount;
      }      

      index = FindItem(id, buffer, *count);
      if(index == -1)
      {
        AddItemCommand(decodeBuffer, buffer, count);
        index = *count - 1;
      }
      else
        UpdateItemCommand(decodeBuffer, buffer, index);
    }
    else
    {
      if(sessionCount == SESSION_MAX_COUNT)
        return false;

      index = FindItem(id, sessions, sessionCount);
      if(index == -1)
      {
        AddItemCommand(decodeBuffer, sessions, &sessionCount);
        index = sessionCount - 1;
      }
      else
        UpdateItemCommand(decodeBuffer, sessions, index);

      if(settings.displayNewItem)
      {
        itemIndexApp = index;
        if(mode == MODE_APPLICATION)
          stateApplication = STATE_APPLICATION_NAVIGATE;

        return true;
      }
    }    
  }
  else if(command == MSG_COMMAND_REMOVE)
  {
    uint32_t id = GetIdFromPackage(decodeBuffer);
    bool isDevice = GetIsDeviceFromRemovePackage(decodeBuffer);

    int8_t index;
    if(isDevice)
    {
      uint8_t deviceFlow = GetDeviceFlowFromRemovePackage(decodeBuffer);

      Item* buffer;
      uint8_t* count;
      int8_t* modeIndex;

      if(deviceFlow == DEVICE_FLOW_INPUT)
      {
        buffer = devicesInput;
        count = &devicesInputCount;
        modeIndex = &itemIndexInput;
      }
      else
      {
        buffer = devicesOutput;
        count = &devicesOutputCount;
        modeIndex = &itemIndexOutput;
      }

      index = FindItem(id, buffer, *count);
      if(index == -1)
        return false;

      RemoveItemCommand(decodeBuffer, buffer, count, index);
      
      bool isItemActive = IsItemActive(index);
      *modeIndex = GetNextIndex(*modeIndex, *count, 0, settings.continuousScroll);
      
      if(*count == 0)
      {
        CycleMode();
        return true;
      }

      if(isItemActive)
      {
        return true;    
      }
    }
    else
    {
      if(sessionCount == 0)
        return false;

      index = FindItem(id, sessions, sessionCount);
      if(index == -1)
        return false;

      RemoveItemCommand(decodeBuffer, sessions, &sessionCount, index);
      
      bool isItemActive = IsItemActive(index);
      itemIndexApp = GetNextIndex(itemIndexApp, sessionCount, 0, settings.continuousScroll);
      if(isItemActive)
      {
        if(mode == MODE_APPLICATION)
          stateApplication = STATE_APPLICATION_NAVIGATE;
        return true;      
      }    
    }
  }
  else if(command == MSG_COMMAND_UPDATE_VOLUME)
  {
    uint32_t id = GetIdFromPackage(decodeBuffer);
    bool isDevice = GetIsDeviceFromUpdatePackage(decodeBuffer);

    int8_t index;
    if(isDevice)
    {
      uint8_t deviceFlow = GetDeviceFlowFromUpdatePackage(decodeBuffer);
      
      Item* buffer;
      uint8_t* count;
      if(deviceFlow == 0)
      {
        buffer = devicesInput;
        count = &devicesInputCount;
      }
      else
      {
        buffer = devicesOutput;
        count = &devicesOutputCount;
      }      

      index = FindItem(id, buffer, *count);
      if(index == -1)
        return false;

      UpdateItemVolumeCommand(decodeBuffer, buffer, index);

      if(IsItemActive(index))
        return true;

      return false;
    }
    else
    {
      index = FindItem(id, sessions, sessionCount);
      if(index == -1)
        return false;

      UpdateItemVolumeCommand(decodeBuffer, sessions, index);

      if(IsItemActive(index))
      {
        if(mode == MODE_GAME)
        {
          if(index == itemIndexGameA && index != itemIndexGameB)
            RebalanceGameVolume(sessions[itemIndexGameA].volume, itemIndexGameB);

          if(index == itemIndexGameB && index != itemIndexGameA)
            RebalanceGameVolume(sessions[itemIndexGameB].volume, itemIndexGameA);
        }                
        return true;
      }
      return false;
    }    
  }
  else if(command == MSG_COMMAND_SET_DEFAULT_ENDPOINT)
  {
    uint32_t id = GetIdFromPackage(decodeBuffer);
    
    uint8_t deviceFlow = GetDeviceFlowFromDefaultEndpointPackage(decodeBuffer);

    Item* buffer;
    uint8_t* count;
    int8_t* modeIndex;
    uint32_t* defaultEndpointId;

    if(deviceFlow == 0)
    {
      buffer = devicesInput;
      count = &devicesInputCount;
      modeIndex = &itemIndexInput;
      defaultEndpointId = &defaultInputEndpointId;
    }
    else
    {
      buffer = devicesOutput;
      count = &devicesOutputCount;
      modeIndex = &itemIndexOutput;
      defaultEndpointId = &defaultOutputEndpointId;
    }      

    int8_t index = FindItem(id, buffer, *count);
    if(index == -1)
        return false;

    *modeIndex = index;
    *defaultEndpointId = id;

    if(mode == MODE_OUTPUT || mode == MODE_INPUT)
      return true;
  }
  else if(command == MSG_COMMAND_SETTINGS)
  {
    UpdateSettingsCommand(decodeBuffer, &settings);
    stateDisplay = STATE_DISPLAY_AWAKE;
    return true;
  }

  return false;
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
  if (!encoderDelta)
    return volume;

  bool dirChanged = ((prevDir > 0) && (encoderDelta < 0)) || ((prevDir < 0) && (encoderDelta > 0));

  uint32_t step;
  if (dirChanged)
  {
     step = 1;
  }
  else
  {
    // Compute acceleration using fixed point maths.
    SQ15x16 speed = (SQ15x16)encoderDelta*1000/deltaTime;
    SQ15x16 accelerationDivisor = max((1-(SQ15x16)settings.accelerationPercentage/100)*ROTARY_ACCELERATION_DIVISOR_MAX, 1);
    SQ15x16 fstep = 1 + absFixed(speed*speed/accelerationDivisor);
    step = fstep.getInteger();
  }

  prevDir = encoderDelta;

  if(encoderDelta > 0)
  {
    volume += step;
  }
  else if(encoderDelta < 0)
  {
    volume -= step;
  }
  
  return constrain(volume, 0, 100);
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderRotation()
{
  int8_t encoderDelta;
  cli();
  encoderDelta = steps;
  if(encoderDelta !=0)
    steps = 0;
  sei();
  
  if(encoderDelta == 0)
    return false;

  uint32_t deltaTime = now - encoderLastTransition;
  encoderLastTransition = now;

  if(stateDisplay == STATE_DISPLAY_SLEEP)
    return true;

  if(mode == MODE_OUTPUT)
  {
    if(stateOutput == STATE_OUTPUT_NAVIGATE)
    {
      itemIndexOutput = GetNextIndex(itemIndexOutput, devicesOutputCount, encoderDelta, settings.continuousScroll);
      Display::ResetTimers();
    }

    else if(stateOutput == STATE_OUTPUT_EDIT)
    {
      devicesOutput[itemIndexOutput].volume = ComputeAcceleratedVolume(encoderDelta, deltaTime, devicesOutput[itemIndexOutput].volume);
      SendItemVolumeCommand(&devicesOutput[itemIndexOutput], sendBuffer, encodeBuffer);
    }
  }
  else  if(mode == MODE_INPUT)
  {
    if(stateInput == STATE_INPUT_NAVIGATE)
    {
      itemIndexInput = GetNextIndex(itemIndexInput, devicesInputCount, encoderDelta, settings.continuousScroll);
      Display::ResetTimers();
    }

    else if(stateInput == STATE_INPUT_EDIT)
    {
      devicesInput[itemIndexInput].volume = ComputeAcceleratedVolume(encoderDelta, deltaTime, devicesInput[itemIndexInput].volume);
      SendItemVolumeCommand(&devicesInput[itemIndexInput], sendBuffer, encodeBuffer);
    }
  }
  else if(mode == MODE_APPLICATION)
  {
    if(stateApplication == STATE_APPLICATION_NAVIGATE)
    {
      itemIndexApp = GetNextIndex(itemIndexApp, sessionCount, encoderDelta, settings.continuousScroll);
      Display::ResetTimers();
    }
    else if(stateApplication == STATE_APPLICATION_EDIT)
    {
      sessions[itemIndexApp].volume = ComputeAcceleratedVolume(encoderDelta, deltaTime, sessions[itemIndexApp].volume);
      SendItemVolumeCommand(&sessions[itemIndexApp], sendBuffer, encodeBuffer);
    }
  }
  else if(mode == MODE_GAME)
  {
    if(stateGame == STATE_GAME_SELECT_A)
    {
      itemIndexGameA = GetNextIndex(itemIndexGameA, sessionCount, encoderDelta, settings.continuousScroll);
      Display::ResetTimers();
    }
    else if(stateGame == STATE_GAME_SELECT_B)
    {
      itemIndexGameB = GetNextIndex(itemIndexGameB, sessionCount, encoderDelta, settings.continuousScroll);
      Display::ResetTimers();
    }

    else if(stateGame == STATE_GAME_EDIT)
    {
      sessions[itemIndexGameA].volume = ComputeAcceleratedVolume(encoderDelta, deltaTime, sessions[itemIndexGameA].volume);
      SendItemVolumeCommand(&sessions[itemIndexGameA], sendBuffer, encodeBuffer);

      if(itemIndexGameA != itemIndexGameB)
        RebalanceGameVolume(sessions[itemIndexGameA].volume, itemIndexGameB);
    } 
  }
  
  return true;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderButton()
{
  cli();
  ButtonEvent readButtonEvent = buttonEvent;
  buttonEvent = none;
  sei();
  
  if(readButtonEvent == none)
  {
    return false;
  }
  else if(readButtonEvent == tap)
  {
    if(stateDisplay == STATE_DISPLAY_SLEEP)
    {
      return true;
    }
    else if(mode == MODE_SPLASH)
    {
      stateSplash = CycleState(stateSplash, STATE_SPLASH_COUNT);
      Display::ResetTimers();
    }
    else if(mode == MODE_OUTPUT)
    {
      if(stateOutput == STATE_OUTPUT_NAVIGATE)
        SendSetDefaultEndpointCommand(&devicesOutput[itemIndexOutput], sendBuffer, encodeBuffer);

      stateOutput = CycleState(stateOutput, STATE_OUTPUT_COUNT);
      Display::ResetTimers();
    }
    else if(mode == MODE_INPUT)
    {
      if(stateInput == STATE_INPUT_NAVIGATE)
        SendSetDefaultEndpointCommand(&devicesInput[itemIndexInput], sendBuffer, encodeBuffer);

      stateInput = CycleState(stateInput, STATE_INPUT_COUNT);
      Display::ResetTimers();
    }
    else if(mode == MODE_APPLICATION)
    {
      stateApplication = CycleState(stateApplication, STATE_APPLICATION_COUNT);
      Display::ResetTimers();
    }
    else if(mode == MODE_GAME)
    {
      stateGame = CycleState(stateGame, STATE_GAME_COUNT);
      Display::ResetTimers();
    }

    return true;
  }
  
  else if(readButtonEvent == doubleTap)
  {
    if(mode == MODE_SPLASH)
    {
      return false;
    }
    if(mode == MODE_OUTPUT)
    {
      ToggleMute(devicesOutput, itemIndexOutput);
    }
    else if(mode == MODE_INPUT)
    {
      ToggleMute(devicesInput, itemIndexInput);
    }
    else if(mode == MODE_APPLICATION)
    {
      ToggleMute(sessions, itemIndexApp);
    }
    else if(mode == MODE_GAME && stateGame == STATE_GAME_EDIT)
    {
      ResetGameVolume();
    }

    return true;
  }
  else if(readButtonEvent == hold)
  {
    if(stateDisplay == STATE_DISPLAY_SLEEP)
    {
      return true;
    }

    if(mode == MODE_SPLASH)
    {
      return false;
    }
      
    CycleMode();
    Display::ResetTimers();
    return true;
  }

  return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessSleep()
{
  if(settings.sleepWhenInactive == 0)
    return false;

  uint32_t activityTimeDelta = now - lastActivityTime;

  if(stateDisplay == STATE_DISPLAY_AWAKE)
  {
    if(activityTimeDelta > settings.sleepAfterSeconds * 1000)
    {
      stateDisplay = STATE_DISPLAY_SLEEP;
      return true;
    }
  }
  else if(stateDisplay == STATE_DISPLAY_SLEEP)
  {
    if(activityTimeDelta < settings.sleepAfterSeconds * 1000)
    {
      stateDisplay = STATE_DISPLAY_AWAKE;
      return true;
    }
  }

  return false;
}

bool ProcessDisplayScroll()
{
  bool result = false;

  if(mode == MODE_OUTPUT)
  {
    result = strlen(devicesOutput[itemIndexOutput].name) > DISPLAY_CHAR_MAX_X2;
  }
  if(mode == MODE_INPUT)
  {
    result = strlen(devicesInput[itemIndexInput].name) > DISPLAY_CHAR_MAX_X2;
  }
  else if(mode == MODE_APPLICATION)
  {
    result = strlen(sessions[itemIndexApp].name) > DISPLAY_CHAR_MAX_X2;
  }
  else if(mode == MODE_GAME)
  {
    if(stateGame == STATE_GAME_SELECT_A)
    {
      result = strlen(sessions[itemIndexGameA].name) > DISPLAY_CHAR_MAX_X2;
    }
    else if(stateGame == STATE_GAME_SELECT_B)
    {
      result = strlen(sessions[itemIndexGameB].name) > DISPLAY_CHAR_MAX_X2;
    }
    else if(stateGame == STATE_GAME_EDIT)
    {
      result = strlen(sessions[itemIndexGameA].name) > DISPLAY_GAME_EDIT_CHAR_MAX ||
               strlen(sessions[itemIndexGameB].name) > DISPLAY_GAME_EDIT_CHAR_MAX;
    }
  }

  return result;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateActivityTime()
{
  lastActivityTime = now;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateDisplay()
{
  if(stateDisplay == STATE_DISPLAY_SLEEP)
  {
    Display::Sleep();
    return;
  }

  // TODO: Update to include devices.
  if(mode == MODE_SPLASH)
  {
    if(stateSplash == STATE_SPLASH_LOGO)
    {
      Display::SplashScreen();
    }
    else if(stateSplash == STATE_SPLASH_INFO)
    {
      Display::InfoScreen();
    }
  }  
  else if(mode == MODE_OUTPUT)
  {
    if(stateOutput == STATE_OUTPUT_NAVIGATE)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexOutput, devicesOutputCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexOutput, devicesOutputCount, settings.continuousScroll);
      uint8_t isDefaultEndpoint =  devicesOutput[itemIndexOutput].id == defaultOutputEndpointId;

      Display::DeviceSelectScreen(&devicesOutput[itemIndexOutput], isDefaultEndpoint, scrollLeft, scrollRight, mode);
    }
    else if(stateOutput == STATE_OUTPUT_EDIT)
    {
      Display::DeviceEditScreen(&devicesOutput[itemIndexOutput], "OUT", mode);
    }
  }
  else if(mode == MODE_INPUT)
  {
    if(stateInput == STATE_INPUT_NAVIGATE)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexInput, devicesInputCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexInput, devicesInputCount, settings.continuousScroll);
      uint8_t isDefaultEndpoint =  devicesInput[itemIndexInput].id == defaultInputEndpointId;

      Display::DeviceSelectScreen(&devicesInput[itemIndexInput], isDefaultEndpoint, scrollLeft, scrollRight, mode);
    }
    else if(stateInput == STATE_INPUT_EDIT)
    {
      Display::DeviceEditScreen(&devicesInput[itemIndexInput], "IN", mode);
    }
  }
  else if(mode == MODE_APPLICATION)
  {
    if(stateApplication == STATE_APPLICATION_NAVIGATE)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexApp, sessionCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexApp, sessionCount, settings.continuousScroll);
      Display::ApplicationSelectScreen(&sessions[itemIndexApp], scrollLeft, scrollRight, mode);
    }
    else if(stateApplication == STATE_APPLICATION_EDIT)
      Display::ApplicationEditScreen(&sessions[itemIndexApp], mode);
  }
  else if(mode == MODE_GAME)
  {
    if(stateGame == STATE_GAME_SELECT_A)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexGameA, sessionCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexGameA, sessionCount, settings.continuousScroll);
      Display::GameSelectScreen(&sessions[itemIndexGameA], 'A', scrollLeft, scrollRight, mode);
    }
    else if(stateGame == STATE_GAME_SELECT_B)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexGameB, sessionCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexGameB, sessionCount, settings.continuousScroll);
      Display::GameSelectScreen(&sessions[itemIndexGameB], 'B', scrollLeft, scrollRight, mode);
    }
    else if(stateGame == STATE_GAME_EDIT)
      Display::GameEditScreen(&sessions[itemIndexGameA], &sessions[itemIndexGameB], mode);
  }
}

//---------------------------------------------------------
//---------------------------------------------------------
void CycleMode()
{
  mode++;
  if(mode == MODE_OUTPUT && devicesOutputCount == 0)
  {
    mode++;
  }

  if(mode == MODE_INPUT && devicesInputCount == 0)
  {
    mode++;
  }

  if(mode == MODE_COUNT)
  {
    mode = 0;
  }
}

//---------------------------------------------------------
//---------------------------------------------------------
uint8_t CycleState(uint8_t state, uint8_t count)
{
  state++;
  if(state == count)
    state = 0;

  return state;
}

//---------------------------------------------------------
//---------------------------------------------------------
void ToggleMute(Item* items, int8_t index)
{
  items[index].isMuted = !items[index].isMuted;
  SendItemVolumeCommand(&items[index], sendBuffer, encodeBuffer);
}

//---------------------------------------------------------
//---------------------------------------------------------
void ResetGameVolume()
{
  sessions[itemIndexGameA].volume = 50;
  sessions[itemIndexGameB].volume = 50;

  SendItemVolumeCommand(&sessions[itemIndexGameA], sendBuffer, encodeBuffer);
  SendItemVolumeCommand(&sessions[itemIndexGameB], sendBuffer, encodeBuffer);
}

void RebalanceGameVolume(uint8_t sourceVolume, uint8_t targetIndex)
{
  sessions[targetIndex].volume = 100 - sourceVolume;
  SendItemVolumeCommand(&sessions[targetIndex], sendBuffer, encodeBuffer);
}

//---------------------------------------------------------
// Finds the item with the given id.
// Returns the index of the item if found, -1 otherwise.
//---------------------------------------------------------
int8_t FindItem(uint32_t id, Item* items, uint8_t itemCount)
{  
  for(int8_t i = 0; i < itemCount; i++)
    if(items[i].id == id)
      return i;

  return -1;
}

//---------------------------------------------------------
// \brief Checks if target ID is the active application.
// \param index The index of the item to be checked.
// \returns true if ids match.
//---------------------------------------------------------
bool IsItemActive(int8_t index)
{
  if(mode == MODE_OUTPUT && itemIndexOutput == index)
  {
    return true;
  }
  else if(mode == MODE_INPUT && itemIndexInput == index)
  {
    return true;
  }
  else if(mode == MODE_APPLICATION && itemIndexApp == index)
  {
    return true;
  }
  else if(mode == MODE_GAME && (itemIndexGameA == index || itemIndexGameB == index))
  {
    return true;
  }

  return false;
}
