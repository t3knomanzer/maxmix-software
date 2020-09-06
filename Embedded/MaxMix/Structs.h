#pragma once

#include "Config.h"

typedef struct __attribute__((__packed__))
{
  char name[ITEM_BUFFER_NAME_SIZE]; // 36 Bytes (36 Chars)
  uint32_t id;                      // 4 Bytes (32 bit)
  int8_t volume;                    // 1 Byte
  uint8_t isMuted;                  // 1 Byte
} Item;

typedef struct __attribute__((__packed__))
{
  uint8_t displayNewItem = 1;          // 1 Byte
  uint8_t sleepWhenInactive = 1;       // 1 Byte
  uint8_t sleepAfterSeconds = 5;       // 1 Byte
  uint8_t continuousScroll = 1;        // 1 Byte
  uint8_t accelerationPercentage = 60; // 1 Byte
} Settings;

static_assert(sizeof(Item) == 42, "'Item' struct not the expected size");
static_assert(sizeof(Settings) == 5, "'Settings' struct not the expected size");