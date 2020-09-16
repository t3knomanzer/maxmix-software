#pragma once

#include "Structs.h"
#include "src/Adafruit_SSD1306/Adafruit_SSD1306.h"

namespace Display 
{
    static const uint8_t DISPLAY_TIMER_A = 0;
    static const uint8_t DISPLAY_TIMER_B = 1;
    
    void UpdateTimers(uint32_t delta);
    void ResetTimers();

    void Initialize(void);

    void Sleep(void);
    
    void SplashScreen(void);
    void InfoScreen(void);
    
    void DeviceSelectScreen(Item* item, bool isDefaultEndpoint, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex);
    void DeviceEditScreen(Item* item, uint8_t modeIndex);
    
    void ApplicationSelectScreen(Item* item, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex);
    void ApplicationEditScreen(Item* item, uint8_t modeIndex);

    void GameSelectScreen(Item* session, char channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex);
    void GameEditScreen(Item *itemA, Item *itemB, uint8_t modeIndex);
};