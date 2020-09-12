#pragma once

#include "Config.h"

typedef struct  __attribute__((__packed__)) {
  uint8_t r;  // 1 Byte 
  uint8_t g;  // 1 Byte
  uint8_t b;  // 1 Byte
} Color;

typedef struct __attribute__((__packed__))
{
  char name[ITEM_BUFFER_NAME_SIZE]; // 24 Bytes (Chars)
  uint32_t id;                      // 4 Bytes (32 bit)
  int8_t volume;                    // 1 Byte
  uint8_t isMuted;                  // 1 Byte
} Item;

typedef struct __attribute__((__packed__))
{
  uint8_t displayNewItem = 1;                   // 1 Byte
  uint8_t sleepWhenInactive = 1;                // 1 Byte
  uint8_t sleepAfterSeconds = 5;                // 1 Byte
  uint8_t continuousScroll = 1;                 // 1 Byte
  uint8_t accelerationPercentage = 60;          // 1 Byte
  Color volumeMinColor = {0x00, 0x00, 0xFF};    // 3 Bytes
  Color volumeMaxColor = {0xFF, 0x00, 0x00};    // 3 Bytes
  Color mixChannelAColor = {0x00, 0x00, 0xFF};  // 3 Bytes
  Color mixChannelBColor = {0xFF, 0x00, 0xFF};  // 3 Bytes 
} Settings;

static_assert(sizeof(Item) == 30, "'Item' struct not the expected size");
static_assert(sizeof(Settings) == 17, "'Settings' struct not the expected size");
static_assert(sizeof(Color) == 3, "'Settings' struct not the expected size");
