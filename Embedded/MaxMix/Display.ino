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
// Draws the screen ui
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

void DisplayGameScreen__(Adafruit_SSD1306* display, Item* items, uint8_t itemIndexA, uint8_t itemIndexB, uint8_t itemCount)
{
  display->clearDisplay();

  // Channel A Left arrow
  display->setCursor(4, 4);

  // Channel A Right arrow
  // Aligned to the right, withing the left half of the screen.
  display->setCursor(128/2 - 4 - 3, 4);

  // Item names
  // The width of half the screen is halfScreen = 128/2 - 4*2(border on both sides).
  // The area available for the text is textArea = halfScreen - 3*2(size of arrows on both sides) - 2*2(margin text and between arrows)
  // That allows gives us a practical size in pixels of 128/2 - 4*2 - 3*2 - 2*2 = 46px
  // 46px / 6.095(characted width) = 7.54 characters

  // Item A
  display->setTextSize(1);             
  display->setTextColor(WHITE);
  
  uint8_t nameLength = min(7, strlen(items[itemIndexA].name));
  uint8_t nameX = round((46 - nameLength * 6) / 2.0f);
  display->setCursor(4 + 3 + 2 + nameX, 4);

  for(size_t i = 0; i < nameLength; i++)
    display->print(items[itemIndexA].name[i]);

  // Center line, height of a character
  display->drawLine(64, 4, 64, 4 + 8, WHITE);
  
  // Item B
  nameLength = min(7, strlen(items[itemIndexB].name));
  nameX = round((46 - nameLength * 6) / 2.0f);
  display->setCursor(128/2 + 3 + 2 + nameX, 4);

  for(size_t i = 0; i < nameLength; i++)
    display->print(items[itemIndexB].name[i]);

  // Volume bars
  // Item A
  uint8_t barY = map(items[itemIndexA].volume, 0, 100, 32 - 4, 4 + 8 + 4);
  display->fillRect(128/2 - 4 - 8,  barY, 8, 32 - 4 - barY , WHITE);

  // Item B
  barY = map(items[itemIndexB].volume, 0, 100, 32 - 4, 4 + 8 + 4);
  display->fillRect(128/2 + 4,  barY, 8, 32 - 4 - barY , WHITE);

  // Volume text
  display->setTextSize(2);          

  // Item A
  display->setCursor(4 + 3 + 2, 4 + 8 + 4);
  display->print(items[itemIndexA].volume);

  // Item B
  display->setCursor(128/2 + 4 + 8 + 4, 4 + 8 + 4);
  display->print(items[itemIndexB].volume);

  display->display();
}

void DisplayGameScreen(Adafruit_SSD1306* display, Item* items, uint8_t itemIndexA, uint8_t itemIndexB, uint8_t itemCount)
{
  display->clearDisplay();

  // Channel A Left arrow
  display->setCursor(4, 4);

  // Channel A Right arrow
  // Aligned to the right, withing the left half of the screen.
  display->setCursor(128/2 - 4 - 3, 4);

  // Item names
  // The width of half the screen is halfScreen = 128/2 - 4*2(border on both sides).
  // The area available for the text is textArea = halfScreen - 3*2(size of arrows on both sides) - 2*2(margin text and between arrows)
  // That allows gives us a practical size in pixels of 128/2 - 4*2 - 3*2 - 2*2 = 46px
  // 46px / 6.095(characted width) = 7.54 characters

  // Item A
  display->setTextSize(1);             
  display->setTextColor(WHITE);
  
  uint8_t nameLength = min(7, strlen(items[itemIndexA].name));
  display->setCursor(4 + 3 + 2 + nameX, 4);

  for(size_t i = 0; i < nameLength; i++)
    display->print(items[itemIndexA].name[i]);

  // Item B
  nameLength = min(7, strlen(items[itemIndexB].name));
  display->setCursor(128/2 + 3 + 2 + nameX, 4);

  for(size_t i = 0; i < nameLength; i++)
    display->print(items[itemIndexB].name[i]);

  // Volume bars
  // Item A
  uint8_t barY = map(items[itemIndexA].volume, 0, 100, 32 - 4, 4 + 8 + 4);
  display->fillRect(128/2 - 4 - 8,  barY, 8, 32 - 4 - barY , WHITE);

  // Item B
  barY = map(items[itemIndexB].volume, 0, 100, 32 - 4, 4 + 8 + 4);
  display->fillRect(128/2 + 4,  barY, 8, 32 - 4 - barY , WHITE);

  display->display();
}
