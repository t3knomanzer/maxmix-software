#include "../../Embedded/MaxMix/Communications.h"
#include <cstdio>
#include <cstdlib>

TestSerial Serial;
DeviceSettings g_Settings;
SessionInfo g_SessionInfo;
SessionData g_Sessions[4];
ModeStates g_ModeStates;
ButtonSettings g_ButtonSettings;
LightingSettings g_LightingSettings;
uint32_t g_HeartbeatTimeout = 0;
uint32_t g_Now = 123;
int failures = 0;

void check(bool pass, const char *name)
{
    std::printf("%s %s\n", pass ? "PASS" : "FAIL", name);
    if (!pass) failures++;
}
void reset()
{
    Serial.input.clear(); Serial.output.clear();
    g_Settings = DeviceSettings(); g_SessionInfo = SessionInfo();
    g_ModeStates = ModeStates();
    g_ButtonSettings = ButtonSettings();
    g_LightingSettings = LightingSettings();
    for (auto &session : g_Sessions) session = SessionData();
}
template<typename T> void complete(Command command, T &target)
{
    reset();
    T expected;
    std::memset(&expected, 0x21, sizeof(T));
    Serial.input.push_back(command);
    const char *bytes = reinterpret_cast<const char *>(&expected);
    Serial.input.insert(Serial.input.end(), bytes, bytes + sizeof(T));
    check(Communications::Read() == command && std::memcmp(&expected, &target, sizeof(T)) == 0
        && Serial.output == std::vector<uint8_t>{Command::OK}, "complete packet is applied and acknowledged");
}
template<typename T> void truncated(Command command, T &target)
{
    reset();
    T before = target;
    Serial.input.push_back(command);
    for (size_t i = 0; i < sizeof(T) - 1; i++) Serial.input.push_back(0x21);
    auto result = Communications::Read();
    check(result == Command::ERROR && std::memcmp(&target, &before, sizeof(T)) == 0
        && Serial.output == std::vector<uint8_t>{uint8_t(Command::ERROR)}, "truncated packet leaves state intact and returns error");
}
int main()
{
    reset(); Serial.input = {15, 94, 1};
    check(Communications::Read() == Command(15) && Serial.output == std::vector<uint8_t>{Command::OK},
          "double-click interval command is accepted");
    check(g_ButtonSettings.doubleTapTime == 350, "double-click interval is stored in milliseconds");
    truncated(Command::BUTTON_SETTINGS, g_ButtonSettings);
    for (uint16_t interval : {0, 2001})
    {
        reset(); Serial.input = {15, uint8_t(interval), uint8_t(interval >> 8)};
        check(Communications::Read() == Command::ERROR && g_ButtonSettings.doubleTapTime == 150,
              "out-of-range double-click interval leaves previous state intact");
    }
    check(!UpdatesDisplay(Command::NONE) && !UpdatesDisplay(Command::OK) && !UpdatesDisplay(Command::TEST)
        && !UpdatesDisplay(Command::ERROR), "idle and heartbeat do not trigger OLED redraws");
    check(UpdatesDisplay(Command::SETTINGS) && UpdatesDisplay(Command::CURRENT_SESSION)
        && UpdatesDisplay(Command::VOLUME_ALT_CHANGE) && UpdatesDisplay(Command::MODE_STATES), "data commands trigger OLED redraws");
    complete(Command::SETTINGS, g_Settings);
    complete(Command::SESSION_INFO, g_SessionInfo);
    complete(Command::MODE_STATES, g_ModeStates);
    complete(Command::LIGHTING_SETTINGS, g_LightingSettings);
    truncated(Command::LIGHTING_SETTINGS, g_LightingSettings);
    for (int i = 0; i < 4; i++)
    {
        complete(Command(Command::CURRENT_SESSION + i), g_Sessions[i]);
        complete(Command(Command::VOLUME_CURR_CHANGE + i), g_Sessions[i].data);
    }
    truncated(Command::SETTINGS, g_Settings);
    truncated(Command::SESSION_INFO, g_SessionInfo);
    truncated(Command::MODE_STATES, g_ModeStates);
    for (int i = 0; i < 4; i++)
    {
        truncated(Command(Command::CURRENT_SESSION + i), g_Sessions[i]);
        truncated(Command(Command::VOLUME_CURR_CHANGE + i), g_Sessions[i].data);
    }
    reset(); Serial.input.push_back(99);
    check(Communications::Read() == Command::ERROR && Serial.output == std::vector<uint8_t>{uint8_t(Command::ERROR)}, "unknown command is rejected");
    reset();
    check(Communications::Read() == Command::NONE && Serial.output.empty(), "idle does not acknowledge phantom messages");
    reset(); Serial.input.push_back(Command::TEST);
    check(Communications::Read() == Command::TEST && Serial.output.front() == Command::TEST && Serial.output.back() == Command::OK, "handshake remains compatible");
    return failures == 0 ? EXIT_SUCCESS : EXIT_FAILURE;
}
