//********************************************************
// PROJECT: MAXMIX
// ORIGINAL AUTHOR: Ruben Henares
// MODIFIED BY: Collin Ostrowski 
//
// ORIGINAL AUTHOR EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
// 
//
//
//********************************************************

//********************************************************
// *** CONSTS
//********************************************************
// --- Serial Comms
static const uint32_t BAUD_RATE = 115200;

// --- Pins
static const uint8_t  PIN_PIXELS = 12; //D12
static const uint8_t  PIN_ENCODER_OUTA = 15; //A1
static const uint8_t  PIN_ENCODER_OUTB = 16; //A2
static const uint8_t  PIN_ENCODER_SWITCH = 2; // Changed to D2 and IS Button Pin
static const uint8_t POT_PIN = 14;

// --- Pot Settings
uint8_t PotVal_old = 0; //Needs to be declared so Functions can see it
static const uint8_t SMOOTH_COUNT = 5; //Number of Values to Average (Decreases Jumpyness but increases latency)
static const double MAX_ANALOG_VAL = 1022; 
static const uint8_t COUNT_PER_TICK = 5; //Number has to pass before menu change								  

// --- States
static const uint8_t  MODE_MASTER = 0;
static const uint8_t  MODE_APPLICATION = 1;
static const uint8_t  MODE_GAME = 2;
static const uint8_t  MODE_COUNT = 3;

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
//LCD
static const uint8_t AMOUNT_OF_COLS = 16; // How many possible columns in the LCD
static const uint8_t AMOUNT_OF_ROWS = 4;  // How many possible rows in the LCD
static const uint8_t RS = 7, EN = 8, D4 = 9, D5 = 10, D6 = 11, D7 = 12; //Pins on Arduino to LCD
static const double VOLUME_ON_SCREEN = 6.25;

// --- Lighting
static const uint8_t  PIXELS_NUM = 8; // Number of pixels in ring

// --- Messages
static const uint8_t ITEM_BUFFER_SIZE = 8;
static const uint8_t ITEM_BUFFER_NAME_SIZE = 36;
static const uint8_t RECEIVE_BUFFER_SIZE = 128;
static const uint8_t SEND_BUFFER_SIZE = 8;

// These values match exactly the ones in the C# application.
static const uint8_t MSG_COMMAND_HS_REQUEST =  0;
static const uint8_t MSG_COMMAND_HS_RESPONSE =  1;
static const uint8_t MSG_COMMAND_ADD =  2;
static const uint8_t MSG_COMMAND_REMOVE =  3;
static const uint8_t MSG_COMMAND_UPDATE_VOLUME =  4;
static const uint8_t MSG_COMMAND_SETTINGS =  5;

static const uint8_t MSG_PACKET_DELIMITER = 0;

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
static const uint8_t DISPLAY_CHAR_MAX_WIDTH_X1 = DISPLAY_CHAR_MAX_X1 * DISPLAY_CHAR_WIDTH_X1 + DISPLAY_CHAR_MAX_X1 - 1;
static const uint8_t DISPLAY_CHAR_MAX_WIDTH_X2 = DISPLAY_CHAR_MAX_X2 * DISPLAY_CHAR_WIDTH_X2 + DISPLAY_CHAR_MAX_X2 - 1;


static const uint8_t DISPLAY_MARGIN_X1 = 2;
static const uint8_t DISPLAY_MARGIN_X2 = 4;
static const uint8_t DISPLAY_MARGIN_X3 = 8;

static const uint8_t DISPLAY_AREA_CENTER_MARGIN_SIDE = 11;
static const uint8_t DISPLAY_AREA_CENTER_MARGIN_BOTTOM = 7;
static const uint8_t DISPLAY_AREA_CENTER_WIDTH = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE * 2;
static const uint8_t DISPLAY_AREA_CENTER_HEIGHT = DISPLAY_HEIGHT - DISPLAY_AREA_CENTER_MARGIN_BOTTOM;

static const uint8_t DISPLAY_WIDGET_DOT_SIZE_X1 = 2;
static const uint8_t DISPLAY_WIDGET_DOT_SIZE_X2 = 4;
static const uint8_t DISPLAY_WIDGET_ARROW_SIZE_X1 = 3;
static const uint8_t DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1 = 6;
static const uint8_t DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2 = 14;

static const uint8_t DISPLAY_WIDGET_DOTGROUP_WIDTH = (MODE_COUNT - 1) * DISPLAY_WIDGET_DOT_SIZE_X1 + DISPLAY_WIDGET_DOT_SIZE_X2 + (MODE_COUNT - 1) * DISPLAY_MARGIN_X2;
static const uint8_t DISPLAY_WIDGET_DOTGROUP_HEIGHT = DISPLAY_WIDGET_DOT_SIZE_X2;

// - Mode specific
static const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX = 11;
static const uint8_t DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH = DISPLAY_GAME_EDIT_CHAR_MAX * DISPLAY_CHAR_WIDTH_X1 + DISPLAY_GAME_EDIT_CHAR_MAX - 1;
static const uint8_t DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT = 7;

