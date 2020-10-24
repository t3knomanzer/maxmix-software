#pragma once
//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
//
//
//
//********************************************************

#include <Arduino.h>
#include "Enums.h"
#include "Structs.h"
#include "src/FixedPoints/FixedPoints.h"
#include "src/FixedPoints/FixedPointsCommon.h"

//********************************************************
// *** DEFINES
//********************************************************
#ifndef VERSION
    #define VERSION "1.4.1"
#endif

//********************************************************
// *** CONSTS
//********************************************************

// --- Serial Comms
static const uint32_t BAUD_RATE = 115200;
// Default serial timeout is 1000 ms, at 115200 baud (bit/s)
// our longest message at 296 bits (33 bytes) takes 2.29ms to send.
static const uint64_t SERIAL_TIMEOUT = 5;

// --- Pins
static const uint8_t PIN_PIXELS = 12;         //D12
static const uint8_t PIN_ENCODER_OUTA = 15;   //A1
static const uint8_t PIN_ENCODER_OUTB = 16;   //A2
static const uint8_t PIN_ENCODER_SWITCH = 17; //A3

// --- States
static const uint8_t STATE_NAVIGATE = 0;
static const uint8_t STATE_LOGO = 0;
static const uint8_t STATE_EDIT = 1;
static const uint8_t STATE_INFO = 1;
static const uint8_t STATE_MAX = 2;
static const uint8_t STATE_SELECT_A = 0;
static const uint8_t STATE_SELECT_B = 1;
static const uint8_t STATE_GAME_EDIT = 2;
static const uint8_t STATE_GAME_MAX = 3;

// --- Display
static const uint8_t DISPLAY_RESET = 4; // Reset pin # (or -1 if sharing Arduino reset pin)
static const uint32_t DISPLAY_SPEED = 400000;

// --- Lighting
static const uint8_t PIXELS_COUNT = 8;      // Number of pixels in ring
static const uint8_t PIXELS_BRIGHTNESS = 96; // Master brightness of all the pixels. [0..255] Be carefull of the current draw on the USB port.

// --- Rotary Encoder
static const uint16_t ROTARY_ACCELERATION_DIVISOR_MAX = 400;

// --- Screen Drawing
static const uint8_t DISPLAY_WIDTH = 128;
static const uint8_t DISPLAY_HEIGHT = 32;
static const uint8_t DISPLAY_ADDRESS = 0x3C;

static const uint8_t DISPLAY_CHAR_WIDTH_X1 = 5;
static const uint8_t DISPLAY_CHAR_HEIGHT_X1 = 7;
static const uint8_t DISPLAY_CHAR_HEIGHT_CLEAR_X1 = 8;
static const uint8_t DISPLAY_CHAR_SPACING_X1 = 1;
static const uint8_t DISPLAY_CHAR_WIDTH_X2 = 10;
static const uint8_t DISPLAY_CHAR_HEIGHT_X2 = 14;
static const uint8_t DISPLAY_CHAR_HEIGHT_CLEAR_X2 = 16;
static const uint8_t DISPLAY_CHAR_SPACING_X2 = 2;

static const uint8_t DISPLAY_CHAR_MAX_X1 = 18;
static const uint8_t DISPLAY_CHAR_MAX_X2 = 9;
static const uint8_t DISPLAY_MARGIN_X1 = 2;
static const uint8_t DISPLAY_MARGIN_X2 = 4;

static const uint8_t DISPLAY_AREA_CENTER_MARGIN_SIDE = 11;
static const uint8_t DISPLAY_AREA_CENTER_MARGIN_BOTTOM = 7;
static const uint8_t DISPLAY_AREA_CENTER_WIDTH = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE * 2;
static const uint8_t DISPLAY_AREA_CENTER_HEIGHT = DISPLAY_HEIGHT - DISPLAY_AREA_CENTER_MARGIN_BOTTOM;

static const uint8_t DISPLAY_WIDGET_DOT_SIZE_X1 = 2;
static const uint8_t DISPLAY_WIDGET_DOT_SIZE_X2 = 4;
static const uint8_t DISPLAY_WIDGET_ARROW_SIZE_X1 = 3;
static const uint8_t DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1 = 6;
static const uint8_t DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2 = 14;
static const uint8_t DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X1 = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_MARGIN_X1 * 3;
static const uint8_t DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X2 = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_MARGIN_X1 * 4 - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 2;

static const uint8_t DISPLAY_WIDGET_DOTGROUP_WIDTH = (DisplayMode::MODE_MAX - 1) * DISPLAY_WIDGET_DOT_SIZE_X1 + DISPLAY_WIDGET_DOT_SIZE_X2 + (DisplayMode::MODE_MAX - 1) * DISPLAY_MARGIN_X2;
static const uint8_t DISPLAY_WIDGET_DOTGROUP_HEIGHT = DISPLAY_WIDGET_DOT_SIZE_X2;

static const SQ15x16 DISPLAY_SCROLL_SPEED_X2 = 3.0;  // Chars per second
static const SQ15x16 DISPLAY_SCROLL_SPEED_X1 = 4.0;  // Chars per second
static const SQ15x16 DISPLAY_SCROLL_IDLE_TIME = 3.0; // Seconds

// - Mode specific
static const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX = 7;
static const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH = DISPLAY_GAME_EDIT_CHAR_MAX * DISPLAY_CHAR_WIDTH_X1 + DISPLAY_GAME_EDIT_CHAR_MAX - 1;
static const uint8_t DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT = 7;
static const uint8_t DISPLAY_GAME_VOLUMEBAR_WIDTH = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH - DISPLAY_MARGIN_X2 - 2 - DISPLAY_MARGIN_X1 * 2;

static const uint32_t DEVICE_RESET_AFTER_INACTIVTY = 5000;
