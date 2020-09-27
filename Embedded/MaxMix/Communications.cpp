#include "Communications.h"

// Defined in the main file
extern DeviceSettings g_Settings;
extern SessionInfo g_SessionInfo;
extern SessionData g_Sessions[4];
extern DisplayData g_DisplayMode;
extern uint32_t g_LastMessage;
extern uint32_t g_Now;

namespace Communications
{
    void Initialize(void)
    {
        Serial.begin(BAUD_RATE);
        Serial.setTimeout(SERIAL_TIMEOUT);
    }

    Command Read(void)
    {
        // Future Notes: Serial.readBytes returns the number of bytes actually read
        // if necessary, we can use that compared against sizeof(T) to validate message
        // was recieved in it's entirity. However, if this happens, we should look at Serial timeout first.
        Command command = Command::NONE;
        if (Serial.available())
        {
            g_LastMessage = g_Now;
            command = (Command)Serial.read();
            if (command == Command::TEST)
                Write(command);
            else if (command == Command::SETTINGS)
                Serial.readBytes((char *)&g_Settings, sizeof(DeviceSettings));
            else if (command == Command::SESSION_INFO)
                Serial.readBytes((char *)&g_SessionInfo, sizeof(SessionInfo));
            else if (command == Command::CURRENT_SESSION)
                Serial.readBytes((char *)&g_Sessions[SessionIndex::INDEX_CURRENT], sizeof(SessionData));
            else if (command == Command::ALTERNATE_SESSION)
                Serial.readBytes((char *)&g_Sessions[SessionIndex::INDEX_ALTERNATE], sizeof(SessionData));
            else if (command == Command::PREVIOUS_SESSION)
                Serial.readBytes((char *)&g_Sessions[SessionIndex::INDEX_PREVIOUS], sizeof(SessionData));
            else if (command == Command::NEXT_SESSION)
                Serial.readBytes((char *)&g_Sessions[SessionIndex::INDEX_NEXT], sizeof(SessionData));
            else if (command == Command::VOLUME_CHANGE)
                Serial.readBytes((char *)&g_Sessions[SessionIndex::INDEX_CURRENT].data, sizeof(VolumeData));
            else if (command == Command::VOLUME_ALT_CHANGE)
                Serial.readBytes((char *)&g_Sessions[SessionIndex::INDEX_ALTERNATE].data, sizeof(VolumeData));
            else if (command == Command::DISPLAY_CHANGE)
                Serial.readBytes((char *)&g_DisplayMode, sizeof(DisplayData));
#ifdef TEST_HARNESS
            else if (command == Command::DEBUG:
            {
                Write(Command::SETTINGS);
                Write(Command::SESSION_INFO);
                Write(Command::PREVIOUS_SESSION);
                Write(Command::CURRENT);
                Write(Command::ALTERNATE);
                Write(Command::NEXT_SESSION);
                Write(Command::VOLUME_CHANGE);
                Write(Command::VOLUME_ALT_CHANGE);
                Write(Command::DISPLAY_CHANGE);
            }
#endif
            Write(Command::OK);
        }
        return command;
    }

    void Write(Command command)
    {
        if (command == Command::TEST)
        {
            Serial.write(command);
            Serial.println(F(FIRMWARE_VERSION));
        }
        else if (command == Command::SESSION_INFO)
        {
            Serial.write(command);
            Serial.write((char *)&g_SessionInfo, sizeof(SessionInfo));
        }
        else if (command == Command::VOLUME_CHANGE)
        {
            Serial.write(command);
            Serial.write((char *)&g_Sessions[SessionIndex::INDEX_CURRENT].data, sizeof(VolumeData));
        }
        else if (command == Command::VOLUME_ALT_CHANGE)
        {
            Serial.write(command);
            Serial.write((char *)&g_Sessions[SessionIndex::INDEX_ALTERNATE].data, sizeof(VolumeData));
        }
        else if (command == Command::DISPLAY_CHANGE)
        {
            Serial.write(command);
            Serial.write((char *)&g_DisplayMode, sizeof(DisplayData));
        }
        else if (command == Command::OK)
        {
            Serial.write(command);
        }
        // Do nothing: CURRENT_SESSION, ALTERNATE_SESSION, PREVIOUS_SESSION, NEXT_SESSION, SETTINGS, DEBUG, NONE, ERROR?
#ifdef TEST_HARNESS
        else if (command == Command::CURRENT_SESSION)
        {
            Serial.write(command);
            Serial.write((char *)&g_Session[SessionIndex::CURRENT_A], sizeof(session_t));
        }
        else if (command == Command::ALTERNATE_SESSION)
        {
            Serial.write(command);
            Serial.write((char *)&g_Session[SessionIndex::CURRENT_B], sizeof(session_t));
        }
        else if (command == Command::PREVIOUS_SESSION)
        {
            Serial.write(command);
            Serial.write((char *)&g_Session[SessionIndex::INDEX_PREVIOUS], sizeof(session_t));
        }
        else if (command == Command::NEXT_SESSION)
        {
            Serial.write(command);
            Serial.write((char *)&g_Session[SessionIndex::INDEX_NEXT], sizeof(session_t));
        }
        else if (command == Command::SETTINGS)
        {
            Serial.write(command);
            Serial.write((char *)&g_Settings, sizeof(DeviceSettings));
        }
        // Do nothing: DEBUG, NONE, ERROR?
#endif
        // Send buffered data
        Serial.flush();
    }
} // namespace Communications