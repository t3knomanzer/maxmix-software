// ----------------------------------------------------------------------------
// Rotary Encoder Driver with Acceleration
// Supports Click, DoubleClick, Long Click
//
// (c) 2010 karl@pitrich.com
// (c) 2014 karl@pitrich.com
// 
// Timer-based rotary encoder logic by Peter Dannegger
// http://www.mikrocontroller.net/articles/Drehgeber
// ----------------------------------------------------------------------------

#ifndef __have__ClickEncoder_h__
#define __have__ClickEncoder_h__

// ----------------------------------------------------------------------------

#include <stdint.h>
#include <avr/io.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>
#include "Arduino.h"

// ----------------------------------------------------------------------------

class ClickEncoder
{
public:

public:
  ClickEncoder(uint8_t A, uint8_t B, 
               uint8_t stepsPerNotch = 1, bool active = LOW);

  void service(void);  
  int16_t getValue(void);

private:
  const uint8_t pinA;
  const uint8_t pinB;
  const bool pinsActive;
  volatile int16_t delta;
  volatile int16_t last;
  uint8_t steps;
};

// ----------------------------------------------------------------------------

#endif // __have__ClickEncoder_h__
