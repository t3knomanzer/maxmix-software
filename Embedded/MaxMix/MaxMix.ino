//********************************************************
// *** MAX MIX
// AUTHOR: Ruben Henares
// EMAIL: ruben@404fs.com
// DECRIPTION:
// 
//
//
// The AVR 328P chip has 2kB of SRAM. 
// Program uses 819 bytes, leaving 1229 bytes for items.
// Therefore we can store a maximum of 14 items (2048 / 84 bytes)
// We limit this to 12 maximum items for safety.,
//********************************************************


//********************************************************
// *** INCLUDES
//********************************************************
#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <Adafruit_NeoPixel.h>
#include <ClickEncoder.h>
#include <TimerOne.h>

#include "Logo.h"


//********************************************************
// *** CONSTS
//********************************************************
// Serial
#define BAUD_RATE 115200

// Pins
static const uint8_t  PIN_PIXELS = 12; //D12
static const uint8_t  PIN_ROTARY_OUTA = 15; //A1
static const uint8_t  PIN_ROTARY_OUTB = 16; //A2
static const uint8_t  PIN_ROTARY_SWITCH = 17; //A3

// States
static const uint8_t  STATE_MENU_NAVIGATE = 1;
static const uint8_t  STATE_MENU_EDIT = 2;

static const uint8_t  STATE_SCREEN_AWAKE = 0;
static const uint8_t  STATE_SCREEN_SLEEP = 1;

// Display
static const uint8_t  SCREEN_WIDTH = 128; // OLED display width, in pixels
static const uint8_t  SCREEN_HEIGHT = 32; // OLED display height, in pixels
static const uint8_t  SCREEN_RESET =   4; // Reset pin # (or -1 if sharing Arduino reset pin)

// Declaration for an SSD1306 display connected to I2C (SDA, SCL pins)
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, SCREEN_RESET);

// Lighting
static const uint8_t  PIXELS_NUM = 8; // Number of pixels in ring
Adafruit_NeoPixel pixels(PIXELS_NUM, PIN_PIXELS, NEO_GRB + NEO_KHZ800);

// Messages
static const uint8_t ITEM_BUFFER_SIZE = 10;
static const uint8_t ITEM_BUFFER_NAME_SIZE = 36;
static const uint8_t RECEIVE_BUFFER_SIZE = 128;
static const uint8_t SEND_BUFFER_SIZE = 8;

// These values match exactly the ones in the C# application.
static const uint8_t MSG_COMMAND_HS_REQUEST =  0;
static const uint8_t MSG_COMMAND_HS_RESPONSE =  1;
static const uint8_t MSG_COMMAND_ADD =  2;
static const uint8_t MSG_COMMAND_REMOVE =  3;
static const uint8_t MSG_COMMAND_UPDATE_VOLUME =  4;
static const uint8_t MSG_COMMAND_SETTINGS =  5;

static const uint8_t MSG_PACKET_DELIMITER = 0;

//********************************************************
// *** STRUCTS
//********************************************************
struct Item
{
  uint32_t id;                          // 4 Bytes (32 bit)
  char name[ITEM_BUFFER_NAME_SIZE];  // 36 Bytes (1 Bytes * 36 Chars)
  uint8_t volume;                       // 1 Byte
  uint8_t isMuted;                      // 1 Byte
                                        // 82 Bytes TOTAL
};

struct Settings
{
  uint8_t displayNewSession = 1;
  uint8_t sleepWhenInactive = 0;
  uint8_t sleepAfterSeconds = 30;
} settings;

//********************************************************
// *** VARIABLES
//********************************************************
// Serial Communication
uint8_t receiveIndex = 0;
uint8_t sendIndex = 0;
uint8_t receiveBuffer[RECEIVE_BUFFER_SIZE];
uint8_t decodeBuffer[RECEIVE_BUFFER_SIZE];
uint8_t sendBuffer[SEND_BUFFER_SIZE];
uint8_t encodeBuffer[SEND_BUFFER_SIZE];

// Menu
uint8_t menuState = STATE_MENU_NAVIGATE;
int8_t menuIndex = -1;

struct Item items[ITEM_BUFFER_SIZE];
uint8_t itemsCount = 0;

// Rotary Encoder
ClickEncoder *encoder;
int16_t encoderLast = -1;
int16_t encoderCurrent = -1;
int8_t encoderVolumeStep = 5;

// Sleep
uint8_t screenState = STATE_SCREEN_AWAKE;
uint32_t lastActivityTime = 0;

//********************************************************
// *** 
//********************************************************
void setup()
{
  Serial.begin(BAUD_RATE);

  //--- Pixels
  pixels.begin();

  // --- Display
  // Address 0x3C for 128x32
  if(!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) 
  { 
    Serial.println(F("SSD1306 allocation failed"));
    for(;;);
  }
  
  // Rotate display 180 degrees
  display.setRotation(2);

  // Show splash screen
  DisplaySplash();

  // --- Encoder
  // Setup encoder
  encoder = new ClickEncoder(PIN_ROTARY_OUTB, PIN_ROTARY_OUTA, PIN_ROTARY_SWITCH, 4, false);

  Timer1.initialize(100);
  Timer1.attachInterrupt(timerIsr);
}

void loop()
{
  // --- Comms
  // Process incoming data
  if(ReceiveData())
  {
    ProcessData();
    UpdateActivityTime();
  }

  // --- Encoder
  encoderCurrent += encoder->getValue();
  if (encoderCurrent != encoderLast)
  { 
    if(itemsCount > 0)
      ProcessEncoderRotation();

    ResetEncoder();
    UpdateActivityTime();
  }

  if(ProcessEncoderButton())
    UpdateActivityTime();

  ProcessSleep();
}

//********************************************************
// *** INTERRUPTS
//********************************************************
void timerIsr() 
{
  encoder->service();
}

//********************************************************
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
// Reads the data from the serial receive buffer.
//---------------------------------------------------------
bool ReceiveData()
{
  if(Serial.available() == 0)
    return false;

  while(Serial.available() > 0)
  {
    uint8_t received = (uint8_t)Serial.read();
    
    if(received == MSG_PACKET_DELIMITER)
      return true;
    
    // Otherwise continue filling the buffer    
    receiveBuffer[receiveIndex] = received;
    receiveIndex++;

    // Check for buffer overlow
    if(receiveIndex == RECEIVE_BUFFER_SIZE)
    {
        ResetReceive();
        return false;
    }
  }

  return false;
}

//---------------------------------------------------------
// Parses the data in the parseBuffer. 
// Splits the message into the necessary chunks and
// executes the appropiate command.
//---------------------------------------------------------
void ProcessData()
{
  // Decode received message
  uint8_t decodedSize = decode(receiveBuffer, receiveIndex, decodeBuffer);

  // Verify message
  uint8_t checksum = decodeBuffer[decodedSize - 1];
  if(checksum != decodedSize)
  {
    ResetReceive();
    return;
  }

    // Extract command
  uint8_t command = decodeBuffer[0];

  // --- Execute commands
  // Handshake Request
  if(command == MSG_COMMAND_HS_REQUEST)
    SendHandshake();

  // Session Add
  else if(command == MSG_COMMAND_ADD)
    AddItem();

  else if(command == MSG_COMMAND_REMOVE)
    RemoveItem();

  else if(command == MSG_COMMAND_UPDATE_VOLUME)
    UpdateItemVolume();

  else if(command == MSG_COMMAND_SETTINGS)
    UpdateSettings();

  // Reset
  ResetReceive();
}

//---------------------------------------------------------
// Send Handshake
//---------------------------------------------------------
void SendHandshake()
{
  sendBuffer[sendIndex++] = MSG_COMMAND_HS_RESPONSE;

  uint8_t encodeSize =  Serialize(sendBuffer, sendIndex, encodeBuffer);
  Serial.write(encodeBuffer, encodeSize);

  ResetSend();
}

//---------------------------------------------------------
// Process Rotary Encoder Rotation
//---------------------------------------------------------
void ProcessEncoderRotation()
{
  int8_t encoderPosDelta = encoderCurrent - encoderLast;

  if(menuState == STATE_MENU_NAVIGATE)
  {
    menuIndex += encoderPosDelta;
    menuIndex = constrain(menuIndex, 0, itemsCount - 1);
    DisplayMenuItem();
  }

  else if(menuState == STATE_MENU_EDIT)
  {
    UpdateItemVolume(menuIndex, encoderPosDelta * encoderVolumeStep);
    DisplayMenuItem();
  }
}

//---------------------------------------------------------
// Process Rotary Encoder Button
//---------------------------------------------------------
bool ProcessEncoderButton()
{
  ClickEncoder::Button button = encoder->getButton();
  if(button == ClickEncoder::Clicked)
  {
      SwitchMode();
      return true;
  }

  return false;
}

void ResetEncoder()
{
  encoderCurrent = encoderLast;
}

//---------------------------------------------------------
// Sleep
//---------------------------------------------------------
void ProcessSleep()
{
  if(settings.sleepWhenInactive == 0)
    return;

  uint32_t activityTimeDelta = millis() - lastActivityTime;

  if(screenState == STATE_SCREEN_AWAKE)
  {
    if(activityTimeDelta > settings.sleepAfterSeconds * 1000)
    {
      display.clearDisplay();
      display.display();
      screenState = STATE_SCREEN_SLEEP;
      SetPixels(0, 0, 0);
    }
  }
  else if(screenState == STATE_SCREEN_SLEEP)
  {
    if(activityTimeDelta < settings.sleepAfterSeconds * 1000)
    {
      if(itemsCount > 0 && menuIndex > -1)
        DisplayMenuItem();
      else
        DisplaySplash();

      screenState = STATE_SCREEN_AWAKE;
    }
  }
}

void UpdateActivityTime()
{
  lastActivityTime = millis();
}

//---------------------------------------------------------
// Sets the volume of the item with the given index
//---------------------------------------------------------
void UpdateItemVolume(uint8_t index, int8_t delta)
{
  items[index].volume += delta;
  items[index].volume = constrain(items[index].volume, 0, 100);

  sendBuffer[sendIndex++] = MSG_COMMAND_UPDATE_VOLUME;
  
  sendBuffer[sendIndex++] = (uint8_t)(items[index].id >> 24) & 0xFF;
  sendBuffer[sendIndex++] = (uint8_t)(items[index].id >> 16) & 0xFF;
  sendBuffer[sendIndex++] = (uint8_t)(items[index].id >> 8) & 0xFF;
  sendBuffer[sendIndex++] = (uint8_t)items[index].id & 0xFF;

  sendBuffer[sendIndex++] = items[index].volume;
  sendBuffer[sendIndex++] = items[index].isMuted;
  
  uint8_t encodeSize =  Serialize(sendBuffer, sendIndex, encodeBuffer);
  Serial.write(encodeBuffer, encodeSize);

  ResetSend();
}

//---------------------------------------------------------
// Adds a new item to the items array.
//---------------------------------------------------------
void AddItem()
{
  if(itemsCount == ITEM_BUFFER_SIZE)  
    return;

  uint32_t id = ((uint32_t)decodeBuffer[1]) |
                ((uint32_t)decodeBuffer[2] << 8)  |
                ((uint32_t)decodeBuffer[3] << 16) |
                ((uint32_t)decodeBuffer[4] << 24);

  // Check if the item already exists
  if(FindItem(id) > -1)
    return;

  items[itemsCount].id = id;
  memcpy(items[itemsCount].name, &decodeBuffer[5], ITEM_BUFFER_NAME_SIZE);
  items[itemsCount].volume = (uint8_t)decodeBuffer[41];
  items[itemsCount].isMuted = (uint8_t)decodeBuffer[42];

  itemsCount++;
}

//---------------------------------------------------------
// Removes the item with the given id from the items array.
//---------------------------------------------------------
void RemoveItem()
{
  if(itemsCount == 0)  
    return;

  uint32_t id = ((uint32_t)decodeBuffer[1]) |
                ((uint32_t)decodeBuffer[2] << 8)  |
                ((uint32_t)decodeBuffer[3] << 16) |
                ((uint32_t)decodeBuffer[4] << 24);

  int8_t index = FindItem(id);
  if(index == -1)
    return;
  
  // Re-order items array.
  for(uint8_t i = index; i < itemsCount - 1; i++)
   items[i] = items[i + 1];

  itemsCount--;

  // If there are no items left, display the splash screen.
  if(itemsCount == 0)
    DisplaySplash();
  // Otherwise, if there are any items left.
  else
  {
    // If the item removed was the displayed item.
    // Change the displayed item to the next one available.
    if(index == menuIndex)
    {
      // If there are items previous to the one deleted,
      // select the first one.
      if(menuIndex > 0)
        menuIndex--;

      // If we deleted the first item (0) and there 
      else
        menuIndex++;

      DisplayMenuItem();
    }
  }
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateSettings() 
{
  settings.displayNewSession = decodeBuffer[1];
  settings.sleepWhenInactive = decodeBuffer[2];
  settings.sleepAfterSeconds = decodeBuffer[3];
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateItemVolume() 
{
  uint32_t id = ((uint32_t)decodeBuffer[1]) |
                ((uint32_t)decodeBuffer[2] << 8)  |
                ((uint32_t)decodeBuffer[3] << 16) |
                ((uint32_t)decodeBuffer[4] << 24);

  
  int8_t index = FindItem(id);
  if(index > -1)
  {
    items[index].volume = decodeBuffer[5];
    items[index].isMuted = decodeBuffer[6];

    if(index == menuIndex)
      DisplayMenuItem();
  }
}

//---------------------------------------------------------
// Finds the item with the given id.
// Returns the index of the item if found, -1 otherwise.
//---------------------------------------------------------
int8_t FindItem(uint32_t id)
{
  for(int8_t i = 0; i < itemsCount; i++)
    if(items[i].id == id)
      return i;

  return -1;
}

//---------------------------------------------------------
// Empties the serial receive buffer.
//---------------------------------------------------------
void ClearSerial()
{
  while(Serial.available() > 0)
    Serial.read();
}

//---------------------------------------------------------
// Resets the message buffer.
//---------------------------------------------------------
void ClearReceiveBuffer()
{
  for(size_t i = 0; i < RECEIVE_BUFFER_SIZE; i++)
    receiveBuffer[i] = 0;
}

void ClearDecodeBuffer()
{
  for(size_t i = 0; i < RECEIVE_BUFFER_SIZE; i++)
    decodeBuffer[i] = 0;
}

void ClearSendBuffer()
{
  for(size_t i = 0; i < SEND_BUFFER_SIZE; i++)
    sendBuffer[i] = 0;
}

void ClearEncodeBuffer()
{
  for(size_t i = 0; i < SEND_BUFFER_SIZE; i++)
    encodeBuffer[i] = 0;
}

void ResetReceive()
{
  receiveIndex = 0;
  ClearReceiveBuffer();
  ClearDecodeBuffer();
}

void ResetSend()
{
  sendIndex = 0;
  ClearSendBuffer();
  ClearEncodeBuffer();
}

//---------------------------------------------------------
// Alternates between navigation and edit modes.
//---------------------------------------------------------
void SwitchMode()
{
  if(menuState == STATE_MENU_NAVIGATE)
  {
    menuState = STATE_MENU_EDIT;
  }
  else if(menuState == STATE_MENU_EDIT)
  {
    menuState = STATE_MENU_NAVIGATE;
  }
}

//---------------------------------------------------------
// Draws the splash screen logo
//---------------------------------------------------------
void DisplaySplash()
{
  display.clearDisplay();
  display.drawBitmap(0, 0, logoBmp, LOGO_WIDTH, LOGO_HEIGHT, 1);
  display.display();

  SetPixels(128, 128, 128);
}

//---------------------------------------------------------
// Draws the screen ui
//---------------------------------------------------------
void DisplayMenuItem()
{
  display.clearDisplay();

  // Left Arrow
  if(menuIndex > 0)
  {
    // X0, Y0, X1, Y1, X2, Y2
    // Height of text is 6.
    display.fillTriangle(0, 3, 3, 0, 3, 6, WHITE);
  }

  // Right Arrow
  if(menuIndex < itemsCount - 1)
  {
    // X0, Y0, X1, Y1, X2, Y2
    // We start at the right of the screen and draw the triangle backwards.
    // We leave 1 pixel of margin on the right, otherwise looks fuzzy.
    display.fillTriangle(127, 3, 124, 0, 124, 6, WHITE);
  }

  // Item Name
  // Width of the left triangle is 3, we leave 4 pixels of margin.
  display.setTextSize(1);             
  display.setTextColor(WHITE);      
  display.setCursor(7, 0);             

  // We can fit up to 18 characters in the line.
  // Truncate the name and draw 1 char at a time.
  // int nameLength = min(18, strlen(items[index].name));
  int nameLength = min(18, strlen(items[menuIndex].name));
  for(size_t i = 0; i < nameLength; i++)
    display.print(items[menuIndex].name[i]);

  // Bottom Line
  // The height of size 2 font is 16 and the screen is 32 pixels high.
  // So we start at 16 down.

  // Volume Bar Margins
  display.drawLine(7, 16, 7, 32, WHITE);
  display.drawLine(88, 16, 88, 32, WHITE);

  // Volume Bar
  // The width of the bar is the area between the 2 margins - 4 pixels margin on each side. 
  int barWidth = max(1, items[menuIndex].volume) * 0.73 + 11;
  int barHeight = max(1, 100 - items[menuIndex].volume) * 0.16 + 16; 
  display.fillTriangle(11, 32, barWidth, barHeight, barWidth, 32, WHITE);

  // Volume Digits
  display.setTextSize(2);
  display.setCursor(92, 16);
  display.print(items[menuIndex].volume);
  display.display();

  // Pixels Color
  int volumeColor = round(items[menuIndex].volume * 2.55f);
  SetPixels(volumeColor, 255 - volumeColor, volumeColor);
}

void SetPixels(uint8_t r, uint8_t g, uint8_t b)
{
  pixels.setPixelColor(0, pixels.Color(r, g, b)); // BACK
  pixels.setPixelColor(3, pixels.Color(r, g, b)); // FRONT-RIGHT
  pixels.setPixelColor(5, pixels.Color(r, g, b)); // FRONT-LEFT
  pixels.show();
}