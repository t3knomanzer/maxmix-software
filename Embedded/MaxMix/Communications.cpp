#include "Communications.h"

// Defined in the main file
extern DeviceSettings g_Settings;
extern SessionInfo g_SessionInfo;
extern SessionData g_Sessions[4];
extern ModeStates g_ModeStates;
extern ButtonSettings g_ButtonSettings;
extern LightingSettings g_LightingSettings;
extern uint32_t g_HeartbeatTimeout;
extern uint32_t g_Now;

//#define TEST_HARNESS

namespace Communications
{
    template <typename T>
    bool ReadPayload(T &target)
    {
        // A timeout must not leave half of a settings/session structure updated.
        T incoming;
        if (Serial.readBytes((char *)&incoming, sizeof(T)) != sizeof(T))
            return false;
        target = incoming;
        return true;
    }

    void Initialize(void)
    {
        Serial.begin(BAUD_RATE);
        Serial.setTimeout(SERIAL_TIMEOUT);
    }

    Command Read(void)
    {
        Command command = Command::NONE;
        if (Serial.available())
        {
            bool valid = true;
            command = (Command)Serial.read();
            if (command == Command::TEST)
                Write(command);
            else if (command == Command::SETTINGS)
                valid = ReadPayload(g_Settings);
            else if (command == Command::SESSION_INFO)
                valid = ReadPayload(g_SessionInfo);
            else if (command >= Command::CURRENT_SESSION && command <= Command::NEXT_SESSION)
                // SessionIndex follows same ordering as Command.
                valid = ReadPayload(g_Sessions[command - Command::CURRENT_SESSION]);
            else if (command >= Command::VOLUME_CURR_CHANGE && command <= Command::VOLUME_NEXT_CHANGE)
                // SessionIndex follows same ordering as Command.
                valid = ReadPayload(g_Sessions[command - Command::VOLUME_CURR_CHANGE].data);
            else if (command == Command::MODE_STATES)
                valid = ReadPayload(g_ModeStates);
            else if (command == Command::BUTTON_SETTINGS)
            {
                ButtonSettings incoming;
                valid = ReadPayload(incoming) && incoming.doubleTapTime >= 1 && incoming.doubleTapTime <= 2000;
                if (valid) g_ButtonSettings = incoming;
            }
            else if (command == Command::LIGHTING_SETTINGS)
                valid = ReadPayload(g_LightingSettings);
            // Do nothing: DEBUG, NONE, ERROR?
#ifdef TEST_HARNESS
            else if (command == Command::DEBUG)
            {
                Write(Command::SETTINGS);
                Write(Command::SESSION_INFO);
                Write(Command::CURRENT_SESSION);
                Write(Command::ALTERNATE_SESSION);
                Write(Command::PREVIOUS_SESSION);
                Write(Command::NEXT_SESSION);
                Write(Command::VOLUME_CURR_CHANGE);
                Write(Command::VOLUME_ALT_CHANGE);
                Write(Command::VOLUME_PREV_CHANGE);
                Write(Command::VOLUME_NEXT_CHANGE);
            }
#endif
            else if (command != Command::OK)
                valid = false;
            if (!valid)
            {
                // ERROR is a wire byte (0xff). The host reconnects and resends state.
                Serial.write((uint8_t)Command::ERROR);
                Serial.flush();
                return Command::ERROR;
            }
            g_HeartbeatTimeout = g_Now + DEVICE_RESET_AFTER_INACTIVTY;
            Write(Command::OK);
        }
        return command;
    }

    void Write(Command command)
    {
        // Do nothing: DEBUG, NONE, ERROR
        if (command == Command::ERROR || command == Command::NONE || command == Command::DEBUG)
            return;

        Serial.write(command);
        if (command == Command::TEST)
            Serial.println(F(VERSION));
        else if (command == Command::SETTINGS)
            Serial.write((char *)&g_Settings, sizeof(DeviceSettings));
        else if (command == Command::SESSION_INFO)
            Serial.write((char *)&g_SessionInfo, sizeof(SessionInfo));
        else if (command >= Command::CURRENT_SESSION && command <= Command::NEXT_SESSION)
            Serial.write((char *)&g_Sessions[command - Command::CURRENT_SESSION], sizeof(SessionData));
        else if (command >= Command::VOLUME_CURR_CHANGE && command <= Command::VOLUME_NEXT_CHANGE)
            Serial.write((char *)&g_Sessions[command - Command::VOLUME_CURR_CHANGE].data, sizeof(VolumeData));
        else if (command == Command::MODE_STATES)
            Serial.write((char *)&g_ModeStates, sizeof(ModeStates));
        // command == Command::OK just replies with command
        // Send buffered data
        Serial.flush();
    }
} // namespace Communications
