//********************************************************
// PROJECT: MAXMIX
// ORIGINAL AUTHOR: Ruben Henares
// MODIFIED BY: Collin Ostrowski 
//
// ORIGINAL AUTHOR EMAIL: rhenares0@gmail.com
//
//********************************************************

//********************************************************
// *** INCLUDES
//********************************************************
#include <Wire.h>
#include "Logo.h"
#include <LiquidCrystal.h>

LiquidCrystal lcd(RS, EN, D4, D5, D6, D7);
//********************************************************
// *** FUNCTIONS
//********************************************************
Adafruit_SSD1306* InitializeDisplay()
{
  lcd.begin(AMOUNT_OF_COLS, AMOUNT_OF_ROWS);
  lcd.clear();

  //Truely this block of code is not needed for the LCD but so MaxMix.ino stays the same this is needed.																															
  Adafruit_SSD1306* display = new Adafruit_SSD1306(DISPLAY_WIDTH, DISPLAY_HEIGHT, &Wire, DISPLAY_RESET);
  display->begin(SSD1306_SWITCHCAPVCC, DISPLAY_ADDRESS);
  display->setRotation(2);

  return display;
}

void DisplaySleep(Adafruit_SSD1306* display)
{
  lcd.clear();
}

//---------------------------------------------------------
// Draws the splash screen logo
//---------------------------------------------------------
void DisplaySplashScreen(Adafruit_SSD1306* display)
{
  lcd.createChar(0, TopLeftAChar);
  lcd.createChar(1, TopRightAChar);

  lcd.createChar(2, TopLeftXChar);
  lcd.createChar(3, TopRightXChar);
  lcd.createChar(4, BottomLeftXChar); //also Right Top of M and Left Bottom of A
  lcd.createChar(5, BottomRightXChar); //also Left Top of M and Right Bottom of A

  lcd.createChar(6, LeftIChar);
  lcd.createChar(7, RightIChar);

  // Draw Top Half
  lcd.setCursor(0, 0);
  lcd.print("  ");
  lcd.write(byte(5)); // Top Left M
  lcd.write(byte(4)); // Top Right M
  lcd.write(byte(0)); // Top Left A
  lcd.write(byte(1)); // Top Right A
  lcd.write(byte(2)); // Top Left X
  lcd.write(byte(3)); // Top Right X
  lcd.write(byte(5)); // Top Left M
  lcd.write(byte(4)); // Top Right M
  lcd.write(byte(6)); // Top Left I
  lcd.write(byte(7)); // Top Right I
  lcd.write(byte(2)); // Top Left X
  lcd.write(byte(3)); // Top Right X

  // Draw Bottom Half
  lcd.setCursor(0, 1);
  lcd.print("  ");
  lcd.write(byte(255)); // Bottom Left M
  lcd.write(byte(255)); // Bottom Right M
  lcd.write(byte(4)); // Bottom Left A
  lcd.write(byte(5)); // Bottom Right A
  lcd.write(byte(4)); // Bottom Left X
  lcd.write(byte(5)); // Bottom Right X
  lcd.write(byte(255)); // Bottom Left M
  lcd.write(byte(255)); // Bottom Right M
  lcd.write(byte(6)); // Bottom Left I
  lcd.write(byte(7)); // Bottom Right I
  lcd.write(byte(4)); // Bottom Left X
  lcd.write(byte(5)); // Bottom Right X
}

//---------------------------------------------------------
// Draws the master mode screen
//---------------------------------------------------------
void DisplayMasterSelectScreen(Adafruit_SSD1306* display, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  lcd.clear();
  
  lcd.setCursor(0, 0);
  lcd.print("MASTER VOL: ");
  
  if(isMuted){
    lcd.print("MUTE");
    lcd.setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Muted Volume Bar
      lcd.print("-");
    }
  }
  else{
    lcd.print(volume);
    lcd.setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Volume Bar
      lcd.write(byte(255));
    }
  }
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayApplicationSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  lcd.clear();
  
  lcd.setCursor(0, 0);
  lcd.print("Name: ");
  int length = min(DISPLAY_CHAR_MAX_X2, strlen(name));
  for(size_t i = 0; i < length; i++)
    lcd.print(name[i]);

  lcd.setCursor(0, 1);
  lcd.print("VOL: ");
  lcd.print(volume);
  if(isMuted){
    lcd.print(" MUTED");
  }
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayGameSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, char* channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  lcd.clear();

  lcd.setCursor(0, 0);
  lcd.print("Name ");
  lcd.print(channel);
  lcd.print(": ");
  int length = min(DISPLAY_CHAR_MAX_X2, strlen(name));  
  for(size_t i = 0; i < length; i++)
    lcd.print(name[i]);

  lcd.setCursor(0, 1);
  lcd.print("VOL: ");
  lcd.print(volume);
}

//---------------------------------------------------------
// Draws the Application mode Edit screen
//---------------------------------------------------------
void DisplayApplicationEditScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  lcd.clear();

  lcd.setCursor(0, 0);
  uint8_t length = min(DISPLAY_GAME_EDIT_CHAR_MAX, strlen(name));
															 
  for(size_t i = 0; i < length; i++)
    lcd.print(name[i]);
    
  if(isMuted){
    lcd.print(":");
    lcd.print("MUTE");
    lcd.setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Muted Volume Bar
      lcd.print("-");
    }
  }
  else{
    lcd.print(": ");
    lcd.print(volume);
    lcd.setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Volume Bar
      lcd.write(byte(255));
    }
  }
}

//---------------------------------------------------------
// Draws the Game mode screen
//---------------------------------------------------------
void DisplayGameEditScreen(Adafruit_SSD1306* display, char* nameA, char* nameB, uint8_t volumeA, uint8_t volumeB, bool isMutedA, bool isMutedB, uint8_t modeIndex, uint8_t modeCount)
{
  lcd.clear();
  
  lcd.setCursor(0, 0);
  uint8_t length = min(DISPLAY_GAME_EDIT_CHAR_MAX, strlen(nameA));
  for(size_t i = 0; i < length; i++)
    lcd.print(nameA[i]);
    
  lcd.print(": ");
  lcd.print(volumeA);
  lcd.setCursor(0, 1);

  length = min(DISPLAY_GAME_EDIT_CHAR_MAX, strlen(nameB));
  for(size_t i = 0; i < length; i++)
    lcd.print(nameB[i]);
    
  lcd.print(": ");
  lcd.print(volumeB);
}