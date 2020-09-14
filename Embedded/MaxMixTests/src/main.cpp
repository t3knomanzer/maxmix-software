#include <main.h>
#include <Messages.h>

// Defined and default initialized via {}
Message::Settings _settings {};
Message::SessionInfo _sessionInfo {};
Message::Session _session[3] {};
Message::Screen _screen {};

uint8_t _loopCount = 0;

void setup(void)
{
    Message::Initialize();
}

void loop(void)
{
    Message::Read();

    // Psudo Simulate Update Loop (LED, OLED, ENCODER)
    delay((uint8_t)random(10, 20));
}

void PreviousSession(void)
{
    if (!_settings.continuousScroll && _sessionInfo.current == 0)
        return;

    if (_sessionInfo.current == 0)
        _sessionInfo.current = _sessionInfo.count;
    _sessionInfo.current--;
    _session[0] = _session[2];
    _session[2] = _session[1];
    memset((void *)&_session[0], 0, sizeof(Message::Session));
    Message::Write(Message::Command::SESSION_INFO);
}

void NextSession(void)
{
    if (!_settings.continuousScroll && _sessionInfo.current == _sessionInfo.count - 1)
        return;

    _sessionInfo.current = (_sessionInfo.current + 1) % _sessionInfo.count;
    _session[0] = _session[1];
    _session[1] = _session[2];
    memset((void *)&_session[2], 0, sizeof(Message::Session));
    Message::Write(Message::Command::SESSION_INFO);
}