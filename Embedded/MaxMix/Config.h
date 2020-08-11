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
static const uint8_t  STATE_READY = 0;
static const uint8_t  STATE_PACKAGE_RECEIVED = 1;
static const uint8_t  STATE_PACKAGE_DECODED = 2;
static const uint8_t  STATE_PACKAGE_PROCESSED = 3;
static const uint8_t  STATE_PACKAGE_ERROR = 4;

static const uint8_t  INPUT_STATE_READY = 0;
static const uint8_t  INPUT_STATE_AVAILABLE = 1;

// --- Display
static const uint8_t  DISPLAY_RESET =   4; // Reset pin # (or -1 if sharing Arduino reset pin)

// --- Lighting
static const uint8_t  PIXELS_NUM = 8; // Number of pixels in ring

// --- Messages
static const uint16_t RECEIVE_BUFFER_SIZE = 512 + 8; // 4096 + 64 bits
static const uint8_t SEND_BUFFER_SIZE = 8;

// These values match exactly the ones in the C# application.
static const uint8_t MSG_COMMAND_HS_REQUEST =  0;
static const uint8_t MSG_COMMAND_HS_RESPONSE =  1;
static const uint8_t MSG_COMMAND_DISPLAY_DATA =  2;

static const uint8_t MSG_PACKET_DELIMITER = 0;

// --- Screen Drawing
static const uint8_t DISPLAY_WIDTH = 128;
static const uint8_t DISPLAY_HEIGHT = 32;
static const uint8_t DISPLAY_ADDRESS = 0x3C;

// DBG
static const uint8_t DBG_HISTORY_SIZE = 4;
static const uint8_t DBG_HISTORY_ITEM_SIZE = 24;