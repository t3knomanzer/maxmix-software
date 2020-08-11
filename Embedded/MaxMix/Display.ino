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

//********************************************************
// *** FUNCTIONS
//********************************************************
Adafruit_SSD1306* InitializeDisplay()
{
  Adafruit_SSD1306* display = new Adafruit_SSD1306(DISPLAY_WIDTH, DISPLAY_HEIGHT, &Wire, DISPLAY_RESET);
  display->begin(SSD1306_SWITCHCAPVCC, DISPLAY_ADDRESS);
  display->setRotation(2);

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
void DisplayMasterSelectScreen(Adafruit_SSD1306* display, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer)
{
  display->clearDisplay();

  DrawSelectionItemName(display, "VOL", displayScrollTimer);
  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionItemVolume(display, volume);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayApplicationSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer)
{
  display->clearDisplay();

  DrawSelectionItemName(display, name, displayScrollTimer);
  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

//---------------------------------------------------------
// Draws the Game mode selection screen
//---------------------------------------------------------
void DisplayGameSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, char* channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer)
{
  display->clearDisplay();

  DrawSelectionItemName(display, name, displayScrollTimer);
  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionVolumeBar(display, volume, isMuted);
  DrawSelectionChannelName(display, channel);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode Edit screen
//---------------------------------------------------------
void DisplayApplicationEditScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer)
{
  display->clearDisplay();

  uint8_t x0, y0, x1, y1;
  uint16_t scrollOffset = ScrollOffset(displayScrollTimer, DISPLAY_SCROLL_STEP_INTERVAL);
  
  if (strlen(name) <= DISPLAY_CHAR_MAX_X1)
    scrollOffset = 0;
    
  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE - scrollOffset, 0);

  if (scrollOffset >= (strlen(name) * (DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_SPACING_X1)))
  {
    UpdateScrollTimer(displayScrollTimer);
  }

  int8_t len = min(strlen(name), DISPLAY_CHAR_MAX_X1 + (scrollOffset/(DISPLAY_CHAR_WIDTH_X1 + 3)));
  for (size_t i = 0; i < len; i++)
    display->print(name[i]);

  // Mask texts
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, 0);

  DrawDotGroup(display, modeIndex, modeCount);
  
  // Volume bar min
  x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE;
  y0 = DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2;
  y1 = y0 + DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2 - 1;

  display->drawLine(x0, y0, x0, y1, WHITE);

  // Volume bar
  x0 += 1 + DISPLAY_MARGIN_X1;
  x1 = DISPLAY_AREA_CENTER_WIDTH - 2 - DISPLAY_MARGIN_X1 * 3 - DISPLAY_CHAR_WIDTH_X2 * 3  - DISPLAY_CHAR_SPACING_X2 * 2; // Volume bar max width
  x1 = map(volume, 0, 100, 0, x1);

  if (x1 > 0)
  {
    if (isMuted)
      display->drawRect(x0, y0, x1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2, WHITE);
    else
      display->fillRect(x0, y0, x1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2, WHITE);
  }

  // Volume bar max
  x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 2 - DISPLAY_MARGIN_X1;

  display->drawLine(x0, y0, x0, y1, WHITE);

  // Volume Digits
  if ( volume < 10)
    x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2;
  else if (volume < 100)
    x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 2;
  else if (volume == 100)
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
void DisplayGameEditScreen(Adafruit_SSD1306* display, char* nameA, char* nameB, uint8_t volumeA, uint8_t volumeB, bool isMutedA, bool isMutedB, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer)
{
  uint8_t py;

  display->clearDisplay();

  py = DISPLAY_MARGIN_X2;
  DrawGameEditItem(display, nameA, volumeA, isMutedA, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, displayScrollTimer);

  py = DISPLAY_MARGIN_X2 + DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2 + DISPLAY_MARGIN_X1;
  DrawGameEditItem(display, nameB, volumeB, isMutedB, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, displayScrollTimer);
  
  DrawDotGroup(display, modeIndex, modeCount);

  display->display();
}


//---------------------------------------------------------
// Draws a row of the Game mode screen
//---------------------------------------------------------
void DrawGameEditItem(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t px, uint8_t py, uint32_t* displayScrollTimer)
{
  uint16_t scrollOffset = ScrollOffset(displayScrollTimer, DISPLAY_SCROLL_STEP_INTERVAL_GAME);
  
  if (strlen(name) <= DISPLAY_GAME_EDIT_CHAR_MAX)
    scrollOffset = 0;
    
  // Name
  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE - scrollOffset, py);

  if (scrollOffset >= (strlen(name) * (DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_SPACING_X1)))
  {
    UpdateScrollTimer(displayScrollTimer);
  }

  uint8_t len = min(strlen(name), DISPLAY_GAME_EDIT_CHAR_MAX + scrollOffset/(DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_SPACING_X1));
  for (size_t i = 0; i < len; i++)
    display->print(name[i]);
    
  // Mask texts
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, py + DISPLAY_CHAR_HEIGHT_X1, 0);
  
  // Volume bar min indicator
  px += DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH + DISPLAY_MARGIN_X2;
  display->drawLine(px, py, px, py + DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT - 1, WHITE);

  // Volume bar
  px += 1 + DISPLAY_MARGIN_X1;
  uint8_t maxWidth = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH - DISPLAY_MARGIN_X2 - 2 - DISPLAY_MARGIN_X1 * 2;
  uint8_t width = map(volume, 0, 100, 0, maxWidth);

  if (width > 0)
  {
    if (isMuted)
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

  for (uint8_t i = 0; i < count; i++)
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

  if (leftArrow)
  {
    x0 = 0;
    y0 = DISPLAY_MARGIN_X2 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    x1 = x0 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    y1 = y0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
    x2 = x0 + DISPLAY_WIDGET_ARROW_SIZE_X1;
    y2 = y0 + DISPLAY_WIDGET_ARROW_SIZE_X1;

    display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
  }

  if (rightArrow)
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
// Draw Item Name
//---------------------------------------------------------
void DrawSelectionItemName(Adafruit_SSD1306* display, char* name, uint32_t* displayScrollTimer)
{
  uint16_t scrollOffset = ScrollOffset(displayScrollTimer, DISPLAY_SCROLL_STEP_INTERVAL);
  
  if (strlen(name) <= DISPLAY_CHAR_MAX_X2)
    scrollOffset = 0;
    
  display->setTextSize(2);
  display->setTextColor(WHITE);
  display->setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE - scrollOffset, 0);

  if (scrollOffset >= (strlen(name) * (DISPLAY_CHAR_WIDTH_X2 + DISPLAY_CHAR_SPACING_X2)))
  {
    UpdateScrollTimer(displayScrollTimer);
  }

  int8_t len = min(strlen(name), DISPLAY_CHAR_MAX_X2 + scrollOffset/(DISPLAY_CHAR_WIDTH_X2 + DISPLAY_CHAR_SPACING_X2));
  for (size_t i = 0; i < len; i++)
    display->print(name[i]);

  // Mask texts
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, 0);
}

//---------------------------------------------------------
// Draw Item Volume
//---------------------------------------------------------
void DrawSelectionItemVolume(Adafruit_SSD1306* display, uint8_t volume)
{
  uint8_t x0;

  if ( volume < 10)
    x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2;
  else if (volume < 100)
    x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2 * 2 - DISPLAY_CHAR_SPACING_X2 * 2;
  else if (volume == 100)
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

  if (width > 0)
  {
    if (isMuted)
      display->drawRect(x0, y0, width, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1, WHITE);
    else
      display->fillRect(x0, y0, width, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1, WHITE);
  }

  // Volume bar max margin
  x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - 1;

  display->drawLine(x0, y0, x0, y1, WHITE);

}

void UpdateScrollTimer(uint32_t* displayScrollTimer) 
{
  *displayScrollTimer = millis();
}

uint16_t ScrollOffset(uint32_t* displayScrollTimer, uint8_t stepDelay) 
{
  uint32_t scrollTimerDelta = millis() - *displayScrollTimer;
  if (scrollTimerDelta < DISPLAY_SCROLL_DELAY) 
  {
    return 0;
  }
  else 
  {
    return ((scrollTimerDelta - DISPLAY_SCROLL_DELAY) / stepDelay);
  }
}

