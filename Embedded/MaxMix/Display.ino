//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
// 
//
//
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
