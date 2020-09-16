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
  if(stateDisplay == STATE_DISPLAY_SLEEP)
  {
    LightingBlackOut();
  }
  else if(sessionCount == 0)
  {
    LightingCircularFunk();
  }
  else if(mode == MODE_OUTPUT)
  {
    LightingVolume(&devicesOutput[itemIndexOutput], &settings.volumeMinColor, &settings.volumeMaxColor);
  }
  else if(mode == MODE_APPLICATION)
  {
    LightingVolume(&sessions[itemIndexApp], &settings.volumeMinColor, &settings.volumeMaxColor);
  }
  else if(mode == MODE_GAME)
  {
    LightingVolume(&sessions[itemIndexGameA], &settings.mixChannelAColor, &settings.mixChannelBColor);
  }

  // Push the colors to the pixels strip
  pixels.show();
}

//---------------------------------------------------------
void LightingBlackOut()
{
  // All black
  pixels.clear();
}

//---------------------------------------------------------
void LightingCircularFunk()
{
  uint32_t t = millis();
  uint16_t hue = t * 20;
  uint32_t rgbColor = pixels.ColorHSV(hue);
  uint16_t period = 500;

  uint8_t startOffset = 0;
  if ((t % period) > (period / 2))
  {
    startOffset = 1;
  }

  pixels.clear();
  pixels.setPixelColor(startOffset, rgbColor);
  pixels.setPixelColor(startOffset+2, rgbColor);
  pixels.setPixelColor(startOffset+4, rgbColor);
  pixels.setPixelColor(startOffset+6, rgbColor);
}

//---------------------------------------------------------
void LightingVolume(Item * item, Color * c1, Color * c2)
{
  if (!item->isMuted) {
    // Dual colors circular lighting representing the volume.
    uint32_t volAcc = ((uint32_t)item->volume * 255 * PIXELS_COUNT) / 100;
    for (int i=0; i<PIXELS_COUNT; i++)
    {
      uint32_t amp = min(volAcc, 255);
      volAcc -= amp;

      // Linear interpolation to get the final color of each pixel.
      Color c = LerpColor(c1, c2, amp);
      pixels.setPixelColor(i, c.r, c.g, c.b);

    }
  }
  else
  {
    // Fast pulse between the first and second color.
    // Both 't' and 'period' need to be signed for the formula to work.
    int32_t t = millis();
    int32_t period = 500;
    uint8_t amp = (period - abs(t % (2*period) - period)) * 255 / period; // Triangular wave

    Color c = LerpColor(c1, c2, amp);
    uint32_t color32 = ((uint32_t)c.r << 16) | ((uint32_t)c.g << 8) | (uint32_t)c.b;
    pixels.fill(color32);
  }
}

//---------------------------------------------------------
Color LerpColor(Color * c1, Color * c2, uint8_t coeff)
{

  SQ15x16 amount = SQ15x16(coeff) / 255;
  SQ15x16 invAmount = 1 - amount;
  uint8_t r = ((invAmount * c1->r) + (amount * c2->r)).getInteger();
  uint8_t g = ((invAmount * c1->g) + (amount * c2->g)).getInteger();
  uint8_t b = ((invAmount * c1->b) + (amount * c2->b)).getInteger();

  return {r, g, b};
}
