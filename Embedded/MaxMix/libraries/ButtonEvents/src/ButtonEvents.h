/* * * * * * * * * * * * * * * * * * * * * * * * * * * *
Written by Edward Wright (fasteddy@thewrightspace.net)
Utilizes the Bounce2 library (https://github.com/thomasfredericks/Bounce2) by Thomas O Fredericks (tof@t-o-f.info)
* * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#ifndef ButtonEvents_h
#define ButtonEvents_h

#include "../../Bounce2/src/Bounce2.h" // use Thomas Fredericks' button debounce library

// these are the default settings used in the constructor - all of them can be changed after instantiation using the corresponding methods 
#define DEFAULT_DEBOUNCE_MS   35
#define DEFAULT_DOUBLETAP_MS  150
#define DEFAULT_HOLD_MS       500
#define DEFAULT_ACTIVE_LOW    true

// enumerations to keep things readable
enum ButtonState { idle, pressed, released };
enum ButtonEvent { none, tap, doubleTap, hold };

class ButtonEvents {
  public:
    // public methods...
    ButtonEvents(); // default constructor - follows the argument-free Bounce2 convention.  Use defaults, or set explicitly later
    void attach(int pin); // passthru to Bounce2 attach() method
    void attach(int pin, int mode); // passthru to Bounce2 attach() overload
    void activeHigh(); // set button mode to active high
    void activeLow(); // set button mode to active low
    void debounceTime(unsigned int debounce_ms); // alias/passthru to Bounce2 interval() method
    void doubleTapTime(unsigned int doubleTap_ms); // method for setting the doubleTap event detection window
    void holdTime(unsigned int hold_ms); // method for setting the time required to trigger a hold event
    void interval(unsigned int interval_ms); // passthru to Bounce2 interval() method
    bool update(); // calls the Bounce2 update() method, then runs button event detection logic
    void reset(); // resets the saved button state to idle
    void retime(); // sets the button event timestamp to the current value of millis()
    ButtonEvent event(); // returns the button event detected during update() call
    bool tapped(); // returns true if the 'tap' event was detected 
    bool doubleTapped(); // returns true if the 'doubleTap' event was detected
    bool held(); // returns true if the 'held' event was detected
    bool read(); // passthru to Bounce2 read() method;
    bool fell(); // passthru to Bounce2 fell() method;
    bool rose(); // passthru to Bounce2 rose() method;
		
  private:
    // private instance variables...
    unsigned long eventTime_ms; // remember when the button was pressed/released
    unsigned int doubleTapTime_ms; // how long to wait for a double tap after the initial button release 
    unsigned int holdTime_ms; // how long the button must be held to generate a hold event
    ButtonState buttonState; // current button state
    ButtonEvent buttonEvent; // detected button event
    bool isActiveLow;
    Bounce debouncedButton; // button debounced using Thomas Fredericks' Bounce2 library
    
    // private methods...
    bool buttonPressed(); // returns true if the button was pressed (accounts for active high/low)
    bool buttonReleased(); // returns true if the button was released (accounts for active high/low)
};

#endif

