/*
  ButtonEvents - An Arduino library for catching tap, double-tap and press-and-hold events for buttons.
  
      Written by Edward Wright (fasteddy@thewrightspace.net)
        Available at https://github.com/fasteddy516/ButtonEvents

      Utilizes the Bounce2 library by Thomas O. Fredericks
        Available at https://github.com/thomasfredericks/Bounce2 

  Example Sketch - Timing Considerations:
    This sketch demonstrates how to avoid/mitigate some issues that can occur when the possibility of
    large delays between update() method calls exists in your code.  It uses the same button on pin 7
    that we have utilized in the other examples, along with another input on pin 8 that we will pretend
    is connected to an active low sensor device.  Note that this example isn't really meant to be executed
    and observed on an Arduino, it is meant to be read through.
 */
 
#include <ButtonEvents.h> // we have to include the library in order to use it

const byte buttonPin = 7; // our button will be connected to pin 7
const byte sensorPin = 8; // our pretend sensor will be "connected" to pin 8

ButtonEvents myButton; // create an instance of the ButtonEvents class to attach to our button


// this is where we run one-time setup code
void setup() {

  // configure our button and sensor input pins
  pinMode(buttonPin, INPUT_PULLUP);  
  pinMode(sensorPin, INPUT_PULLUP);

  // attach our ButtonEvents instance to the button pin
  myButton.attach(buttonPin);

  // initialize the arduino serial port and send a welcome message
  Serial.begin(9600);
  Serial.println("ButtonEvents 'Timing Considerations' example started");
}


// Because the ButtonEvents library uses comparisons based on millis() rather than interrupts, it
// is critical that the update() method is called frequently in order to process events properly.
// There may be instances in your code where execution is significantly delayed between calls to
// the update() method, causing button events to trigger improperly/unexpectedly.  Consider the
// following main loop:
void loop() {

  // This is a tightly-packed version of our standard event detection printout logic.  The update()
  // method is called as part of the if() statement every time the loop executes.   
  if (myButton.update()) {
    switch(myButton.event()) {
      case (tap) : { Serial.print("TAP"); break; }
      case (doubleTap) : { Serial.print("DOUBLE-TAP"); break; }
      case (hold) : { Serial.print("HOLD"); break; }
    }
    Serial.println(" event detected");
  }

  // Imagine that our sensor connected to pin 8 requires that we delay the main loop for 2 full
  // seconds whenever it pulls the input pin low:
  if (digitalRead(sensorPin) == LOW) {
    delay(2000); // delay for 2 seconds (2000ms) when our pretend sensor tells us to
  }

  // Let's pretend that our button is tapped at the same time that our sensor pulls the signal on
  // 'sensorPin' low.  The initial button press was detected and logged in the update() method
  // before the 2 second delay, and those 2 seconds will have elapsed by the time we get through
  // the main loop and get around to calling the update() method again.  This will result in a
  // 'hold' event being triggered even though the button was actually only tapped.  The ButtonEvents
  // library includes two methods to help avoid this type of incorrect mis-detection.

  // The first method is reset(), which resets the last known state of the button (as stored by the
  // update() method to 'idle'.    
  if (digitalRead(sensorPin) == LOW) {
      delay(2000); // delay for 2 seconds (2000ms) when our pretend sensor tells us to
      myButton.reset(); // reset the saved button state to 'idle' to prevent event mis-detection
  }
  
  // Using the reset() method prevents the 'hold' event from incorrectly triggering the next time
  // the update() method is called.  It actually prevents *any* event from triggering the next
  // time update() is called, including what would have been a tap event.  Use the reset() method
  // when you want button events that get interrupted by delays to be completely ignored.

  // The second method is retime(), which restarts the timing logic used by the update() method:
  if (digitalRead(sensorPin) == LOW) {
      delay(2000); // delay for 2 seconds (2000ms) when our pretend sensor tells us to
      myButton.retime(); // restart button event timing logic
  }

  // Using the reset() method will allow a tap that occurred before the delay to be triggered the 
  // next time the update() method is called, and allows hold events to trigger after the delay,
  // assuming that the button was pressed before the delay, and is held for the full hold duration
  // after the delay.  Double-taps that are interrupted by delays will never be triggered.
}
