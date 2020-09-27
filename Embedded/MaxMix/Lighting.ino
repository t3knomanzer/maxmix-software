//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
//********************************************************

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateLighting()
{
    if (g_DisplayAsleep)
    {
        LightingBlackOut();
    }
    else if (g_SessionInfo.mode == DisplayMode::MODE_SPLASH)
    {
        LightingCircularFunk();
    }
    else if (g_SessionInfo.mode == DisplayMode::MODE_GAME)
    {
        LightingVolume(&g_Sessions[SessionIndex::INDEX_CURRENT], &g_Settings.mixChannelAColor, &g_Settings.mixChannelBColor);
    }
    else
    {
        LightingVolume(&g_Sessions[SessionIndex::INDEX_CURRENT], &g_Settings.volumeMinColor, &g_Settings.volumeMaxColor);
    }
    // Push the colors to the g_Pixels strip
    g_Pixels.show();
}

//---------------------------------------------------------
void LightingBlackOut()
{
    // All black
    g_Pixels.clear();
}

//---------------------------------------------------------
void LightingCircularFunk()
{
    uint32_t t = millis();
    uint16_t hue = t * 20;
    uint32_t rgbColor = g_Pixels.ColorHSV(hue);

    g_Pixels.clear();
    for (uint8_t i = (t % 500) / 250; i < PIXELS_COUNT; i += 2)
    {
        g_Pixels.setPixelColor(i, rgbColor);
    }
}

//---------------------------------------------------------
void LightingVolume(SessionData *item, Color *c1, Color *c2)
{
    if (!item->data.isMuted)
    {
        // Dual colors circular lighting representing the volume.
        uint32_t volAcc = ((uint32_t)item->data.volume * 255 * PIXELS_COUNT) / 100;
        for (int i = 0; i < PIXELS_COUNT; i++)
        {
            uint32_t amp = min(volAcc, 255);
            volAcc -= amp;

            // Linear interpolation to get the final color of each pixel.
            Color c = LerpColor(c1, c2, amp);
            g_Pixels.setPixelColor(i, c.r, c.g, c.b);
        }
    }
    else
    {
        // Fast pulse between the first and second color.
        // Both 't' and 'period' need to be signed for the formula to work.
        int32_t t = millis();
        int32_t period = 500;
        uint8_t amp = (period - abs(t % (2 * period) - period)) * 255 / period; // Triangular wave

        Color c = LerpColor(c1, c2, amp);
        uint32_t color32 = ((uint32_t)c.r << 16) | ((uint32_t)c.g << 8) | (uint32_t)c.b;
        g_Pixels.fill(color32);
    }
}

//---------------------------------------------------------
Color LerpColor(Color *c1, Color *c2, uint8_t coeff)
{

    SQ15x16 amount = SQ15x16(coeff) / 255;
    SQ15x16 invAmount = 1 - amount;
    uint8_t r = ((invAmount * c1->r) + (amount * c2->r)).getInteger();
    uint8_t g = ((invAmount * c1->g) + (amount * c2->g)).getInteger();
    uint8_t b = ((invAmount * c1->b) + (amount * c2->b)).getInteger();

    return {r, g, b};
}
