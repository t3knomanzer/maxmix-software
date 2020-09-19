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

#include "src/FixedPoints/FixedPoints.h"
#include "src/FixedPoints/FixedPointsCommon.h"

//********************************************************
// *** DEFINES
//********************************************************
#ifndef VERSION_MAJOR
    #define VERSION_MAJOR 1
#endif

#ifndef VERSION_MINOR
    #define VERSION_MINOR 3
#endif

#ifndef VERSION_PATCH
    #define VERSION_PATCH 0
#endif


//********************************************************
// *** CONSTS
//********************************************************

// --- Serial Comms
static const uint32_t BAUD_RATE = 115200;

// --- Pins
static const uint8_t  PIN_PIXELS = 12; //D12
static const uint8_t  PIN_ENCODER_OUTA = 15; //A1
static const uint8_t  PIN_ENCODER_OUTB = 16; //A2
static const uint8_t  PIN_ENCODER_SWITCH = 17; //A3

// --- States
static const uint8_t  MODE_SPLASH = 255;

static const uint8_t  MODE_OUTPUT = 0;
static const uint8_t  MODE_INPUT = 1;
static const uint8_t  MODE_APPLICATION = 2;
static const uint8_t  MODE_GAME = 3;
static const uint8_t  MODE_COUNT = 4;

static const uint8_t  STATE_SPLASH_LOGO = 0;
static const uint8_t  STATE_SPLASH_INFO = 1;
static const uint8_t  STATE_SPLASH_COUNT = 2;

static const uint8_t  STATE_OUTPUT_NAVIGATE = 0;
static const uint8_t  STATE_OUTPUT_EDIT = 1;
static const uint8_t  STATE_OUTPUT_COUNT = 2;

static const uint8_t  STATE_INPUT_NAVIGATE = 0;
static const uint8_t  STATE_INPUT_EDIT = 1;
static const uint8_t  STATE_INPUT_COUNT = 2;

static const uint8_t  STATE_APPLICATION_NAVIGATE = 0;
static const uint8_t  STATE_APPLICATION_EDIT = 1;
static const uint8_t  STATE_APPLICATION_COUNT = 2;

static const uint8_t  STATE_GAME_SELECT_A = 0;
static const uint8_t  STATE_GAME_SELECT_B = 1;
static const uint8_t  STATE_GAME_EDIT = 2;
static const uint8_t  STATE_GAME_COUNT = 3;

static const uint8_t  STATE_DISPLAY_AWAKE = 0;
static const uint8_t  STATE_DISPLAY_SLEEP = 1;

// --- Display
static const uint8_t  DISPLAY_RESET =   4; // Reset pin # (or -1 if sharing Arduino reset pin)
static const uint32_t DISPLAY_SPEED =   400000;

// --- Lighting
static const uint8_t  PIXELS_COUNT = 8; // Number of pixels in ring
static const uint8_t  PIXELS_BRIGHTNESS = 96; // Master brightness of all the pixels. [0..255] Be carefull of the current draw on the USB port.

// --- Rotary Encoder
static const uint16_t ROTARY_ACCELERATION_DIVISOR_MAX = 400;

// --- Messages
static const uint8_t DEVICE_OUTPUT_MAX_COUNT = 4;
static const uint8_t DEVICE_INPUT_MAX_COUNT = 2;
static const uint8_t SESSION_MAX_COUNT = 6;
static const uint8_t ITEM_BUFFER_NAME_SIZE = 24;
static const uint8_t RECEIVE_BUFFER_SIZE = 37; // 1 overhead + 1 revision + 1 command + (31) payload + 1 length + 1 end byte.
static const uint8_t DECODE_BUFFER_SIZE = 32; // Largest message received.
static const uint8_t SEND_BUFFER_SIZE = 7; // Largest message sent (7) + 1 command.
static const uint8_t ENCODE_BUFFER_SIZE = 10; //  1 overhead + (7) payload + 1 length + 1 end byte.

// These values match exactly the ones in the C# application.
static const uint8_t MSG_COMMAND_HANDSHAKE_REQUEST =  0;
static const uint8_t MSG_COMMAND_ACKNOWLEDGMENT =  1;
static const uint8_t MSG_COMMAND_ADD =  2;
static const uint8_t MSG_COMMAND_REMOVE =  3;
static const uint8_t MSG_COMMAND_UPDATE_VOLUME =  4;
static const uint8_t MSG_COMMAND_SET_DEFAULT_ENDPOINT =  5;
static const uint8_t MSG_COMMAND_SETTINGS =  6;
static const uint8_t MSG_COMMAND_HEARTBEAT =  7;
static const uint8_t MSG_PACKET_DELIMITER = 0;

static const uint8_t DEVICE_FLOW_INPUT = 0;
static const uint8_t DEVICE_FLOW_OUTPUT = 1;

// --- Screen Drawing
static const uint8_t DISPLAY_WIDTH = 128;
static const uint8_t DISPLAY_HEIGHT = 32;
static const uint8_t DISPLAY_ADDRESS = 0x3C;

static const uint8_t DISPLAY_CHAR_WIDTH_X1 = 5;
static const uint8_t DISPLAY_CHAR_HEIGHT_X1 = 7;
static const uint8_t DISPLAY_CHAR_SPACING_X1 = 1;
static const uint8_t DISPLAY_CHAR_WIDTH_X2 = 10;
static const uint8_t DISPLAY_CHAR_HEIGHT_X2 = 14;
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

static const uint8_t DISPLAY_WIDGET_DOTGROUP_WIDTH = (MODE_COUNT - 1) * DISPLAY_WIDGET_DOT_SIZE_X1 + DISPLAY_WIDGET_DOT_SIZE_X2 + (MODE_COUNT - 1) * DISPLAY_MARGIN_X2;
static const uint8_t DISPLAY_WIDGET_DOTGROUP_HEIGHT = DISPLAY_WIDGET_DOT_SIZE_X2;

static const SQ15x16 DISPLAY_SCROLL_SPEED_X2 = 3.0; // Chars per second
static const SQ15x16 DISPLAY_SCROLL_SPEED_X1 = 4.0; // Chars per second
static const SQ15x16 DISPLAY_SCROLL_IDLE_TIME = 3.0; // Seconds

// - Mode specific
static const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX = 7;
static const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH = DISPLAY_GAME_EDIT_CHAR_MAX * DISPLAY_CHAR_WIDTH_X1 + DISPLAY_GAME_EDIT_CHAR_MAX - 1;
static const uint8_t DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT = 7;
static const uint8_t DISPLAY_GAME_VOLUMEBAR_WIDTH = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH - DISPLAY_MARGIN_X2 - 2 - DISPLAY_MARGIN_X1 * 2;

static const uint32_t resetAfterInactivity = 4000;
