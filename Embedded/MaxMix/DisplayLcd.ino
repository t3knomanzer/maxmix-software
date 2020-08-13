//********************************************************
// PROJECT: MAXMIX
// ORIGINAL AUTHOR: Ruben Henares
// MODIFIED BY: Collin Ostrowski 
//
// ORIGINAL AUTHOR EMAIL: rhenares0@gmail.com
//
//********************************************************

#ifdef SCREEN_LCD

//********************************************************
// *** INCLUDES
//********************************************************
#include <Wire.h>
#include "LogoLcd.h"

//********************************************************
// *** FUNCTIONS
//********************************************************
LiquidCrystal* InitializeDisplay()
{
  LiquidCrystal* display = new LiquidCrystal(RS, EN, D4, D5, D6, D7);
  display->begin(AMOUNT_OF_COLS, AMOUNT_OF_ROWS);
  display->clear();

  return display;
}

void DisplaySleep(LiquidCrystal* display)
{
  display->clear();
}

//---------------------------------------------------------
// Draws the splash screen logo
//---------------------------------------------------------
void DisplaySplashScreen(LiquidCrystal* display)
{
  display->createChar(0, TopLeftAChar);
  display->createChar(1, TopRightAChar);

  display->createChar(2, TopLeftXChar);
  display->createChar(3, TopRightXChar);
  display->createChar(4, BottomLeftXChar); //also Right Top of M and Left Bottom of A
  display->createChar(5, BottomRightXChar); //also Left Top of M and Right Bottom of A

  display->createChar(6, LeftIChar);
  display->createChar(7, RightIChar);

  // Draw Top Half
  display->setCursor(0, 0);
  display->print("  ");
  display->write(byte(5)); // Top Left M
  display->write(byte(4)); // Top Right M
  display->write(byte(0)); // Top Left A
  display->write(byte(1)); // Top Right A
  display->write(byte(2)); // Top Left X
  display->write(byte(3)); // Top Right X
  display->write(byte(5)); // Top Left M
  display->write(byte(4)); // Top Right M
  display->write(byte(6)); // Top Left I
  display->write(byte(7)); // Top Right I
  display->write(byte(2)); // Top Left X
  display->write(byte(3)); // Top Right X

  // Draw Bottom Half
  display->setCursor(0, 1);
  display->print("  ");
  display->write(byte(255)); // Bottom Left M
  display->write(byte(255)); // Bottom Right M
  display->write(byte(4)); // Bottom Left A
  display->write(byte(5)); // Bottom Right A
  display->write(byte(4)); // Bottom Left X
  display->write(byte(5)); // Bottom Right X
  display->write(byte(255)); // Bottom Left M
  display->write(byte(255)); // Bottom Right M
  display->write(byte(6)); // Bottom Left I
  display->write(byte(7)); // Bottom Right I
  display->write(byte(4)); // Bottom Left X
  display->write(byte(5)); // Bottom Right X
}

//---------------------------------------------------------
// Draws the master mode screen
//---------------------------------------------------------
void DisplayMasterSelectScreen(LiquidCrystal* display, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  display->clear();
  
  display->setCursor(0, 0);
  display->print("MASTER VOL: ");
  
  if(isMuted){
    display->print("MUTE");
    display->setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Muted Volume Bar
      display->print("-");
    }
  }
  else{
    display->print(volume);
    display->setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Volume Bar
      display->write(byte(255));
    }
  }
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayApplicationSelectScreen(LiquidCrystal* display, char* name, uint8_t volume, bool isMuted, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  display->clear();
  
  display->setCursor(0, 0);
  display->print("Name: ");
  int length = min(DISPLAY_CHAR_MAX_X2, strlen(name));
  for(size_t i = 0; i < length; i++)
    display->print(name[i]);

  display->setCursor(0, 1);
  display->print("VOL: ");
  display->print(volume);
  if(isMuted){
    display->print(" MUTED");
  }
}

//---------------------------------------------------------
// Draws the Application mode selection screen
//---------------------------------------------------------
void DisplayGameSelectScreen(LiquidCrystal* display, char* name, uint8_t volume, bool isMuted, char* channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  display->clear();

  display->setCursor(0, 0);
  display->print("Name ");
  display->print(channel);
  display->print(": ");
  int length = min(DISPLAY_CHAR_MAX_X2, strlen(name));  
  for(size_t i = 0; i < length; i++)
    display->print(name[i]);

  display->setCursor(0, 1);
  display->print("VOL: ");
  display->print(volume);
}

//---------------------------------------------------------
// Draws the Application mode Edit screen
//---------------------------------------------------------
void DisplayApplicationEditScreen(LiquidCrystal* display, char* name, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  display->clear();

  display->setCursor(0, 0);
  uint8_t length = min(DISPLAY_GAME_EDIT_CHAR_MAX, strlen(name));
															 
  for(size_t i = 0; i < length; i++)
    display->print(name[i]);
    
  if(isMuted){
    display->print(":");
    display->print("MUTE");
    display->setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Muted Volume Bar
      display->print("-");
    }
  }
  else{
    display->print(": ");
    display->print(volume);
    display->setCursor(0, 1);
    for(int c=0; c<(volume/VOLUME_ON_SCREEN); c++){ // Creates Volume Bar
      display->write(byte(255));
    }
  }
}

//---------------------------------------------------------
// Draws the Game mode screen
//---------------------------------------------------------
void DisplayGameEditScreen(LiquidCrystal* display, char* nameA, char* nameB, uint8_t volumeA, uint8_t volumeB, bool isMutedA, bool isMutedB, uint8_t modeIndex, uint8_t modeCount)
{
  display->clear();
  
  display->setCursor(0, 0);
  uint8_t length = min(DISPLAY_GAME_EDIT_CHAR_MAX, strlen(nameA));
  for(size_t i = 0; i < length; i++)
    display->print(nameA[i]);
    
  display->print(": ");
  display->print(volumeA);
  display->setCursor(0, 1);

  length = min(DISPLAY_GAME_EDIT_CHAR_MAX, strlen(nameB));
  for(size_t i = 0; i < length; i++)
    display->print(nameB[i]);
    
  display->print(": ");
  display->print(volumeB);
}

#endif
