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
    LightingVolume(&devices[itemIndexOutput]);
  }
  else if(mode == MODE_APPLICATION)
  {
    LightingVolume(&sessions[itemIndexApp]);
  }
  else if(mode == MODE_GAME)
  {
    LightingVolumeDual(&sessions[itemIndexGameA]);
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

  uint16_t startOffset = 0;
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
void LightingVolume(Item * item)
{
  if (!item->isMuted)
  {
    // Circular lighting representing volume.
    uint32_t volAcc = ((uint32_t)item->volume * 255 * PIXELS_COUNT) / 100;
    for (int i=0; i<PIXELS_COUNT; i++)
    {
      uint32_t amp = min(volAcc, 255);
      volAcc -= amp;
      uint32_t color = (amp << 16) | (amp << 8) | amp;
      pixels.setPixelColor(i, color);
    }
  }
  else
  {
    // Red breathing.
    // All vars need to be signed for the formula to work.
    int32_t t = millis();
    int32_t period = 500;
    int32_t amp = (period - abs(t % (2*period) - period)) * 255 / period; // Triangular wave
    uint32_t color = (amp << 16);
    pixels.fill(color);
  }
}

//---------------------------------------------------------
void LightingVolumeDual(Item * item)
{
  // Circular lighting representing volume.
  uint32_t volAcc = ((uint32_t)item->volume * 255 * PIXELS_COUNT) / 100;
  for (int i=0; i<PIXELS_COUNT; i++)
  {
    uint32_t amp = min(volAcc, 255);
    volAcc -= amp;
    uint32_t color = (amp << 16) | (255 - amp);
    pixels.setPixelColor(i, color);
  }
}
