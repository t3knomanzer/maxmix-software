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
void DisplayMasterSelectScreen(Adafruit_SSD1306* display, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer, uint32_t now, bool isDirty)
{
  display->clearDisplay();

  DrawSelectionItemName(display, "VOL", displayScrollTimer, now, DISPLAY_SCROLL_OFFSET_UNSET, isDirty);
  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionItemVolume(display, volume);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayApplicationSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer, uint32_t now, uint16_t* prevScrollOffset, bool isDirty)
{
  if (!DrawSelectionItemName(display, name, displayScrollTimer, now, *prevScrollOffset, isDirty))
    return;
  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

//---------------------------------------------------------
// Draws the Game mode selection screen
//---------------------------------------------------------
void DisplayGameSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, char* channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer, uint32_t now, uint16_t* prevScrollOffset, bool isDirty)
{
  if (!DrawSelectionItemName(display, name, displayScrollTimer, now, prevScrollOffset, isDirty))
    return;
  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionVolumeBar(display, volume, isMuted);
  DrawSelectionChannelName(display, channel);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode Edit screen
//---------------------------------------------------------
void DisplayApplicationEditScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer, uint32_t now, uint16_t* prevScrollOffset, bool isDirty)
{
  uint8_t x0, y0, x1, y1;
  uint16_t scrollOffset = 0;
  
  if (strlen(name) > DISPLAY_CHAR_MAX_X1)
  {
    scrollOffset = ScrollOffset(displayScrollTimer, now, strlen(name), 0, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_CHAR_MAX_WIDTH_X1, DISPLAY_SCROLL_STEP_INTERVAL_X1);
    if (*prevScrollOffset == scrollOffset && !isDirty)
      return;
    *prevScrollOffset = scrollOffset;
  }
  
  display->clearDisplay();

  if (strlen(name) <= DISPLAY_CHAR_MAX_X1)
    scrollOffset = 0;

  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE - scrollOffset, 0);

  uint8_t len = min(strlen(name), DISPLAY_CHAR_MAX_X1 + 1 + (scrollOffset / (DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_SPACING_X1)));
  for (size_t i = 0; i < len; i++)
    display->print(name[i]);

  // Mask texts
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, 0);
  display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE + 1, 0, DISPLAY_WIDTH, DISPLAY_CHAR_HEIGHT_X1, 0);

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
void DisplayGameEditScreen(Adafruit_SSD1306* display, char* nameA, char* nameB, uint8_t volumeA, uint8_t volumeB, bool isMutedA, bool isMutedB, uint8_t modeIndex, uint8_t modeCount, uint32_t* displayScrollTimer, uint32_t now, uint16_t* prevScrollOffsetA, uint16_t* prevScrollOffsetB, bool isDirty)
{
  uint8_t py;
  bool requireUpdate = false;

  uint16_t scrollOffsetA = ScrollOffset(displayScrollTimer, now, strlen(nameA), strlen(nameB), DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH, DISPLAY_SCROLL_STEP_INTERVAL_X1);
  uint16_t scrollOffsetB = ScrollOffset(displayScrollTimer, now, strlen(nameB), strlen(nameA), DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH, DISPLAY_SCROLL_STEP_INTERVAL_X1);

  if (strlen(nameA) < DISPLAY_GAME_EDIT_CHAR_MAX && strlen(nameB) < DISPLAY_GAME_EDIT_CHAR_MAX && !isDirty)
  {
    return;
  }
  else if (scrollOffsetA == *prevScrollOffsetA && scrollOffsetB == *prevScrollOffsetB && !isDirty)
  {
    return;
  }

  if (strlen(nameA) < DISPLAY_GAME_EDIT_CHAR_MAX)
  {
    scrollOffsetA = 0;
  }
  else
  {
    *prevScrollOffsetA = scrollOffsetA;  
  }

  if (strlen(nameB) < DISPLAY_GAME_EDIT_CHAR_MAX)
  {
    scrollOffsetB = 0;
  }
  else
  {
    *prevScrollOffsetB = scrollOffsetB;  
  }
  
  display->clearDisplay();
  
  py = DISPLAY_MARGIN_X2;
  DrawGameEditItem(display, nameA, volumeA, isMutedA, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, scrollOffsetA);
    
  py = DISPLAY_MARGIN_X2 + DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2 + DISPLAY_MARGIN_X1;
  DrawGameEditItem(display, nameB, volumeB, isMutedB, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, scrollOffsetB);

  DrawDotGroup(display, modeIndex, modeCount);

  display->display();
}

//---------------------------------------------------------
// Draws a row of the Game mode screen
//---------------------------------------------------------
bool DrawGameEditItem(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t px, uint8_t py, uint16_t scrollOffset)
{
  // Name
  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE - scrollOffset, py);

  uint8_t len = min(strlen(name), DISPLAY_GAME_EDIT_CHAR_MAX + 1 + scrollOffset / (DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_SPACING_X1));
  for (size_t i = 0; i < len; i++)
    display->print(name[i]);

  // Mask texts
  display->fillRect(0, py, DISPLAY_AREA_CENTER_MARGIN_SIDE, py + DISPLAY_CHAR_HEIGHT_X1, 0);
  display->fillRect(DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH, py, DISPLAY_WIDTH, py + DISPLAY_CHAR_HEIGHT_X1, 0);

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

  return true;
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
bool DrawSelectionItemName(Adafruit_SSD1306* display, char* name, uint32_t* displayScrollTimer, uint32_t now, uint16_t* prevScrollOffset, bool isDirty)
{
  uint16_t scrollOffset = 0;
  if (strlen(name) > DISPLAY_CHAR_MAX_X2)
  {
    scrollOffset = ScrollOffset(displayScrollTimer, now, strlen(name), 0, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_CHAR_MAX_WIDTH_X2, DISPLAY_SCROLL_STEP_INTERVAL_X2);
    if (*prevScrollOffset == scrollOffset && !isDirty)
      return false;
    *prevScrollOffset = scrollOffset;
  }
  
  display->clearDisplay();

  display->setTextSize(2);
  display->setTextColor(WHITE);
  display->setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE - scrollOffset, 0);

  uint8_t len = min(strlen(name), DISPLAY_CHAR_MAX_X2 + 1 + scrollOffset / (DISPLAY_CHAR_WIDTH_X2 + DISPLAY_CHAR_SPACING_X2));
  for (size_t i = 0; i < len; i++)
    display->print(name[i]);

  // Mask texts
  display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, 0);
  display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_WIDTH, DISPLAY_CHAR_HEIGHT_X2, 0);
  
  return true;
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

//---------------------------------------------------------
// Draw Game Mode Channel Name
//---------------------------------------------------------
void DrawSelectionChannelName(Adafruit_SSD1306* display, char* channel)
{
  display->setTextSize(1);
  display->setTextColor(WHITE);
  display->setCursor(0, DISPLAY_AREA_CENTER_HEIGHT - DISPLAY_CHAR_HEIGHT_X1 - 1);

  display->print(channel);
}

//---------------------------------------------------------
// Draw Volume Bar
//---------------------------------------------------------
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

//---------------------------------------------------------
// Calculates Offset to print scrolling text
//---------------------------------------------------------
uint16_t ScrollOffset(uint32_t* displayScrollTimer, uint32_t now, size_t nameLength, size_t nameLengthOther, uint8_t charWidth, uint8_t charSpacing, uint8_t maxDrawWidth, uint8_t stepDelay)
{
  uint32_t scrollTimerDelta = now - *displayScrollTimer;

  if (scrollTimerDelta < DISPLAY_SCROLL_DELAY_INITIAL)
  {
    return 0;
  }

  uint16_t currentOffset = (scrollTimerDelta - DISPLAY_SCROLL_DELAY_INITIAL) / stepDelay;
  uint16_t maxOffset = (nameLength * (charWidth + charSpacing)) - maxDrawWidth;
  uint32_t maxOffsetTime = (maxOffset * stepDelay) + DISPLAY_SCROLL_DELAY_INITIAL;

  if (currentOffset < maxOffset) // Initial Scroll
  {
    return currentOffset;
  }
  else if (scrollTimerDelta < maxOffsetTime + DISPLAY_SCROLL_DELAY_REVERSE) // Pause
  {
    return maxOffset;
  }
  else if (scrollTimerDelta < maxOffsetTime + DISPLAY_SCROLL_DELAY_REVERSE + maxOffsetTime - DISPLAY_SCROLL_DELAY_INITIAL) // Reverse Scroll
  {
    return (maxOffset - ((scrollTimerDelta - maxOffsetTime - DISPLAY_SCROLL_DELAY_REVERSE) / stepDelay));
  }
  else // Reset Scroll
  {
    if (nameLengthOther == 0) // GameMode check
    {
      UpdateScrollTimer();
    }
    else if (nameLength >= nameLengthOther)
    {
      UpdateScrollTimer();
    }
    return 0;
  }
}
