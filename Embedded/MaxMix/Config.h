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


//********************************************************
// *** CONFIG OPTIONS
//********************************************************
// Uncomment if your hardware requires half-stepping
// #define HALF_STEP

//********************************************************
// *** CONSTS
//********************************************************
// --- Serial Comms
static PROGMEM const uint32_t BAUD_RATE = 115200;

// --- Pins
static PROGMEM const uint8_t  PIN_PIXELS = 12; //D12
static PROGMEM const uint8_t  PIN_ENCODER_OUTA = 15; //A1
static PROGMEM const uint8_t  PIN_ENCODER_OUTB = 16; //A2
static PROGMEM const uint8_t  PIN_ENCODER_SWITCH = 17; //A3

// --- States
static PROGMEM const uint8_t  MODE_MASTER = 0;
static PROGMEM const uint8_t  MODE_APPLICATION = 1;
static PROGMEM const uint8_t  MODE_GAME = 2;
static PROGMEM const uint8_t  MODE_COUNT = 3;

static PROGMEM const uint8_t  STATE_APPLICATION_NAVIGATE = 0;
static PROGMEM const uint8_t  STATE_APPLICATION_EDIT = 1;
static PROGMEM const uint8_t  STATE_APPLICATION_COUNT = 2;

static PROGMEM const uint8_t  STATE_GAME_SELECT_A = 0;
static PROGMEM const uint8_t  STATE_GAME_SELECT_B = 1;
static PROGMEM const uint8_t  STATE_GAME_EDIT = 2;
static PROGMEM const uint8_t  STATE_GAME_COUNT = 3;

static PROGMEM const uint8_t  STATE_DISPLAY_AWAKE = 0;
static PROGMEM const uint8_t  STATE_DISPLAY_SLEEP = 1;

static PROGMEM const uint8_t  STATE_DISPLAY_NOT_SCROLLING = 0;
static PROGMEM const uint8_t  STATE_DISPLAY_SCROLLING = 1;

// --- Display
static PROGMEM const uint8_t  DISPLAY_RESET =   4; // Reset pin # (or -1 if sharing Arduino reset pin)

// --- Lighting
static PROGMEM const uint8_t  PIXELS_NUM = 8; // Number of pixels in ring

// --- Rotary Encoder
static PROGMEM const uint16_t ROTARY_ACCELERATION_DIVISOR_MAX = 400;

// --- Messages
static PROGMEM const uint8_t ITEM_MAX_COUNT = 8;
static PROGMEM const uint8_t ITEM_BUFFER_NAME_SIZE = 36;
static PROGMEM const uint8_t RECEIVE_BUFFER_SIZE = 128;
static PROGMEM const uint8_t SEND_BUFFER_SIZE = 8;

// These values match exactly the ones in the C# application.
static PROGMEM const uint8_t MSG_COMMAND_HS_REQUEST =  0;
static PROGMEM const uint8_t MSG_COMMAND_HS_RESPONSE =  1;
static PROGMEM const uint8_t MSG_COMMAND_ADD =  2;
static PROGMEM const uint8_t MSG_COMMAND_REMOVE =  3;
static PROGMEM const uint8_t MSG_COMMAND_UPDATE_VOLUME =  4;
static PROGMEM const uint8_t MSG_COMMAND_SETTINGS =  5;

static const uint8_t MSG_PACKET_DELIMITER = 0;

// --- Screen Drawing
static PROGMEM const uint8_t DISPLAY_WIDTH = 128;
static PROGMEM const uint8_t DISPLAY_HEIGHT = 32;
static PROGMEM const uint8_t DISPLAY_ADDRESS = 0x3C;

static PROGMEM const uint8_t DISPLAY_CHAR_WIDTH_X1 = 5;
static PROGMEM const uint8_t DISPLAY_CHAR_HEIGHT_X1 = 7;
static PROGMEM const uint8_t DISPLAY_CHAR_SPACING_X1 = 1;
static PROGMEM const uint8_t DISPLAY_CHAR_WIDTH_X2 = 10;
static PROGMEM const uint8_t DISPLAY_CHAR_HEIGHT_X2 = 14;
static PROGMEM const uint8_t DISPLAY_CHAR_SPACING_X2 = 2;

static PROGMEM const uint8_t DISPLAY_CHAR_MAX_X1 = 18;
static PROGMEM const uint8_t DISPLAY_CHAR_MAX_X2 = 9;
static PROGMEM const uint8_t DISPLAY_CHAR_MAX_WIDTH_X1 = DISPLAY_CHAR_MAX_X1 * DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_MAX_X1 - 1;
static PROGMEM const uint8_t DISPLAY_CHAR_MAX_WIDTH_X2 = DISPLAY_CHAR_MAX_X2 * DISPLAY_CHAR_WIDTH_X2 + DISPLAY_CHAR_MAX_X2 - 1;

static PROGMEM const uint8_t DISPLAY_MARGIN_X1 = 2;
static PROGMEM const uint8_t DISPLAY_MARGIN_X2 = 4;
static PROGMEM const uint8_t DISPLAY_MARGIN_X3 = 8;

static PROGMEM const uint8_t DISPLAY_AREA_CENTER_MARGIN_SIDE = 11;
static PROGMEM const uint8_t DISPLAY_AREA_CENTER_MARGIN_BOTTOM = 7;
static PROGMEM const uint8_t DISPLAY_AREA_CENTER_WIDTH = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE * 2;
static PROGMEM const uint8_t DISPLAY_AREA_CENTER_HEIGHT = DISPLAY_HEIGHT - DISPLAY_AREA_CENTER_MARGIN_BOTTOM;

static PROGMEM const uint8_t DISPLAY_WIDGET_DOT_SIZE_X1 = 2;
static PROGMEM const uint8_t DISPLAY_WIDGET_DOT_SIZE_X2 = 4;
static PROGMEM const uint8_t DISPLAY_WIDGET_ARROW_SIZE_X1 = 3;
static PROGMEM const uint8_t DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1 = 6;
static PROGMEM const uint8_t DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2 = 14;

static PROGMEM const uint8_t DISPLAY_WIDGET_DOTGROUP_WIDTH = (MODE_COUNT - 1) * DISPLAY_WIDGET_DOT_SIZE_X1 + DISPLAY_WIDGET_DOT_SIZE_X2 + (MODE_COUNT - 1) * DISPLAY_MARGIN_X2;
static PROGMEM const uint8_t DISPLAY_WIDGET_DOTGROUP_HEIGHT = DISPLAY_WIDGET_DOT_SIZE_X2;

// - Mode specific
static PROGMEM const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX = 7;
static PROGMEM const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH = DISPLAY_GAME_EDIT_CHAR_MAX * DISPLAY_CHAR_WIDTH_X1 + DISPLAY_GAME_EDIT_CHAR_MAX - 1;
static PROGMEM const uint8_t DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT = 7;

// - Item Name Scrolling
static PROGMEM const uint16_t DISPLAY_SCROLL_DELAY_INITIAL = 3000;  //  in ms
static PROGMEM const uint16_t DISPLAY_SCROLL_DELAY_REVERSE = 1000;  //  in ms
static PROGMEM const uint8_t DISPLAY_SCROLL_STEP_INTERVAL_X1 = 30;  //  in ms
static PROGMEM const uint8_t DISPLAY_SCROLL_STEP_INTERVAL_X2 = 20;  //  in ms
static PROGMEM const uint16_t DISPLAY_SCROLL_OFFSET_UNSET = 0xFFFF;
static PROGMEM const char DISPLAY_SCROLL_ITEM_SEPARATOR[] = "   ";
