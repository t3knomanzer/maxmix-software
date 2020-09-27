#pragma once

#include "Config.h"
#include <Adafruit_SSD1306/Adafruit_SSD1306.h>

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

    void DeviceSelectScreen(SessionData *item, bool leftArrow, bool rightArrow, uint8_t modeIndex);
    void DeviceEditScreen(SessionData *item, const char *label, uint8_t modeIndex);

    void ApplicationSelectScreen(SessionData *item, bool leftArrow, bool rightArrow, uint8_t modeIndex);
    void ApplicationEditScreen(SessionData *item, uint8_t modeIndex);

    void GameSelectScreen(SessionData *session, char channel, bool leftArrow, bool rightArrow, uint8_t modeIndex);
    void GameEditScreen(SessionData *itemA, SessionData *itemB, uint8_t modeIndex);
}; // namespace Display