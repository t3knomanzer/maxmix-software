# ButtonEvents

  ###### An Arduino library for catching tap, double-tap and press-and-hold events for buttons

  Written by Edward Wright (fasteddy@thewrightspace.net)

  Available at https://github.com/fasteddy516/ButtonEvents

  _ButtonEvents utilizes (and thus depends on) the Bounce2 library by Thomas O. Fredericks, which
  is available at https://github.com/thomasfredericks/Bounce2._


# Description

  ButtonEvents is a library for Arduino that provides methods for detecting **tap**,
  **double-tap** and **press-and-hold** events associated with buttons connected
  to pins configured as digital inputs.


# Installation

  Put the "ButtonEvents" folder in your Arduino "libraries" folder. To identify
  this location from the Arduino IDE, open "menubar-> File -> Preferences".

  Select "menubar -> Sketch -> Import Library -> ButtonEvents" to import the library
  into your sketch. An "#include ButtonEvents.h" line will appear at the top of
  your Sketch.

  **In order to use the ButtonEvents library, the Bounce2 library is required, and must
  also be installed.  A link to the Bounce2 library is included at the top of this file.**


# Details

  ButtonEvents can detect **tap**, **double-tap** and **press-and-hold** events
  based on transitions that occur on a pin configured as a digital input - generally
  as the result of a button press.

  * A **tap** event is triggered after the button has been *released*, and the
    double-tap detection window has elapsed with no further button presses.

  * A **double-tap** event is triggered after the button has been released and is
    then *pressed* again before the double-tap detection window has elapsed.

  * A **press-and-hold** event is triggered after the button has been *pressed
    and held* down for the hold duration.


  The raw signal on the input pin is debounced using the Bounce2 library.  Unless
  otherwise specified, the signal on the pin is assumed to be active-low, meaning
  that when the button is pressed is generates a low signal on the pin.

  A newly created ButtonEvent instance assumes the following default settings:
  * 35ms debounce for raw input signal on pin
  * 150ms double-tap detection window
  * 500ms hold duration
  * Active-low signal on input pin


  All of these settings can be adjusted using the methods described later in this
  file.


# Examples

  A number of code examples are included with this library, and can be accessed
  through the Arduino IDE by opening "menubar-> File -> Examples -> ButtonEvents"
  and choosing the desired example sketch.


# Types

  ButtonEvents uses two enumerated types for code readability:

  `enum ButtonState { idle, pressed, released };`

  `enum ButtonEvent { none, tap, doubleTap, hold };`


# Methods

  #### `ButtonEvents()`
   * Instantiates a ButtonEvents object. (See *Basic* example sketch.)


  #### `void attach(int pin)`
   * Attaches a ButtonEvents instance to a pin.  The pin needs to be configured
     using `pinMode()` before ButtonEvents is attached to it. (See *Basic* example sketch)


  #### `void activeHigh()`
   * Sets the input processing mode to active high. (See *Advanced* example sketch)


  #### `void activeLow()`
   * Sets the input processing mode to active low. (See *Advanced* example sketch)


  #### `void debounceTime(unsigned int debounce_ms)`
   * Sets the debounce time for the raw input signal.  (See *Advanced* example sketch)


  #### `void doubleTapTime(unsigned int doubleTap_ms)`
   * Sets the double-tap event detection window.  (See *Advanced* example sketch)


  #### `void holdTime(unsigned int hold_ms)`
   * Sets the time required to trigger a hold event.  (See *Advanced* example sketch)


  #### `bool update()`
   * Like the Bounce2 library, ButtonEvents does not use interrupts.  Instead,
     you have to "update" the instance in order to detect events, and the updates
     need to be done as frequently as possible - presumably within your `loop()`.
     The `update()` method updates the instance and returns true (1) if a button
     event occurred, **or** if the pin state changed. False (0) if no event or
     state change occurred. `update()` should only be called once per loop().
     (See *Basic* example sketch)


  #### `void reset()`
   * Resets the saved button state to idle. (See *TimingConsiderations* example sketch)


  #### `void retime()`
   * Sets the button event timestamp to the current value of `millis()`. (See *TimingConsiderations* example sketch)


  #### `ButtonEvent event()`
   * Returns the button event detected during the last `update()` call. (See *Advanced* example sketch)


  #### `bool tapped()`
   * Returns true if the 'tap' event was detected. (See *Basic* example sketch)


  #### `bool doubleTapped()`
   * Returns true if the 'doubleTap' event was detected. (See *Basic* example sketch)


  #### `bool held()`
   * Returns true if the 'held' event was detected. (See *Basic* example sketch)

# Bounce2 Passthru Methods

  If access to underlying Bounce2 library functionality is required, the following
  passthru methods can be used.  Please refer to the Bounce2 documentation for
  information regarding these methods:

  #### `void interval(unsigned int interval_ms)`


  #### `bool read()`


  #### `bool fell()`


  #### `bool rose()`
