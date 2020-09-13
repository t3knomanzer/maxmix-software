#include <Messages.h>

extern Message::Settings _settings;
extern Message::SessionInfo _sessionInfo;
extern Message::Session _session[3];
extern Message::Screen _screen;

namespace Message
{
    void Initialize(void)
    {
        Serial.begin(115200);
        // Default serial timeout is 1000 ms, at 115200 baud (bit/s)
        // our longest message at 296 bits (37 bytes) takes 2.5694ms to send.
        Serial.setTimeout(5);
    }

    void Read(void)
    {
        // Future Notes: Serial.readBytes returns the number of bytes actually read
        // if necessary, we can use that compared against sizeof(T) to validate message
        // was recieved in it's entirity. However, if this happens, we should look at Serial timeout first.

        if (Serial.available())
        {
            uint8_t command = Serial.read(); 
            switch (command)
            {
                case Message::TEST:
                    Write(command);
                    break;
                case Message::SETTINGS:
                    Serial.readBytes((uint8_t*)&_settings, sizeof(Message::Settings));
                    break;
                case Message::SESSION_INFO:
                    Serial.readBytes((uint8_t*)&_sessionInfo, sizeof(Message::SessionInfo));
                    break;
                case Message::CURRENT:
                    Serial.readBytes((uint8_t*)&_session[1], sizeof(Message::Session));
                    break;
                case Message::PREVIOUS:
                    Serial.readBytes((uint8_t*)&_session[0], sizeof(Message::Session));
                    break;
                case Message::NEXT:
                    Serial.readBytes((uint8_t*)&_session[2], sizeof(Message::Session));
                    break;
                case Message::VOLUME:
                    Serial.readBytes((uint8_t*)&_session[1].values, sizeof(Message::Volume));
                    break;
                case Message::SCREEN:
                    Serial.readBytes((uint8_t*)&_screen, sizeof(Message::Screen));
                    break;
                // Do nothing: OK
            }
            Write(Message::OK);
        }
    }

    void Write(uint8_t command)
    {
        switch (command)
        {
            case Message::TEST:
            {
                Serial.write(command);
                Serial.print(F(FIRMWARE_VERSION));
            }
                break;
            case Message::SESSION_INFO:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_sessionInfo, sizeof(Message::SessionInfo));
            }
                break;
            case Message::VOLUME:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[1].values, sizeof(Message::Volume));
            }
                break;
            case Message::SCREEN:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_screen, sizeof(Message::Screen));
            }
                break;
            case Message::OK:
                Serial.write(command);
                break;
            // Do nothing: CURRENT, PREVIOUS, NEXT, SETTINGS
#ifdef TEST_HARNESS
            case Message::CURRENT:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[1], sizeof(Message::Session));
            }
                break;
            case Message::PREVIOUS:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[0], sizeof(Message::Session));
            }
                break;
            case Message::NEXT:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[2], sizeof(Message::Session));
            }
            case Message::SETTINGS:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_settings, sizeof(Message::Settings));
            }
                break;
#endif
        }
        // Send buffered data
        Serial.flush();
    }
}