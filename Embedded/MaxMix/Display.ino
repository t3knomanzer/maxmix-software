//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
//********************************************************

//********************************************************
// *** INCLUDES
//********************************************************
#include <Wire.h>
#include "Logo.h"

float timerDisplayA = 0;
float timerDisplayB = 0;

//********************************************************
// *** FUNCTIONS
//********************************************************
void TimerDisplayUpdate(uint32_t delta)
{
  timerDisplayA += delta / 1000.0f;
  timerDisplayB += delta / 1000.0f;
}

void ResetTimerDisplayA()
{
  timerDisplayA = 0;
}

void ResetTimerDisplayB()
{
  timerDisplayB = 0;
}

void TimerDisplayReset()
{
  ResetTimerDisplayA();
  ResetTimerDisplayB();
}

float GetTimerDisplayA()
{
  return max(0, timerDisplayA - DISPLAY_SCROLL_IDLE_TIME);
}

float GetTimerDisplayB()
{
  return max(0, timerDisplayB - DISPLAY_SCROLL_IDLE_TIME);
}

Adafruit_SSD1306* InitializeDisplay()
{
  Adafruit_SSD1306* display = new Adafruit_SSD1306(DISPLAY_WIDTH, DISPLAY_HEIGHT, &Wire, DISPLAY_RESET);
  display->begin(SSD1306_SWITCHCAPVCC, DISPLAY_ADDRESS);
  display->setRotation(2);
  display->setTextWrap(false);

  return display;
}

void DisplaySleep(Adafruit_SSD1306* display)
{
  display->clearDisplay();
  display->display();
}

//---------------------------------------------------------
// Draws the splash screen logo
//---------------------------------------------------------
void DisplaySplashScreen(Adafruit_SSD1306* display)
{
  display->clearDisplay();
  display->drawBitmap(0, 0, LOGOBMP, LOGO_WIDTH, LOGO_HEIGHT, 1);
  display->display();
}

//---------------------------------------------------------
// Draws the master mode screen
//---------------------------------------------------------
void DisplayMasterSelectScreen(Adafruit_SSD1306* display, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  display->clearDisplay();

  DrawDotGroup(display, modeIndex, modeCount);
  DrawItemName(display, "VOL", 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, GetTimerDisplayA, ResetTimerDisplayA, DISPLAY_SCROLL_SPEED_X2);
  DrawSelectionItemVolume(display, volume);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayApplicationSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  display->clearDisplay();

  DrawDotGroup(display, modeIndex, modeCount);
  
  DrawItemName(display, name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, GetTimerDisplayA, ResetTimerDisplayA, DISPLAY_SCROLL_SPEED_X2);
  // Clear sides
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
  display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);

  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayGameSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, char* channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  display->clearDisplay();

  DrawDotGroup(display, modeIndex, modeCount);
  
  DrawItemName(display, name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, GetTimerDisplayA, ResetTimerDisplayA, DISPLAY_SCROLL_SPEED_X2);
  // Clear sides
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
  display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
  
  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionVolumeBar(display, volume, isMuted);
  DrawSelectionChannelName(display, channel);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode Edit screen
//---------------------------------------------------------
void DisplayApplicationEditScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  display->clearDisplay();
  
  DrawDotGroup(display, modeIndex, modeCount);

  uint8_t x0, y0, x1, y1;

  // Item name
  DrawItemName(display, name, 1, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_HEIGHT_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, GetTimerDisplayA, ResetTimerDisplayA, DISPLAY_SCROLL_SPEED_X1);

  // Clear sides
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);
  display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);

  // Volume bar min
  x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE;
  y0 = DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2;
  y1 = y0 + DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2 - 1;

  display->drawLine(x0, y0, x0, y1, WHITE);

  // Volume bar
  x0 += 1 + DISPLAY_MARGIN_X1;
  x1 = DISPLAY_AREA_CENTER_WIDTH - 2 - DISPLAY_MARGIN_X1 * 3 - DISPLAY_CHAR_WIDTH_X2 * 3  - DISPLAY_CHAR_SPACING_X2 * 2; // Volume bar max width
  x1 = map(volume, 0, 100, 0, x1);

  if(x1 > 0)
  {
    if(isMuted)
      display->drawRect(x0, y0, x1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2, WHITE);
    else
      display->fillRect(x0, y0, x1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2, WHITE);
  }

  // Volume bar max
  x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 2 - DISPLAY_MARGIN_X1;

  display->drawLine(x0, y0, x0, y1, WHITE);

  // Volume Digits
  if( volume < 10)
    x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2;
  else if(volume < 100)
    x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 2;
  else if(volume == 100)
    x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 3;

  display->setTextSize(2);
  display->setTextColor(WHITE);
  display->setCursor(x0, y0);
  display->print(volume);

  display->display();
}

//---------------------------------------------------------
// Draws the Game mode screen
//---------------------------------------------------------
void DisplayGameEditScreen(Adafruit_SSD1306* display, char* nameA, char* nameB, uint8_t volumeA, uint8_t volumeB, bool isMutedA, bool isMutedB, uint8_t modeIndex, uint8_t modeCount)
{
  uint8_t py;
  
  display->clearDisplay();
  
  DrawDotGroup(display, modeIndex, modeCount);

  py = DISPLAY_MARGIN_X2;
  DrawGameEditItem(display, nameA, volumeA, isMutedA, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, GetTimerDisplayA, ResetTimerDisplayA);

  py = DISPLAY_MARGIN_X2 + DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2 + DISPLAY_MARGIN_X1;
  DrawGameEditItem(display, nameB, volumeB, isMutedB, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, GetTimerDisplayB, ResetTimerDisplayB);

  display->display();
}

//---------------------------------------------------------
// Draws a row of the Game mode screen
//---------------------------------------------------------
void DrawGameEditItem(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t px, uint8_t py, float (*getTime)(), void (*resetTime)())
{
  // Name 
  display->setTextSize(1);             
  display->setTextColor(WHITE);
  display->setCursor(px, py);

  // Item name
  DrawItemName(display, name, 1, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_HEIGHT_X1, DISPLAY_CHAR_SPACING_X1, px, py, getTime, resetTime, DISPLAY_SCROLL_SPEED_X1);

  // Clear sides
  display->fillRect(0, py, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);
  display->fillRect(DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH, py, DISPLAY_WIDTH, DISPLAY_CHAR_HEIGHT_X1, BLACK);

  // Volume bar min indicator
  px += DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH + DISPLAY_MARGIN_X2;
  display->drawLine(px, py, px, py + DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT - 1, WHITE);

  // Volume bar
  px += 1 + DISPLAY_MARGIN_X1;  
  uint8_t maxWidth = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH - DISPLAY_MARGIN_X2 - 2 - DISPLAY_MARGIN_X1 * 2;
  uint8_t width = map(volume, 0, 100, 0, maxWidth);
  
  if(width > 0)
  {
    if(isMuted)
      display->drawRect(px, py, width, DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT, WHITE);
    else
      display->fillRect(px, py, width, DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT, WHITE);
  }

  // Volume bar min indicator
  px += maxWidth + DISPLAY_MARGIN_X1;
  display->drawLine(px, py, px, py + DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT - 1, WHITE);
}

//---------------------------------------------------------
// Draw Mode Indicator
// Horizontal alignment: display center
// Vertical alignment: display bottom
//---------------------------------------------------------
void DrawDotGroup(Adafruit_SSD1306* display, uint8_t index, uint8_t count)
{
  uint8_t px, py, x0, y0, dotSize;  

  px = DISPLAY_WIDTH / 2 - DISPLAY_WIDGET_DOTGROUP_WIDTH / 2;
  py = DISPLAY_HEIGHT - DISPLAY_WIDGET_DOTGROUP_HEIGHT / 2;
  
  x0 = px;
  y0 = py;

  for(uint8_t i = 0; i < count; i++)
  {
    dotSize = i == index ? DISPLAY_WIDGET_DOT_SIZE_X2 : DISPLAY_WIDGET_DOT_SIZE_X1;
    y0 = py - dotSize / 2;

    display->fillRect(x0, y0, dotSize, dotSize, WHITE);
    x0 += dotSize + DISPLAY_MARGIN_X2;
  }
}


//---------------------------------------------------------
// Draw Selection Arrows
//---------------------------------------------------------
void DrawSelectionArrows(Adafruit_SSD1306* display, uint8_t leftArrow, uint8_t rightArrow)
{
  uint8_t x0, y0, x1, y1, x2, y2;

  if(leftArrow)
  {
    x0 = 0;
    y0 = DISPLAY_MARGIN_X2 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    x1 = x0 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    y1 = y0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
    x2 = x0 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    y2 = y0 + DISPLAY_WIDGET_ARROW_SIZE_X1;

    display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
  }

  if(rightArrow)
  {
    x0 = DISPLAY_WIDTH - 1;
    y0 = DISPLAY_MARGIN_X2 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    x1 = x0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
    y1 = y0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
    x2 = x0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
    y2 = y0 + DISPLAY_WIDGET_ARROW_SIZE_X1;

    display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
  }
}

//---------------------------------------------------------
// Draw Selection Arrows
//---------------------------------------------------------
void DrawItemName(Adafruit_SSD1306* display, char* name, uint8_t fontSize, uint8_t charWidth, uint8_t charHeight, uint8_t charSpacing,
                  uint8_t x, uint8_t y, float (*getTime)(), void (*resetTime)(), float scrollSpeed)
{
  uint8_t nameCopies = 1;
  uint8_t nameLength = strlen(name);
  int16_t scrollMin = 0;
  int16_t scrollMax = (nameLength + 1) * (charWidth + charSpacing);
  int16_t scroll = Scroll(name, (*getTime)(), scrollMin, scrollMax, scrollSpeed); 

  if(abs(scroll) >= abs(scrollMax))
    (*resetTime)();

  if(abs(scroll) > 0)
    nameCopies = 2;

  display->setTextSize(fontSize);
  display->setTextColor(WHITE);  
  display->setCursor(x - scroll, y);
  while(nameCopies > 0)
  {
    for(size_t i = 0; i < nameLength; i++)
      display->print(name[i]);

    display->print(' ');
    nameCopies--;
  }
}

//---------------------------------------------------------
// Draw Selection Arrows
//---------------------------------------------------------
void DrawSelectionItemVolume(Adafruit_SSD1306* display, uint8_t volume)
{
  uint8_t x0;

  if( volume < 10)
    x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2;
  else if(volume < 100)
    x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2 * 2 - DISPLAY_CHAR_SPACING_X2 * 2;
  else if(volume == 100)
    x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 3;

  display->setTextSize(2);
  display->setTextColor(WHITE);
  display->setCursor(x0, 0);
  
  display->print(volume);
}

void DrawSelectionChannelName(Adafruit_SSD1306* display, char* channel)
{
  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(0, DISPLAY_AREA_CENTER_HEIGHT - DISPLAY_CHAR_HEIGHT_X1 - 1);
  
  display->print(channel);
}

void DrawSelectionVolumeBar(Adafruit_SSD1306* display, uint8_t volume, bool isMuted)
{
  uint8_t x0, y0, x1, y1;

  // Volume bar min margin
  x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE;
  y0 = DISPLAY_CHAR_HEIGHT_X2 + DISPLAY_MARGIN_X2;
  y1 = y0 + DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1 - 1;

  display->drawLine(x0, y0, x0, y1, WHITE);

  // Volume bar
  x0 += 1 + DISPLAY_MARGIN_X1;
  uint8_t width = DISPLAY_AREA_CENTER_WIDTH - 2 - DISPLAY_MARGIN_X1 * 2;
  width = map(volume, 0, 100, 0, width);

  if(width > 0)
  {
    if(isMuted)
      display->drawRect(x0, y0, width, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1, WHITE);
    else
      display->fillRect(x0, y0, width, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1, WHITE);
  }

  // Volume bar max margin
  x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - 1;

  display->drawLine(x0, y0, x0, y1, WHITE);
  
}

int16_t Scroll(char* name, float time, float scrollMin, float scrollMax, float speed)
{
  uint8_t nameLength = strlen(name);
  if(nameLength <= DISPLAY_CHAR_MAX_X2)
    return 0;

  // Value mapping: (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min
  float timeMax = nameLength / speed;
  int16_t scroll = (time - 0.0f) * (scrollMax - scrollMin) / (timeMax - 0.0f) + scrollMin;

  return scroll;
}