/*
  ButtonEvents - An Arduino library for catching tap, double-tap and press-and-hold events for buttons.
  
      Written by Edward Wright (fasteddy@thewrightspace.net)
        Available at https://github.com/fasteddy516/ButtonEvents

      Utilizes the Bounce2 library by Thomas O. Fredericks
        Available at https://github.com/thomasfredericks/Bounce2 

  Example Sketch - Bounce2 Passthru:
    This sketch demonstrates the ButtonEvents passthru methods that provide access to the underlying
    Bounce2 functionality if needed.  It will monitor a button connected to pin 7 and send strings
    to the serial monitor indicating when Bounce2 events are triggered.  
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

  // set the debounce time to 50ms
  myButton.interval(50);

  // initialize the arduino serial port and send a welcome message
  Serial.begin(9600);
  Serial.println("ButtonEvents 'Passthru' example started");
}


// this is the main loop, which will repeat forever
void loop() {

  // Update the ButtonEvents (and underlying Bounce2) instance.  Remember that this update() method
  // returns true if ButtonEvents *or* Bounce2 detects an event, so - if you are using the return
  // value in your code - the results will be slightly different than when using the Bounce2
  // update() method by itself.
  myButton.update();

  // things to do if the debounced signal from the button pin went high
  if (myButton.rose() == true) {
    Serial.println("Signal on button pin went HIGH");
    Serial.print("myButton.read() returns a value of ");
    Serial.println(myButton.read());
    Serial.println("---");
  }

  // things to do if the debounced signal from the button pin went low 
  if (myButton.fell() == true) {
    Serial.println("Signal on button pin went LOW");
    Serial.print("myButton.read() returns a value of ");
    Serial.println(myButton.read());
    Serial.println("---");
  }
}
