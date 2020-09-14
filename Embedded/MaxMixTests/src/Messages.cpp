#include <Messages.h>

// Defined in main.cpp
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
            Command command = (Command)Serial.read(); 
            switch (command)
            {
                case Command::TEST:
                    Write(command);
                    break;
                case Command::SETTINGS:
                    Serial.readBytes((uint8_t*)&_settings, sizeof(Settings));
                    break;
                case Command::SESSION_INFO:
                    Serial.readBytes((uint8_t*)&_sessionInfo, sizeof(SessionInfo));
                    break;
                case Command::CURRENT_SESSION:
                    Serial.readBytes((uint8_t*)&_session[1], sizeof(Session));
                    break;
                case Command::PREVIOUS_SESSION:
                    Serial.readBytes((uint8_t*)&_session[0], sizeof(Session));
                    break;
                case Command::NEXT_SESSION:
                    Serial.readBytes((uint8_t*)&_session[2], sizeof(Session));
                    break;
                case Command::VOLUME_CHANGE:
                    Serial.readBytes((uint8_t*)&_session[1].values, sizeof(Volume));
                    break;
                case Command::SCREEN_CHANGE:
                    Serial.readBytes((uint8_t*)&_screen, sizeof(Screen));
                    break;
                case Command::OK:
                    // Do nothing;
                    break;
#ifdef TEST_HARNESS
                case Command::DEBUG:
                {
                    Write(Command::SETTINGS);
                    Write(Command::SESSION_INFO);
                    Write(Command::PREVIOUS_SESSION);
                    Write(Command::CURRENT_SESSION);
                    Write(Command::NEXT_SESSION);
                    Write(Command::VOLUME_CHANGE);
                    Write(Command::SCREEN_CHANGE);
                }
                    break;
#endif
            }
            Write(Command::OK);
        }
    }

    // Pretty sure this is not needed as we call Serial.flush() at the end of Write()
    /*static void Write(Command command, const uint8_t* buffer, size_t size)
    {
        // Ensure there is enough buffer space for the command + payload
        while(Serial.availableForWrite() < size + 1)
            delay(1);
        Serial.write(command);
        Serial.write(buffer, size);
        Serial.flush();
    }*/

    void Write(Command command)
    {
        switch (command)
        {
            case Command::TEST:
            {
                Serial.write(command);
                Serial.println(F(FIRMWARE_VERSION));
            }
                break;
            case Command::SESSION_INFO:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_sessionInfo, sizeof(SessionInfo));
            }
                break;
            case Command::VOLUME_CHANGE:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[1].values, sizeof(Volume));
            }
                break;
            case Command::SCREEN_CHANGE:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_screen, sizeof(Screen));
            }
                break;
            case Command::OK:
            {
                Serial.write(command);
            }
                break;
            // Do nothing: CURRENT, PREVIOUS, NEXT, SETTINGS, DEBUG
#ifdef TEST_HARNESS
            case Command::CURRENT_SESSION:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[1], sizeof(Session));
            }
                break;
            case Command::PREVIOUS_SESSION:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[0], sizeof(Session));
            }
                break;
            case Command::NEXT_SESSION:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_session[2], sizeof(Session));
            }
                break;
            case Command::SETTINGS:
            {
                Serial.write(command);
                Serial.write((uint8_t*)&_settings, sizeof(Settings));
            }
                break;
            case Command::DEBUG:
                // Do nothing
                break;
#endif
        }
        // Send buffered data
        Serial.flush();
    }
}