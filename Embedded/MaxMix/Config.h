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
static const uint8_t  MODE_MASTER = 0;
static const uint8_t  MODE_APPLICATION = 1;
static const uint8_t  MODE_GAME = 2;

static const uint8_t  STATE_APPLICATION_NAVIGATE = 0;
static const uint8_t  STATE_APPLICATION_EDIT = 1;

static const uint8_t  STATE_SCREEN_AWAKE = 0;
static const uint8_t  STATE_SCREEN_SLEEP = 1;

// --- Display
static const uint8_t  SCREEN_WIDTH = 128; // Width in pixels
static const uint8_t  SCREEN_HEIGHT = 32; // Height in pixels
static const uint8_t  SCREEN_RESET =   4; // Reset pin # (or -1 if sharing Arduino reset pin)

// --- Lighting
static const uint8_t  PIXELS_NUM = 8; // Number of pixels in ring

// --- Messages
static const uint8_t ITEM_BUFFER_SIZE = 10;
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