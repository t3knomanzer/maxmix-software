#pragma once

#include <main.h>

namespace Message
{
    static const uint8_t TEST       = 1;
    static const uint8_t OK         = 2;
    static const uint8_t SETTINGS   = 3;

    static const uint8_t SESSION_INFO   = 10;
    static const uint8_t CURRENT    = 11;
    static const uint8_t PREVIOUS   = 12;
    static const uint8_t NEXT       = 13;
    
    static const uint8_t VOLUME = 21;
    static const uint8_t SCREEN = 22;

    struct __attribute__((__packed__)) SessionInfo
    {
        uint8_t current;    // 8 bits
        uint8_t count;      // 8 bits
    }; // 16 bits - 2 bytes
    static_assert(sizeof(SessionInfo) == 2, "Invalid Expected Message Size");

    // Can be produced via memcopy of Session, offset after name
    struct __attribute__((__packed__)) Volume
    {
        uint8_t id          :7; // 7 bits
        bool    isDefault   :1; // 1 bit
        uint8_t volume      :7; // 7 bits 
        bool    isMuted     :1; // 1 bit
    }; // 16 bits - 2 bytes
    static_assert(sizeof(Volume) == 2, "Invalid Expected Message Size");

    struct __attribute__((__packed__)) Session
    {
        char    name[30];   // 240 bits
        Volume  values;     // 24 bits
    }; // 256 bits - 32 bytes
    static_assert(sizeof(Session) == 32, "Invalid Expected Message Size");

    struct __attribute__((__packed__)) Screen
    {
        uint8_t id;     // 8 bits
    }; // 8 bits - 1 byte
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

        Settings() : sleepAfterSeconds(5), accelerationPercentage(60), continuousScroll(true),
            volumeMinColor(0, 0, 255), volumeMaxColor(255, 0, 0), 
            mixChannelAColor(0, 0, 255), mixChannelBColor(255, 0, 255) { }
    }; // 112 bits - 14 bytes
    static_assert(sizeof(Settings) == 14, "Invalid Expected Message Size");

    void Initialize(void);
    void Read(void);
    void Write(uint8_t command);
}
