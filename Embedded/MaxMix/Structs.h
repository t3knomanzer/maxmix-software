#pragma once

#include "Config.h"

// NOTES: Code might be simpler if we use an array of uint8_t and index into it using DisplayMode's value instead
struct __attribute__((__packed__)) DisplayState
{
    // Splash (true) vs Firmware info (false)
    bool splash : 1;
    // Navigate (true) vs Edit (false)
    bool output : 1;
    bool input : 1;
    bool application : 1;
    // Select A (1), Select B (2) Edit (0)
    uint8_t game : 2;
    // new modes
    uint8_t unused : 2;
    DisplayState() : splash(true), output(false), input(false), application(true), game(1), unused(3) {}
};
static_assert(sizeof(DisplayState) == 1, "Invalid Expected Message Size");

struct __attribute__((__packed__)) SessionInfo
{
    uint8_t current; // 8 bits
    uint8_t count;   // 8 bits
    // 16 bits - 2 bytes

    SessionInfo() : current(0), count(0) {}
};
static_assert(sizeof(SessionInfo) == 2, "Invalid Expected Message Size");

struct __attribute__((__packed__)) VolumeData
{
    uint8_t id : 7;     // 7 bits
    bool isDefault : 1; // 1 bit
    uint8_t volume : 7; // 7 bits
    bool isMuted : 1;   // 1 bit
    // 16 bits - 2 bytes

    VolumeData() : id(0), isDefault(false), volume(0), isMuted(false) {}
};
static_assert(sizeof(VolumeData) == 2, "Invalid Expected Message Size");

struct __attribute__((__packed__)) SessionData
{
    char name[30]; // 240 bits
    VolumeData data; // 24 bits
    // 256 bits - 32 bytes

    // name & data use { } initializers
    SessionData() : name{0}, data{} {}
};
static_assert(sizeof(SessionData) == 32, "Invalid Expected Message Size");

struct __attribute__((__packed__)) DisplayData
{
    DisplayMode id; // 8 bits
    // 8 bits - 1 byte

    DisplayData() : id(DisplayMode::MODE_SPLASH) {}
};
static_assert(sizeof(DisplayData) == 1, "Invalid Expected Message Size");

struct __attribute__((__packed__)) Color
{
    uint8_t r; // 8 bits
    uint8_t g; // 8 bits
    uint8_t b; // 8 bits

    Color() : r(0), g(0), b(0) {}
    Color(uint8_t r, uint8_t g, uint8_t b) : r(r), g(g), b(b) {}
}; // 24 bits - 3 bytes
static_assert(sizeof(Color) == 3, "Invalid Expected Message Size");

struct __attribute__((__packed__)) DeviceSettings
{
    uint8_t sleepAfterSeconds;          // 8 Bits
    uint8_t accelerationPercentage : 7; // 7 Bits
    bool continuousScroll : 1;          // 1 Bit
    Color volumeMinColor;               // 24 Bits
    Color volumeMaxColor;               // 24 Bits
    Color mixChannelAColor;             // 24 Bits
    Color mixChannelBColor;             // 24 Bits
    // 112 bits - 14 bytes

    DeviceSettings() : sleepAfterSeconds(5), accelerationPercentage(60), continuousScroll(true),
                 volumeMinColor(0, 0, 255), volumeMaxColor(255, 0, 0), mixChannelAColor(0, 0, 255), mixChannelBColor(255, 0, 255) {}
};
static_assert(sizeof(DeviceSettings) == 14, "Invalid Expected Message Size");