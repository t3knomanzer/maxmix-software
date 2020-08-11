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
#include <Wire.h>

// Third-party
#include "src/Adafruit_GFX/Adafruit_GFX.h"
#include "src/Adafruit_NeoPixel/Adafruit_NeoPixel.h"
#include "src/Adafruit_SSD1306/Adafruit_SSD1306.h"
#include "src/ButtonEvents/ButtonEvents.h"
#include "src/Rotary/Rotary.h"

// Custom
#include "Config.h"

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

// Rotary Encoder
ButtonEvents encoderButton;
Rotary encoderRotary(PIN_ENCODER_OUTB, PIN_ENCODER_OUTA);
int8_t encoderDelta = 0;

// Lighting
Adafruit_NeoPixel* pixels;

// Display
Adafruit_SSD1306* display;

// State
uint8_t state = STATE_READY;
uint8_t inputState = INPUT_STATE_READY;

// DebugPrint
char dbgHistory[DBG_HISTORY_SIZE][DBG_HISTORY_ITEM_SIZE];
uint8_t dbgIndex = 0;

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
  display = new Adafruit_SSD1306(DISPLAY_WIDTH, DISPLAY_HEIGHT, &Wire, DISPLAY_RESET);
  display->begin(SSD1306_SWITCHCAPVCC, DISPLAY_ADDRESS);
  display->setRotation(2);
  display->clearDisplay();
  display->display();
  
  // --- Encoder
  pinMode(PIN_ENCODER_SWITCH, INPUT_PULLUP);
  encoderButton.attach(PIN_ENCODER_SWITCH);
  encoderRotary.begin(true);
}

//---------------------------------------------------------
//---------------------------------------------------------
void loop()
{
  if(state == STATE_READY)
  {
    DbgPrint("Receiving pkg");
    ReceivePackage();
  }

  if(state == STATE_PACKAGE_RECEIVED)
  {
    DbgPrint("Decoding pkg");
    DecodePackage();
  }

  if(state == STATE_PACKAGE_DECODED)
  {
    DbgPrint("Processing pkg");
    ProcessPackage();
  }

  if(state == STATE_PACKAGE_ERROR)
  {
    DbgPrint("Pkg error");
    DbgDump();

    receiveIndex = 0;
    sendIndex = 0;    
    state = 255;
  }

  if(state == STATE_PACKAGE_PROCESSED)
  {
    DbgPrint("Pkg prcessed");
    DbgDump();

    receiveIndex = 0;
    sendIndex = 0;
    state = STATE_READY;
  }

  if(inputState == INPUT_STATE_READY)
  {
    ProcessEncoderRotation();
    ProcessEncoderButton();
  }

  if(inputState == INPUT_STATE_AVAILABLE)
  {
    SendInput();
    
    sendIndex = 0;
    encoderDelta = 0;
    inputState = INPUT_STATE_READY;
  }

  encoderButton.update();
}

//********************************************************
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
// 
//---------------------------------------------------------
void SetState(uint8_t state_)
{
  state = state_;
}

//---------------------------------------------------------
// Reads the data from the serial receive buffer.
//---------------------------------------------------------
void ReceivePackage()
{
  while(Serial.available() > 0)
  {
    uint8_t received = (uint8_t)Serial.read();

    if(receiveIndex == RECEIVE_BUFFER_SIZE)
    {
        SetState(STATE_PACKAGE_ERROR);
        return;
    }

    if(received == MSG_PACKET_DELIMITER)
    {
      SetState(STATE_PACKAGE_RECEIVED);
      return;
    }

    // Otherwise continue filling the buffer
    receiveBuffer[receiveIndex] = received;
    receiveIndex++;
  }
}

void SendPackage()
{
  uint8_t encodeSize = EncodePackage();
  Serial.write(encodeBuffer, encodeSize);
}

//---------------------------------------------------------
// 
//---------------------------------------------------------
void DecodePackage()
{
  // Decode received message
  uint8_t outSize = Decode(receiveBuffer, receiveIndex, decodeBuffer);

  // Check message size is greater than 0
  if(outSize == 0)
  {
    SetState(STATE_PACKAGE_ERROR);
    return;
  }

  // Verify message checksum
  uint8_t checksum = decodeBuffer[outSize - 1];
  if(checksum != outSize)
  {
    SetState(STATE_PACKAGE_ERROR);
    return;
  }

  SetState(STATE_PACKAGE_DECODED);
}

//---------------------------------------------------------
// 
//---------------------------------------------------------
uint8_t EncodePackage()
{
    // Add checksum
    sendBuffer[sendIndex] = sendIndex + 1;
    sendIndex++;

    // Encode message
    size_t outSize = Encode(sendBuffer, sendIndex, encodeBuffer);

    // Add packet delimiter
    encodeBuffer[outSize] = 0;
    outSize++;

    return outSize;
}

//---------------------------------------------------------
//---------------------------------------------------------
void ProcessPackage()
{
  
  uint8_t command = decodeBuffer[0];
  
  if(command == MSG_COMMAND_HS_REQUEST)
  {
    SendHandshakePackage();
  }

  else if(command == MSG_COMMAND_DISPLAY_DATA)
  {
    UpdateDisplay();
  }

  SetState(STATE_PACKAGE_PROCESSED);
} 


//---------------------------------------------------------
//---------------------------------------------------------
void ProcessEncoderRotation()
{
  uint8_t encoderDir = encoderRotary.process();  
  
  if(encoderDir == DIR_CW)
    encoderDelta = 1;

  else if(encoderDir == DIR_CCW)
    encoderDelta = -1;

  // if(encodeDir != DIR_NONE)
  //   inputState = INPUT_STATE_AVAILABLE;
}

//---------------------------------------------------------
//---------------------------------------------------------
void ProcessEncoderButton()
{
  if(encoderButton.tapped())
  {
   
  }
  
  if(encoderButton.doubleTapped())
  {
    
  }

  if(encoderButton.held())
  {
    
  }
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateDisplay()
{
  DbgPrint("Display data received");
  DbgDump();
}

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateLighting()
{
}

//---------------------------------------------------------
//---------------------------------------------------------
void SendHandshakePackage()
{
  sendBuffer[sendIndex] = MSG_COMMAND_HS_RESPONSE;  
  SendPackage();
}

//---------------------------------------------------------
//---------------------------------------------------------
void SendInput()
{

}

//---------------------------------------------------------
//---------------------------------------------------------
void DbgPrint(char* msg)
{
  strcpy(dbgHistory[dbgIndex], msg);
  dbgIndex++;

  if(dbgIndex == DBG_HISTORY_SIZE)
    dbgIndex = 0;
}

//---------------------------------------------------------
//---------------------------------------------------------
void DbgDump()
{
  display->clearDisplay();
  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(0, 0);
  
  int8_t m = max(0, dbgIndex - 1);
  for (size_t i = 0; i < DBG_HISTORY_SIZE; i++)
  {
    display->println(dbgHistory[m]);
    
    m--;
    if(m < 0)
      m = DBG_HISTORY_SIZE - 1;
  }
  
  display->display();
}