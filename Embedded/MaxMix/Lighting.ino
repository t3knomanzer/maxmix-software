#include "LightingEffects.h"

void UpdateLighting()
{
    Color pixels[PIXELS_COUNT];
    LightingEffects::Render(g_SessionInfo.mode,
        g_Sessions[SessionIndex::INDEX_CURRENT], g_Sessions[SessionIndex::INDEX_ALTERNATE],
        g_Settings, g_LightingSettings, g_DisplayAsleep, g_Now, pixels);
    for (uint8_t i = 0; i < PIXELS_COUNT; i++)
        g_Pixels.setPixelColor(i, pixels[i].r, pixels[i].g, pixels[i].b);
    g_Pixels.show();
}
