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
// Third-party
#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <Adafruit_NeoPixel.h>
#include <ButtonEvents.h>

#define HALF_STEP
#include <Rotary.h>

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
  uint8_t sleepAfterSeconds = 30;
<<<<<<< HEAD
=======
  bool continuousScroll = 1;
>>>>>>> parent of 7ce8b14... Change continuous scroll type
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
uint8_t state = STATE_APPLICATION_NAVIGATE;
uint8_t isDirty = true;

struct Item items[ITEM_BUFFER_SIZE];
int8_t itemIndex = -1;
uint8_t itemCount = 0;

// Settings
struct Settings settings;

// Rotary Encoder
ButtonEvents encoderButton;
Rotary encoderRotary(PIN_ENCODER_OUTB, PIN_ENCODER_OUTA);
int8_t encoderVolumeStep = 5;


// Sleep
uint8_t screenState = STATE_SCREEN_AWAKE;
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
  display = new Adafruit_SSD1306(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, SCREEN_RESET);
  if(!display->begin(SSD1306_SWITCHCAPVCC, 0x3C)) 
    for(;;);

  display->setRotation(2);
  DisplaySplash(display);

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
      ProcessPackage();

    ClearReceive();
    UpdateActivityTime();
    isDirty = true;
  }

  if(ProcessEncoderRotation() || ProcessEncoderButton())
  {
    UpdateActivityTime();
    isDirty = true;
  }

  if(ProcessSleep())
  {
    isDirty = true;
  }

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
//---------------------------------------------------------
void ProcessPackage()
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
      return;

    // Check if item exists, add or update accordingly.
    uint32_t id = GetIdFromPackage(decodeBuffer);
    int8_t index = FindItem(id);
    if(index == -1)
    {
      AddItemCommand(decodeBuffer, items, &itemCount);
      index = itemCount - 1;
    }
    else
    {
      UpdateItemCommand(decodeBuffer, items, index);
    }

    // Switch to newly added item.
    if(settings.displayNewSession)
      itemIndex = index;
  }
  else if(command == MSG_COMMAND_REMOVE)
  {
    // Check if there are any existing items first.
    if(itemCount == 0)  
      return;

    // Check if item to be removed exists.
    uint32_t id = GetIdFromPackage(decodeBuffer);
    int8_t index = FindItem(id);
    if(index == -1)
      return;
      
    RemoveItemCommand(decodeBuffer, items, &itemCount, index);

    // Make sure current menu index is not out of bounds after removing item.
    itemIndex = constrain(itemIndex, 0, itemCount - 1);
  }
  else if(command == MSG_COMMAND_UPDATE_VOLUME)
  {
    // Check that the item exists.
    uint32_t id = GetIdFromPackage(decodeBuffer);  
    int8_t index = FindItem(id);
    if(index == -1)
      return;

    UpdateItemVolumeCommand(decodeBuffer, items, index);
  }
  else if(command == MSG_COMMAND_SETTINGS)
  {
    UpdateSettingsCommand(decodeBuffer, &settings);
  }
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

  if(itemCount == 0)
    return true;

  if(state == STATE_APPLICATION_NAVIGATE)
  {
    itemIndex += encoderDelta;
    itemIndex = constrain(itemIndex, 0, itemCount - 1);
  }

  else if(state == STATE_APPLICATION_EDIT)
  {
    items[itemIndex].volume += encoderDelta * encoderVolumeStep;
    items[itemIndex].volume = constrain(items[itemIndex].volume, 0, 100);

    SendItemVolumeCommand(&items[itemIndex], sendBuffer, encodeBuffer);
  }
  
  return true;
}

//---------------------------------------------------------
//---------------------------------------------------------
bool ProcessEncoderButton()
{
  if(encoderButton.tapped())
  {
    if(itemCount > 0)
      SwitchMode();
      
    return true;
  }
  
  if(encoderButton.doubleTapped())
  {

  }

  if(encoderButton.held())
  {

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

  if(screenState == STATE_SCREEN_AWAKE)
  {
    if(activityTimeDelta > settings.sleepAfterSeconds * 1000)
    {
      screenState = STATE_SCREEN_SLEEP;
      return true;
    }
  }
  else if(screenState == STATE_SCREEN_SLEEP)
  {
    if(activityTimeDelta < settings.sleepAfterSeconds * 1000)
    {
      screenState = STATE_SCREEN_AWAKE;
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
  if(screenState == STATE_SCREEN_SLEEP)
  {
    display->clearDisplay();
    display->display();
    return;
  }

  if(itemCount == 0)
  {
    DisplaySplash(display);
  }
  else if(state == STATE_APPLICATION_NAVIGATE)
  {
    DisplayAppNavigateScreen(display, &items[itemIndex], itemIndex, itemCount);
  }
  else if(state == STATE_APPLICATION_EDIT)
  {
    DisplayAppEditScreen(display, &items[itemIndex]);
  }
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateLighting()
{
   if(screenState == STATE_SCREEN_SLEEP)
   {
     SetPixelsColor(pixels, 0,0,0);
     return;
   }
 
   if(itemCount == 0)
   {
     SetPixelsColor(pixels, 128,128,128);
   }
   else
   {
      uint8_t volumeColor = round(items[itemIndex].volume * 2.55f);
      SetPixelsColor(pixels, volumeColor, 255 - volumeColor, volumeColor);
   }
}

//---------------------------------------------------------
//---------------------------------------------------------
void SwitchMode()
{
  if(state == STATE_APPLICATION_NAVIGATE)
  {
    state = STATE_APPLICATION_EDIT;
  }
  else if(state == STATE_APPLICATION_EDIT)
  {
    state = STATE_APPLICATION_NAVIGATE;
  }
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
