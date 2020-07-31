/* * * * * * * * * * * * * * * * * * * * * * * * * * * *
Written by Edward Wright (fasteddy@thewrightspace.net)
Utilizes the Bounce2 library (https://github.com/thomasfredericks/Bounce2) by Thomas O Fredericks (tof@t-o-f.info)
* * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include "Arduino.h"
#include "ButtonEvents.h"

// default (and only) constructor
ButtonEvents::ButtonEvents() {
  debouncedButton.interval(DEFAULT_DEBOUNCE_MS);
  doubleTapTime_ms = DEFAULT_DOUBLETAP_MS;
  holdTime_ms = DEFAULT_HOLD_MS;
  isActiveLow = DEFAULT_ACTIVE_LOW;
  eventTime_ms = 0; // initialize button timestamps and states...
  buttonState = idle;
  buttonEvent = none;
}

// passthru to Bounce2 attach() method
void ButtonEvents::attach(int pin) {
  debouncedButton.attach(pin);
}

// passthru to Bounce2 attach() overload
void ButtonEvents::attach(int pin, int mode) {
  debouncedButton.attach(pin, mode);
}

// set button mode to active high
void ButtonEvents::activeHigh() {
  isActiveLow = false;
}

// set button mode to active low
void ButtonEvents::activeLow() {
  isActiveLow = true;
}

// alias/passthru to Bounce2 interval() method
void ButtonEvents::debounceTime(unsigned int debounce_ms) {
  debouncedButton.interval(debounce_ms);
}

// method to set the 'doubleTap' event detection window
void ButtonEvents::doubleTapTime(unsigned int doubleTap_ms) {
  doubleTapTime_ms = doubleTap_ms;
}

// method to set the amount of time that must elapse to trigger a 'hold' event
void ButtonEvents::holdTime(unsigned int hold_ms) {
  holdTime_ms = hold_ms;
}

// passthru to Bounce2 interval() method
void ButtonEvents::interval(unsigned int interval_ms) {
  debouncedButton.interval(interval_ms);
}

// returns true if button was pressed (accounts for active high/low)
bool ButtonEvents::buttonPressed() {
  if (isActiveLow && debouncedButton.fell()) return true;
  else if (!isActiveLow && debouncedButton.rose()) return true;
  return false;
}

// returns true if button was released (accounts for active high/low)
bool ButtonEvents::buttonReleased() {
  if (isActiveLow && debouncedButton.rose()) return true;
  else if (!isActiveLow && debouncedButton.fell()) return true;
  return false;
}

// calls the Bounce2 update() method, then runs button event detection logic
bool ButtonEvents::update() {
  bool passthruState = debouncedButton.update(); // update debounced button state

  if (buttonPressed()) {
    // if the button was previously idle, store the press time and update the button state
    if (buttonState == idle) {
      eventTime_ms = millis();
      buttonState = pressed;
    }

    // if the button was in a released state (waiting for a double tap), update the button
    // state and indicate that a double tap event occurred
    else if (buttonState == released) {
      buttonState = idle;
      buttonEvent = doubleTap;
      return true;
    }
  }

  else if (buttonReleased()) {
    // if the button was in a pressed state, store the release time and update the button state
    if (buttonState == pressed) {
      eventTime_ms = millis();
      buttonState = released;
    }
  }

  // if the button is currently in a pressed state...
  if (buttonState == pressed) {
    // if the specified hold time has been reached or passed, update the button state and
    // indicate that a hold event occurred
    if ((millis() - eventTime_ms) >= holdTime_ms) {
      buttonState = idle;
      buttonEvent = hold;
      return true;
    }
  }

  // if the button is currently in a released state...
  else if (buttonState == released) {
    // if the specified double tap time has been reached or passed, update the button state
    // and indicate that a (single) tap event occurred
    if ((millis() - eventTime_ms) >= doubleTapTime_ms) {
      buttonState = idle;
      buttonEvent = tap;
      return true;
    }
  }

  // if we get to this point, indicate that no button event occurred in this cycle
  buttonEvent = none;
  return passthruState;
}

// resets the saved button state to idle
void ButtonEvents::reset() {
  buttonState = idle;
  buttonEvent = none;
}

// sets the button event timestamp to the current value of millis()
void ButtonEvents::retime() {
  eventTime_ms = millis();
  
  // prevent double-tap from triggering after a call to retime() - only taps and holds
  // can be effectivley retimed after a delay
  if (buttonState == released) {
    eventTime_ms += doubleTapTime_ms;
  }  
}

// returns the last triggered event
ButtonEvent ButtonEvents::event() {
  return buttonEvent;
}

// returns true if button was tapped
bool ButtonEvents::tapped() {
  return (buttonEvent == tap);
}

// returns true if button was double-tapped
bool ButtonEvents::doubleTapped() {
  return (buttonEvent == doubleTap);
}

// returns true if button was held
bool ButtonEvents::held() {
  return (buttonEvent == hold);
}

// passthru to Bounce2 read() method
bool ButtonEvents::read() {
  return debouncedButton.read();
}

// passthru to Bounce2 fell() method
bool ButtonEvents::fell() {
  return debouncedButton.fell();
}

// passthru to Bounce2 rose() method
bool ButtonEvents::rose() {
  return debouncedButton.rose();
}
