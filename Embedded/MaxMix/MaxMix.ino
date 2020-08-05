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
// Program uses 819 bytes, leaving 1229 bytes for items.
// Therefore we can store a maximum of 14 items (2048 / 84 bytes)
// We limit this to 12 maximum items for safety.
//********************************************************


//********************************************************
// *** INCLUDES
//********************************************************
#include <Arduino.h>

// Third-party
#include "src/Adafruit_GFX/Adafruit_GFX.h"
#include "src/Adafruit_NeoPixel/Adafruit_NeoPixel.h"
#include "src/Adafruit_SSD1306/Adafruit_SSD1306.h"
#include "src/ButtonEvents/ButtonEvents.h"
#include "src/Rotary/Rotary.h"

// Custom
#include "Config.h"

//********************************************************
// *** STRUCTS
//********************************************************
struct Item
{
  uint32_t id;                          // 4 Bytes (32 bit)
  char name[ITEM_BUFFER_NAME_SIZE];     // 36 Bytes (1 Bytes * 36 Chars)
  int8_t volume;                        // 1 Byte
  uint8_t isMuted;                      // 1 Byte
                                        // 82 Bytes TOTAL
};

struct Settings
{
  uint8_t displayNewSession = 1;
  uint8_t sleepWhenInactive = 1;
  uint8_t sleepAfterSeconds = 5;
  uint8_t continuousScroll = 1;
};

//********************************************************
// *** VARIABLES
//*******************************************************
// Serial Communication
uint8_t receiveIndex = 0;
uint8_t sendIndex = 0;
uint8_t receiveBuffer[RECEIVE_BUFFER_SIZE];
uint8_t decodeBuffer[RECEIVE_BUFFER_SIZE];
uint8_t sendBuffer[SEND_BUFFER_SIZE];
uint8_t encodeBuffer[SEND_BUFFER_SIZE];

// State
uint8_t mode = 0;
uint8_t stateApplication = 0;
uint8_t stateGame = 0;
uint8_t stateScreen = 0;
uint8_t isDirty = true;

struct Item items[ITEM_BUFFER_SIZE];
int8_t itemIndexMaster = 0;
int8_t itemIndexApp = -1;
int8_t itemIndexGameA = 0;
int8_t itemIndexGameB = 0;
uint8_t itemCount = 0;

// Settings
struct Settings settings;

// Rotary Encoder
ButtonEvents encoderButton;
Rotary encoderRotary(PIN_ENCODER_OUTB, PIN_ENCODER_OUTA);
int8_t encoderVolumeStep = 5;

// Sleep
uint32_t lastActivityTime = 0;

// Lighting
Adafruit_NeoPixel* pixels;

// Display
Adafruit_SSD1306* display;

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
  pixels = new Adafruit_NeoPixel(PIXELS_NUM, PIN_PIXELS, NEO_GRB + NEO_KHZ800);
  pixels->begin();

  // --- Display
  display = InitializeDisplay();
  DisplaySplashScreen(display);

  // --- Encoder
  pinMode(PIN_ENCODER_SWITCH, INPUT_PULLUP);
  encoderButton.attach(PIN_ENCODER_SWITCH);
  encoderRotary.begin(true);
}

//---------------------------------------------------------
//---------------------------------------------------------
void loop()
{
  if(ReceivePackage(receiveBuffer, &receiveIndex, MSG_PACKET_DELIMITER, RECEIVE_BUFFER_SIZE))
  {
    if(DecodePackage(receiveBuffer, receiveIndex, decodeBuffer))
    {
      if(ProcessPackage())
        RequireDisplayUpdate();
    }
      
    ClearReceive();
  }

  if(ProcessEncoderRotation() || ProcessEncoderButton())
  {
    RequireDisplayUpdate();
  }

  if(ProcessSleep())
    isDirty = true;

  // Check for buffer overflow
  if(receiveIndex == RECEIVE_BUFFER_SIZE)
    ClearReceive();

  ClearSend();
  encoderButton.update();

  if(isDirty)
  {
    UpdateDisplay();
    UpdateLighting();  
    isDirty = false;
  }  
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
// \brief Handles incoming commands.
// \returns true if screen update is required.
//---------------------------------------------------------
bool ProcessPackage()
{
  uint8_t command = GetCommandFromPackage(decodeBuffer);
  
  if(command == MSG_COMMAND_HS_REQUEST)
  {
    SendHandshakeCommand(sendBuffer, encodeBuffer);
  }
  else if(command == MSG_COMMAND_ADD)
  {
    // Check for buffer overflow first.
    if(itemCount == ITEM_BUFFER_SIZE)
      return false;

    // Check if item exists, add or update accordingly.
    uint32_t id = GetIdFromPackage(decodeBuffer);
    int8_t index = FindItem(id);
    if(index == -1)
    {
      AddItemCommand(decodeBuffer, items, &itemCount);
      index = itemCount - 1;
    }
    else
      UpdateItemCommand(decodeBuffer, items, index);

    // Switch to newly added item.
    if(settings.displayNewSession)
    {
      itemIndexApp = index;
      if(mode == MODE_APPLICATION)
        stateApplication = STATE_APPLICATION_NAVIGATE;

      return true;
    }
  }
  else if(command == MSG_COMMAND_REMOVE)
  {
    // Check if there are any existing items first.
    if(itemCount == 0)  
      return false;

    // Check if item to be removed exists.
    uint32_t id = GetIdFromPackage(decodeBuffer);
    int8_t index = FindItem(id);
    if(index == -1)
      return false;
      
    RemoveItemCommand(decodeBuffer, items, &itemCount, index);
    
    bool isItemActive = IsItemActive(index);

    itemIndexApp = GetNextIndex(itemIndexApp, itemCount, 0, settings.continuousScroll);
    itemIndexGameA = GetNextIndex(itemIndexGameA, itemCount, 0, settings.continuousScroll);
    itemIndexGameB = GetNextIndex(itemIndexGameB, itemCount, 0, settings.continuousScroll);

    if(isItemActive)
    {
      if(mode == MODE_APPLICATION)
        stateApplication = STATE_APPLICATION_NAVIGATE;

      return true;      
    }
  }
  else if(command == MSG_COMMAND_UPDATE_VOLUME)
  {
    // Check that the item exists.
    uint32_t id = GetIdFromPackage(decodeBuffer);  
    int8_t index = FindItem(id);
    if(index == -1)
      return false;

    UpdateItemVolumeCommand(decodeBuffer, items, index);

    if(IsItemActive(index))
      return true;

  }
  else if(command == MSG_COMMAND_SETTINGS)
  {
    UpdateSettingsCommand(decodeBuffer, &settings);
    stateScreen = STATE_DISPLAY_AWAKE;
    return true;
  }

  return false;
} 

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderRotation()
{
  uint8_t encoderDir = encoderRotary.process();  
  int8_t encoderDelta = 0;

  if(encoderDir == DIR_NONE)
    return false;
  else if(encoderDir == DIR_CW)
    encoderDelta = 1;
  else if(encoderDir == DIR_CCW)
    encoderDelta = -1;

  if(itemCount == 0 || stateScreen == STATE_DISPLAY_SLEEP)
    return true;

  if(mode == MODE_MASTER)
  {
    items[0].volume += encoderDelta * encoderVolumeStep;
    items[0].volume = constrain(items[0].volume, 0, 100);

    SendItemVolumeCommand(&items[0], sendBuffer, encodeBuffer);
  }
  else if(mode == MODE_APPLICATION)
  {
    if(stateApplication == STATE_APPLICATION_NAVIGATE)
      itemIndexApp = GetNextIndex(itemIndexApp, itemCount, encoderDelta, settings.continuousScroll);

    else if(stateApplication == STATE_APPLICATION_EDIT)
    {
      items[itemIndexApp].volume += encoderDelta * encoderVolumeStep;
      items[itemIndexApp].volume = constrain(items[itemIndexApp].volume, 0, 100);

      SendItemVolumeCommand(&items[itemIndexApp], sendBuffer, encodeBuffer);
    }
  }
  else if(mode == MODE_GAME)
  {
    if(stateGame == STATE_GAME_SELECT_A)
      itemIndexGameA = GetNextIndex(itemIndexGameA, itemCount, encoderDelta, settings.continuousScroll);
    else if(stateGame == STATE_GAME_SELECT_B)
      itemIndexGameB = GetNextIndex(itemIndexGameB, itemCount, encoderDelta, settings.continuousScroll);

    else if(stateGame == STATE_GAME_EDIT)
    {
      items[itemIndexGameA].volume += encoderDelta * encoderVolumeStep;
      items[itemIndexGameA].volume = constrain(items[itemIndexGameA].volume, 0, 100);

      items[itemIndexGameB].volume -= encoderDelta * encoderVolumeStep;
      items[itemIndexGameB].volume = constrain(items[itemIndexGameB].volume, 0, 100);

      SendItemVolumeCommand(&items[itemIndexGameA], sendBuffer, encodeBuffer);
      SendItemVolumeCommand(&items[itemIndexGameB], sendBuffer, encodeBuffer);
    } 
  }
  
  return true;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderButton()
{
  if(encoderButton.tapped())
  {
    if(itemCount == 0 || stateScreen == STATE_DISPLAY_SLEEP)
      return true;
    
    if(mode == MODE_APPLICATION)
      CycleApplicationState();

    else if(mode == MODE_GAME)
      CycleGameState();

    return true;
  }
  
  if(encoderButton.doubleTapped())
  {
    if(itemCount == 0 || stateScreen == STATE_DISPLAY_SLEEP)
      return true;

    if(mode == MODE_MASTER)
      ToggleMute(itemIndexMaster);

    else if(mode == MODE_APPLICATION)
      ToggleMute(itemIndexApp);

    else if(mode == MODE_GAME && stateGame == STATE_GAME_EDIT)
      ResetGameVolume();

    return true;
  }

  if(encoderButton.held())
  {
    if(itemCount == 0 || stateScreen == STATE_DISPLAY_SLEEP)
      return true;
      
    CycleMode();      
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

  uint32_t activityTimeDelta = millis() - lastActivityTime;

  if(stateScreen == STATE_DISPLAY_AWAKE)
  {
    if(activityTimeDelta > settings.sleepAfterSeconds * 1000)
    {
      stateScreen = STATE_DISPLAY_SLEEP;
      return true;
    }
  }
  else if(stateScreen == STATE_DISPLAY_SLEEP)
  {
    if(activityTimeDelta < settings.sleepAfterSeconds * 1000)
    {
      stateScreen = STATE_DISPLAY_AWAKE;
      return true;
    }
  }

  return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateActivityTime()
{
  lastActivityTime = millis();
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateDisplay()
{
  if(stateScreen == STATE_DISPLAY_SLEEP)
  {
    DisplaySleep(display);
    return;
  }

  if(itemCount == 0)
  {
    DisplaySplashScreen(display);
    return;
  }
  
  if(mode == MODE_MASTER)
  {
    DisplayMasterSelectScreen(display, items[0].volume, items[0].isMuted, mode, MODE_COUNT);
  }
  else if(mode == MODE_APPLICATION)
  {
    if(stateApplication == STATE_APPLICATION_NAVIGATE)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexApp, itemCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexApp, itemCount, settings.continuousScroll);
      DisplayApplicationSelectScreen(display, items[itemIndexApp].name, items[itemIndexApp].volume, items[itemIndexApp].isMuted, scrollLeft, scrollRight, mode, MODE_COUNT);
    }

    else if(stateApplication == STATE_APPLICATION_EDIT)
      DisplayApplicationEditScreen(display, items[itemIndexApp].name, items[itemIndexApp].volume, items[itemIndexApp].isMuted, mode, MODE_COUNT);
  }
  else if(mode == MODE_GAME)
  {
    if(stateGame == STATE_GAME_SELECT_A)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexGameA, itemCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexGameA, itemCount, settings.continuousScroll);
      DisplayGameSelectScreen(display, items[itemIndexGameA].name, items[itemIndexGameA].volume, items[itemIndexGameA].isMuted, "A", scrollLeft, scrollRight, mode, MODE_COUNT);
    }
    else if(stateGame == STATE_GAME_SELECT_B)
    {
      uint8_t scrollLeft = CanScrollLeft(itemIndexGameB, itemCount, settings.continuousScroll);
      uint8_t scrollRight = CanScrollRight(itemIndexGameB, itemCount, settings.continuousScroll);
      DisplayGameSelectScreen(display, items[itemIndexGameB].name, items[itemIndexGameB].volume, items[itemIndexGameB].isMuted, "B", scrollLeft, scrollRight, mode, MODE_COUNT);
    }
    else if(stateGame == STATE_GAME_EDIT)
      DisplayGameEditScreen(display, items[itemIndexGameA].name, items[itemIndexGameB].name, items[itemIndexGameA].volume, items[itemIndexGameB].volume, items[itemIndexGameA].isMuted, items[itemIndexGameB].isMuted, mode, MODE_COUNT);
  }
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateLighting()
{
   if(stateScreen == STATE_DISPLAY_SLEEP)
   {
     SetPixelsColor(pixels, 0,0,0);
     return;
   }
 
   if(itemCount == 0)
   {
     SetPixelsColor(pixels, 128,128,128);
     return;
   }
   
   if(mode == MODE_MASTER)
   {
      uint8_t volumeColor = round(items[0].volume * 2.55f);
      SetPixelsColor(pixels, volumeColor, 255 - volumeColor, volumeColor);
   }
   else if(mode == MODE_APPLICATION)
   {
      uint8_t volumeColor = round(items[itemIndexApp].volume * 2.55f);
      SetPixelsColor(pixels, volumeColor, 255 - volumeColor, volumeColor);
   }
   else if(mode == MODE_GAME)
   {
     uint8_t volumeColor;
     if(stateGame == STATE_GAME_SELECT_A)
     {
       volumeColor = round(items[itemIndexGameA].volume * 2.55f);
       SetPixelsColor(pixels, volumeColor, 255 - volumeColor, volumeColor);
     }
     else if(stateGame == STATE_GAME_SELECT_B)
     {
       volumeColor = round(items[itemIndexGameB].volume * 2.55f);
       SetPixelsColor(pixels, volumeColor, 255 - volumeColor, volumeColor);
     }
     else
     {
      SetPixelsColor(pixels, 128, 128, 128);
     }
   }
}

//---------------------------------------------------------
//---------------------------------------------------------
void CycleMode()
{
    mode++;

    if(mode == MODE_COUNT)
      mode = 0;
}

//---------------------------------------------------------
//---------------------------------------------------------
void CycleApplicationState()
{
  stateApplication++;
  if(stateApplication == STATE_APPLICATION_COUNT)
    stateApplication = 0;
}

//---------------------------------------------------------
//---------------------------------------------------------
void CycleGameState()
{
  stateGame++;
  if(stateGame == STATE_GAME_COUNT)
    stateGame = 0;
}

//---------------------------------------------------------
//---------------------------------------------------------
void ToggleMute(int8_t index)
{
  items[index].isMuted = !items[index].isMuted;
  SendItemVolumeCommand(&items[index], sendBuffer, encodeBuffer);
}

//---------------------------------------------------------
//---------------------------------------------------------
void ResetGameVolume()
{
  items[itemIndexGameA].volume = 50;
  items[itemIndexGameB].volume = 50;

  SendItemVolumeCommand(&items[itemIndexGameA], sendBuffer, encodeBuffer);
  SendItemVolumeCommand(&items[itemIndexGameB], sendBuffer, encodeBuffer);
}

//---------------------------------------------------------
// Finds the item with the given id.
// Returns the index of the item if found, -1 otherwise.
//---------------------------------------------------------
int8_t FindItem(uint32_t id)
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
  if(mode == MODE_MASTER && itemIndexMaster == index)
    return true;

  else if(mode == MODE_APPLICATION && itemIndexApp == index)
    return true;

  else if(mode == MODE_GAME && (itemIndexGameA == index || itemIndexGameB == index))
    return true;

  return false;
}

//---------------------------------------------------------
//---------------------------------------------------------
void RequireDisplayUpdate()
{
  UpdateActivityTime();
  isDirty = true;
}

