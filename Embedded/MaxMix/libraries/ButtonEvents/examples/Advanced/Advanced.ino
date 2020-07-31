/*
  ButtonEvents - An Arduino library for catching tap, double-tap and press-and-hold events for buttons.
  
      Written by Edward Wright (fasteddy@thewrightspace.net)
        Available at https://github.com/fasteddy516/ButtonEvents

      Utilizes the Bounce2 library by Thomas O. Fredericks
        Available at https://github.com/thomasfredericks/Bounce2 

  Example Sketch - Advanced Usage:
    This sketch demonstrates the use of some of the additional methods provided in this library.  As in
    the 'Basic' example, it will monitor a button connected to pin 7 and send strings to the serial monitor
    indicating when events are triggered.  
 */

#include <ButtonEvents.h> // we have to include the library in order to use it

const byte buttonPin = 7; // our button will be connected to pin 7

ButtonEvents myButton; // create an instance of the ButtonEvents class to attach to our button


// this is where we run one-time setup code
void setup() {
  
  // configure the button pin as a digital input with internal pull-up resistor enabled
  pinMode(buttonPin, INPUT_PULLUP);  

  // attach our ButtonEvents instance to the button pin
  myButton.attach(buttonPin);

  // If your button is connected such that pressing it generates a high signal on the pin, you need to
  // specify that it is "active high"
  myButton.activeHigh();

  // If your button is connected such that pressing it generates a low signal on the pin, you can specify
  // that it is "active low", or don't bother, since this is the default setting anyway.
  myButton.activeLow();

  // By default, the raw signal on the input pin has a 35ms debounce applied to it.  You can change the
  // debounce time if necessary.
  myButton.debounceTime(15); //apply 15ms debounce

  // The double-tap detection window is set to 150ms by default.  Decreasing this value will result in
  // more responsive single-tap events, but requires really fast tapping to trigger a double-tap event.
  // Increasing this value will allow slower taps to still trigger a double-tap event, but will make
  // single-tap events more laggy, and can cause taps that were meant to be separate to be treated as
  // double-taps.  The necessary timing really depends on your use case, but I have found 150ms to be a
  // reasonable starting point.  If you need to change the double-tap detection window, you can do so
  // as follows:
  myButton.doubleTapTime(250); // set double-tap detection window to 250ms
  
  // The hold duration can be increased to require longer holds before an event is triggered, or reduced to
  // have hold events trigger more quickly.
  myButton.holdTime(2000); // require button to be held for 2000ms before triggering a hold event
   
  // initialize the arduino serial port and send a welcome message
  Serial.begin(9600);
  Serial.println("ButtonEvents 'Advanced' example started");
}


// this is the main loop, which will repeat forever
void loop() {

  // The update() method returns true if an event or state change occurred.  It serves as a passthru
  // to the Bounce2 library update() function as well, so it will stll return true if a press/release
  // is detected but has not triggered a tap/double-tap/hold event
  if (myButton.update() == true) {

    // The event() method returns tap, doubleTap, hold or none depending on which event was detected
    // the last time the update() method was called.  The following code accomplishes the same thing
    // we did in the 'Basic' example, but I personally prefer this arrangement.
    switch(myButton.event()) {
      
      // things to do if the button was tapped (single tap)
      case (tap) : {
        Serial.println("TAP event detected");          
        break;
      }

      // things to do if the button was double-tapped
      case (doubleTap) : {
        Serial.println("DOUBLE-TAP event detected");
        break;
      }
   
      // things to do if the button was held
      case (hold) : {
        Serial.println("HOLD event detected");
        break;
      }
      
    }
  }
}
