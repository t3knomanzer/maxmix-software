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
// *** FUNCTIONS
//********************************************************
//---------------------------------------------------------
// Draws the master mode screen
//---------------------------------------------------------
void DisplayMasterSelectScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex, uint8_t modeCount)
{
  display->clearDisplay();

  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionArrows(display, leftArrow, rightArrow);
  DrawSelectionItemName(display, name);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}

void DisplayMasterEditScreen(Adafruit_SSD1306* display, char* name, uint8_t volume, bool isMuted, uint8_t modeIndex, uint8_t modeCount)
{
  display->clearDisplay();

  DrawDotGroup(display, modeIndex, modeCount);
  DrawSelectionItemName(display, "VOL");
  DrawSelectionItemVolume(display, volume);
  DrawSelectionVolumeBar(display, volume, isMuted);

  display->display();
}
