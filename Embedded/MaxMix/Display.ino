//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
// Resolution: 128x32
// Characters @size 1: 21x4
// Character size in pixels: 6.095x8
//********************************************************

//********************************************************
// *** INCLUDES
//********************************************************
#include "Logo.h"


//********************************************************
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
// Draws the splash screen logo
//---------------------------------------------------------
void DisplaySplash(Adafruit_SSD1306* display)
{
  display->clearDisplay();
  display->drawBitmap(0, 0, LOGOBMP, LOGO_WIDTH, LOGO_HEIGHT, 1);
  display->display();
}

//---------------------------------------------------------
// Draws the Application mode Navigation screen
//---------------------------------------------------------
void DisplayAppNavigateScreen(Adafruit_SSD1306* display, Item* item, int8_t itemIndex, uint8_t itemCount, uint8_t continuousScroll)
{
  display->clearDisplay();

  if(itemCount > 0)
  {
    if(continuousScroll)
    {
      // Left Arrow
      // X0, Y0, X1, Y1, X2, Y2
      // Height of text is 6.
      display->fillTriangle(0, 10, 3, 7, 3, 13, WHITE);
  
      // Right Arrow
      // X0, Y0, X1, Y1, X2, Y2
      // We start at the right of the screen and draw the triangle backwards.
      // We leave 1 pixel of margin on the right, otherwise looks fuzzy.
      display->fillTriangle(127, 10, 124, 7, 124, 13, WHITE);
    }
    else
    {
      if(itemIndex > 0)
      {
        // Left Arrow
        // X0, Y0, X1, Y1, X2, Y2
        // Height of text is 6.
        display->fillTriangle(0, 10, 3, 7, 3, 13, WHITE);
      }
    
      if(itemIndex < itemCount - 1)
      { 
        // Right Arrow
        // X0, Y0, X1, Y1, X2, Y2
        // We start at the right of the screen and draw the triangle backwards.
        // We leave 1 pixel of margin on the right, otherwise looks fuzzy.
        display->fillTriangle(127, 10, 124, 7, 124, 13, WHITE);
      }
    }
  }
  
  // Item Name
  // Width of the left triangle is 3, we leave 4 pixels of margin.
  display->setTextSize(2);             
  display->setTextColor(WHITE);      
  display->setCursor(11, 4);             

  // We can fit up to 9 characters in the line.
  // Truncate the name and draw 1 char at a time.
  int nameLength = min(9, strlen(item->name));  
  for(size_t i = 0; i < nameLength; i++)
    display->print(item->name[i]);

  // Bottom Line
  // The height of size 2 font is 16 and the screen is 32 pixels high.
  // So we start at 16 down.

  // Volume Bar Margins
  display->drawLine(7, 28, 7, 32, WHITE);
  display->drawLine(121, 28, 121, 32, WHITE);

  // Volume Bar
  // The width of the bar is the area between the 2 margins - 4 pixels margin on each side. 
  int barWidth = max(1, item->volume) * 1.07;
  display->fillRect(11, 28, barWidth, 32, WHITE);

  display->display();
}

//---------------------------------------------------------
// Draws the Application mode Edit screen
//---------------------------------------------------------
void DisplayAppEditScreen(Adafruit_SSD1306* display, Item* item)
{
  display->clearDisplay();

  // Item Name
  // Width of the left triangle is 3, we leave 4 pixels of margin.
  display->setTextSize(1);             
  display->setTextColor(WHITE);      
  display->setCursor(7, 4);             

  // We can fit up to 18 characters in the line.
  // Truncate the name and draw 1 char at a time.
  // int nameLength = min(18, strlen(items[index].name));
  int nameLength = min(18, strlen(item->name));
  for(size_t i = 0; i < nameLength; i++)
    display->print(item->name[i]);

  // Bottom Line
  // The height of size 2 font is 16 and the screen is 32 pixels high.
  // So we start at 16 down.

  // Volume Bar limits
  display->drawLine(7, 18, 7, 32, WHITE); // Left
  display->drawLine(86, 18, 86, 32, WHITE); // Right

  // Volume Bar
  // The width of the bar is the area between the 2 margins - 4 pixels margin on each side. 
  int barWidth = max(1, item->volume) * 0.72;
  display->fillRect(11, 18, barWidth, 32, WHITE);

  // Volume Digits
  display->setTextSize(2);
  display->setCursor(92, 18);
  display->print(item->volume);

  display->display();
}

//---------------------------------------------------------
// Draws the Game mode screen
//---------------------------------------------------------
void DisplayGameScreen(Adafruit_SSD1306* display, Item* items, uint8_t itemIndexA, uint8_t itemIndexB,
                       uint8_t itemCount, uint8_t state, uint8_t continuousScroll)
{
  display->clearDisplay();

  // Top Row
  uint8_t paddingVertical = (SCREEN_HEIGHT - (SCREEN_MODE_GAME_ROW_HEIGHT * 2 + SCREEN_MARGIN_X2)) / 2.0f;
  uint8_t py = paddingVertical + SCREEN_MODE_GAME_ROW_HEIGHT / 2; // Vertical center of top row
  uint8_t leftArrow = CanScrollLeft(itemIndexA, itemCount, continuousScroll) && state == STATE_GAME_SELECT_A;
  uint8_t rightArrow = CanScrollRight(itemIndexA, itemCount, continuousScroll) && state == STATE_GAME_SELECT_A;

  DrawGameModeRow(display, items, itemIndexA, SCREEN_MARGIN_X1, py, leftArrow, rightArrow);

  // Bottom Row
  py += SCREEN_MARGIN_X2 + SCREEN_MODE_GAME_ROW_HEIGHT; // Vertical center of bottom row
  leftArrow = CanScrollLeft(itemIndexB, itemCount, continuousScroll) && state == STATE_GAME_SELECT_B;
  rightArrow = CanScrollRight(itemIndexB, itemCount, continuousScroll) && state == STATE_GAME_SELECT_B;
  DrawGameModeRow(display, items, itemIndexB, SCREEN_MARGIN_X1, py, leftArrow, rightArrow);

  display->display();
}


//---------------------------------------------------------
// Draws a row of the Game mode screen
//---------------------------------------------------------
void DrawGameModeRow(Adafruit_SSD1306* display, Item* items, uint8_t itemIndex, uint8_t px, uint8_t py, uint8_t leftArrow, uint8_t rightArrow)
{
  uint8_t x0, y0, x1, y1, x2, y2;

  // Left arrow
  x0 = px;
  y0 = py;
  x1 = px + SCREEN_MODE_GAME_ARROW_SIZE;
  y1 = py - SCREEN_MODE_GAME_ARROW_SIZE;
  x2 = px + SCREEN_MODE_GAME_ARROW_SIZE;
  y2 = py + SCREEN_MODE_GAME_ARROW_SIZE;

  if(leftArrow)
    display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);

  // Name 
  px += SCREEN_MODE_GAME_ARROW_SIZE + SCREEN_MARGIN_X2;
  x0 = px;
  y0 = py - SCREEN_CHAR_HEIGHT_X1 / 2;

  uint8_t nameLength = min(SCREEN_MODE_GAME_MAX_NAME_CHARS, strlen(items[itemIndex].name));

  display->setTextSize(1);             
  display->setTextColor(WHITE);
  display->setCursor(x0, y0);

  for(size_t i = 0; i < nameLength; i++)
    display->print(items[itemIndex].name[i]);

  // Right arrow
  px += SCREEN_MODE_GAME_MAX_NAME_WIDTH + SCREEN_MARGIN_X2;
  
  x0 = px;
  y0 = py;
  x1 = px - SCREEN_MODE_GAME_ARROW_SIZE;
  y1 = py - SCREEN_MODE_GAME_ARROW_SIZE;
  x2 = px - SCREEN_MODE_GAME_ARROW_SIZE;
  y2 = py + SCREEN_MODE_GAME_ARROW_SIZE;

  if(rightArrow)
    display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);

  // Min indicator
  px += SCREEN_MODE_GAME_ARROW_SIZE + SCREEN_MARGIN_X2;
  x0 = px;
  y0 = py - SCREEN_MODE_GAME_ROW_HEIGHT / 2;
  x1 = px;
  y1 = py + SCREEN_MODE_GAME_ROW_HEIGHT / 2;

  display->drawLine(x0, y0, x1, y1, WHITE);

  // Volume bar
  px += SCREEN_MARGIN_X1 + 1;
  x0 = px;
  y0 = py - SCREEN_MODE_GAME_ROW_HEIGHT / 2;
  x1 = SCREEN_WIDTH - px - SCREEN_MARGIN_X1 * 2 - 1; // max width
  y2 = SCREEN_MODE_GAME_ROW_HEIGHT; // height
  x2 = map(items[itemIndex].volume, 0, 100, 0, x1); // current width
  
  if(x2 > 0)
    display->fillRect(x0, y0, x2, y2, WHITE);

  // Max indicator
  px += x1 + SCREEN_MARGIN_X1; // Add max volume bar width and margin
  x0 = px;
  y0 = py - SCREEN_MODE_GAME_ROW_HEIGHT / 2;
  x1 = px;
  y1 = py + SCREEN_MODE_GAME_ROW_HEIGHT / 2;

  display->drawLine(x0, y0, x1, y1, WHITE);

}
