#pragma once

#include <main.h>

namespace Message
{
    enum Command : uint8_t
    { 
        TEST = 1,
        OK,
        SETTINGS,
        SESSION_INFO,
        CURRENT_SESSION,
        PREVIOUS_SESSION,
        NEXT_SESSION,
        VOLUME_CHANGE,
        SCREEN_CHANGE,
        DEBUG
    };


    struct __attribute__((__packed__)) SessionInfo
    {
        uint8_t current;    // 8 bits
        uint8_t count;      // 8 bits
        // 16 bits - 2 bytes

        SessionInfo() : current(0), count(0) { }
    }; 
    static_assert(sizeof(SessionInfo) == 2, "Invalid Expected Message Size");


    struct __attribute__((__packed__)) Volume
    {
        uint8_t id          :7; // 7 bits
        bool    isDefault   :1; // 1 bit
        uint8_t volume      :7; // 7 bits 
        bool    isMuted     :1; // 1 bit
        // 16 bits - 2 bytes
        
        Volume() : id(0), isDefault(false), volume(0), isMuted(false) { }
    }; 
    static_assert(sizeof(Volume) == 2, "Invalid Expected Message Size");


    struct __attribute__((__packed__)) Session
    {
        char    name[30];   // 240 bits
        Volume  values;     // 24 bits
        // 256 bits - 32 bytes

        // name & values use { } initializers
        Session() : name {0}, values { } { }
    }; 
    static_assert(sizeof(Session) == 32, "Invalid Expected Message Size");


    struct __attribute__((__packed__)) Screen
    {
        uint8_t id;     // 8 bits
        // 8 bits - 1 byte
        
        Screen() : id(0) { }
    };
    static_assert(sizeof(Screen) == 1, "Invalid Expected Message Size");


    struct __attribute__((__packed__)) Color
    {
        uint8_t r;  // 8 bits
        uint8_t g;  // 8 bits
        uint8_t b;  // 8 bits

        Color() : r(0), g(0), b(0) { }
        Color(uint8_t r, uint8_t g, uint8_t b) : r(r), g(g), b(b) { }
    }; // 24 bits - 3 bytes
    static_assert(sizeof(Color) == 3, "Invalid Expected Message Size");


    struct __attribute__((__packed__)) Settings
    {
        uint8_t sleepAfterSeconds;          // 8 Bits
        uint8_t accelerationPercentage  :7; // 7 Bits
        bool    continuousScroll        :1; // 1 Bit
        Color   volumeMinColor;             // 24 Bits
        Color   volumeMaxColor;             // 24 Bits
        Color   mixChannelAColor;           // 24 Bits
        Color   mixChannelBColor;           // 24 Bits
        // 112 bits - 14 bytes

        Settings() : sleepAfterSeconds(5), accelerationPercentage(60), continuousScroll(true),
            volumeMinColor(0, 0, 255), volumeMaxColor(255, 0, 0),  mixChannelAColor(0, 0, 255), mixChannelBColor(255, 0, 255) { }
    }; 
    static_assert(sizeof(Settings) == 14, "Invalid Expected Message Size");

    void Initialize(void);
    void Read(void);
    void Write(Command command);
}
