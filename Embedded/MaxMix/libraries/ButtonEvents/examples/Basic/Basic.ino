/*
  ButtonEvents - An Arduino library for catching tap, double-tap and press-and-hold events for buttons.
  
      Written by Edward Wright (fasteddy@thewrightspace.net)
        Available at https://github.com/fasteddy516/ButtonEvents

      Utilizes the Bounce2 library by Thomas O. Fredericks
        Available at https://github.com/thomasfredericks/Bounce2 

  Example Sketch - Basic Usage:
    This sketch demonstrates the most simple use of the ButtonEvents library.  It will monitor a button
    connected to pin 7 and send strings to the serial monitor indicating when one of the supported
    button events has occured.  The events that can be detected are:
         
      1) A 'tap' event is triggered after the button has been released, and the double-tap detection
         window has elapsed with no further button presses.

      2) A 'double-tap' event is triggered after the button has been released and is then pressed again
         before the double-tap detection window has elapsed.  The default double-tap detection window
         duration is 150ms.

      3) A 'press-and-hold' event is triggered after the button has been pressed and held down for the hold
         duration.  The default hold duration is set to 500ms.
        
    The raw signal on the input pin has a 35ms debounce applied to it (using the Bounce2 library) by
    default.  Unless otherwise specified, the signal on the input is assumed to be active low, meaning
    that when the button is pressed is generates a low signal on the pin.

    The double-tap detection window, hold duration, debounce time and input type (active low/high) can
    all be adjusted as needed - please see the 'Advanced' example sketch included with this library for
    details.  For this 'Basic' example, we will stick with the default settings.  
 */
 
#include <ButtonEvents.h> // we have to include the library in order to use it

const byte buttonPin = 7; // our button will be connected to pin 7

ButtonEvents myButton; // create an instance of the ButtonEvents class to attach to our button


// this is where we run one-time setup code
void setup() {
  
  // Configure the button pin as a digital input with internal pull-up resistor enabled.  This pin
  // setup needs to be done before we attach our ButtonEvents instance to the pin.
  pinMode(buttonPin, INPUT_PULLUP);  
  
  // attach our ButtonEvents instance to the button pin
  myButton.attach(buttonPin);

  // initialize the arduino serial port and send a welcome message
  Serial.begin(9600);
  Serial.println("ButtonEvents 'basic' example started");
}


// this is the main loop, which will repeat forever
void loop() {

  // the update() method is where all the magic happens - it needs to be called regularly in order
  // to function correctly, so it should be part of the main loop.  
  myButton.update();
  
  // things to do if the button was tapped (single tap)
  if (myButton.tapped() == true) {
    Serial.println("TAP event detected");          
  }

  // things to do if the button was double-tapped
  if (myButton.doubleTapped() == true) {
    Serial.println("DOUBLE-TAP event detected");
  }
  
  // things to do if the button was held
  if (myButton.held() == true) {
        Serial.println("HOLD event detected");
  }  
}
