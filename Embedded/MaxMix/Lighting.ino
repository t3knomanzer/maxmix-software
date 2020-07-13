//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
//********************************************************

//********************************************************
// *** FUNCTIONS
//********************************************************
void SetPixelsColor(Adafruit_NeoPixel* pixels, uint8_t r, uint8_t g, uint8_t b)
{
  pixels->setPixelColor(0, pixels->Color(r, g, b)); // BACK
  pixels->setPixelColor(3, pixels->Color(r, g, b)); // FRONT-RIGHT
  pixels->setPixelColor(5, pixels->Color(r, g, b)); // FRONT-LEFT
  pixels->show();
}