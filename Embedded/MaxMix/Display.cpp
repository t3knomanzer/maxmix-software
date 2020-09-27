#include "Display.h"

#include "Config.h"
#include "Logo.h"

namespace Display
{
    //---------------------------------------------------------
    // Timers & Timer Functions
    //---------------------------------------------------------
    static SQ15x16 displayTimer[] = {0, 0};

    void UpdateTimers(uint32_t delta)
    {
        SQ15x16 dt = SQ15x16(delta) / 1000;
        displayTimer[0] += dt;
        displayTimer[1] += dt;
    }

    void ResetTimers()
    {
        displayTimer[0] = 0;
        displayTimer[1] = 0;
    }

    //---------------------------------------------------------
    // Display & Display Functions
    //---------------------------------------------------------
    static Adafruit_SSD1306 display(DISPLAY_WIDTH, DISPLAY_HEIGHT, &Wire, DISPLAY_RESET);

    void Initialize(void)
    {
        Wire.setClock(DISPLAY_SPEED);
        display.begin(SSD1306_SWITCHCAPVCC, DISPLAY_ADDRESS);
        display.setRotation(2);
        display.setTextWrap(false);
    }

    void Sleep(void)
    {
        // TODO: replace with display off
        display.clearDisplay();
        display.display();
    }

    //---------------------------------------------------------
    // Volume Bar Functions
    //---------------------------------------------------------
    static void DrawVolumeBar(SessionData *item, uint8_t x0, uint8_t y0, uint8_t maxWidth, uint8_t height)
    {
        // Min limit
        uint8_t y1 = y0 + height - 1;

        display.drawLine(x0, y0, x0, y1, WHITE);

        // Bar
        x0 += 1 + DISPLAY_MARGIN_X1;
        SQ7x8 width = item->data.volume / (SQ7x8)100 * maxWidth;

        if (width > 0)
        {
            if (item->data.isMuted)
                display.drawRect(x0, y0, width.getInteger(), height, WHITE);
            else
                display.fillRect(x0, y0, width.getInteger(), height, WHITE);
        }

        // Max limit
        x0 += maxWidth + DISPLAY_MARGIN_X1;
        display.drawLine(x0, y0, x0, y1, WHITE);
    }

    //---------------------------------------------------------
    // Item Functions
    //---------------------------------------------------------
    static void DrawItemName(const char *name, uint8_t fontSize, uint8_t charWidth, uint8_t charHeight, uint8_t charSpacing, uint8_t charMax, uint8_t x, uint8_t y, uint8_t timerIndex, SQ15x16 scrollSpeed)
    {
        uint8_t nameLength = strlen(name);
        SQ15x16 scrollMax = (nameLength + 1) * (charWidth + charSpacing);
        SQ15x16 scroll = 0;
        if (nameLength > charMax)
            scroll = max(0, displayTimer[timerIndex] - DISPLAY_SCROLL_IDLE_TIME) * scrollMax / (nameLength / scrollSpeed);

        if (abs(scroll) >= abs(scrollMax))
            displayTimer[timerIndex] = 0;

        display.setTextSize(fontSize);
        display.setTextColor(WHITE);
        display.setCursor(x - scroll.getInteger(), y);

        uint8_t nameCopies = scroll == 0 ? 1 : 2;
        while (nameCopies > 0)
        {
            display.print(name);
            display.print(' ');
            nameCopies--;
        }

        // clear margins
        display.fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, charHeight, BLACK);
        display.fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, charHeight, BLACK);
    }

    static void DrawGameEditItem(SessionData *item, uint8_t y0, uint8_t timerIndex)
    {
        // Name
        display.setTextSize(1);
        display.setTextColor(WHITE);
        display.setCursor(DISPLAY_AREA_CENTER_MARGIN_SIDE, y0);

        // Item name
        DrawItemName(item->name, 1, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_HEIGHT_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_GAME_EDIT_CHAR_MAX, DISPLAY_AREA_CENTER_MARGIN_SIDE, y0, timerIndex, DISPLAY_SCROLL_SPEED_X1);

        // Clear sides
        display.fillRect(0, y0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);
        display.fillRect(DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH, y0, DISPLAY_WIDTH, DISPLAY_CHAR_HEIGHT_X1, BLACK);

        // Volume bar min indicator
        DrawVolumeBar(item, DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH + DISPLAY_MARGIN_X2, y0, DISPLAY_GAME_VOLUMEBAR_WIDTH, DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT);
    }

    //---------------------------------------------------------
    // Screen Element Functions
    //---------------------------------------------------------
    static void DrawSelectionChannelName(char channel)
    {
        display.setTextSize(1);
        display.setTextColor(WHITE);
        display.setCursor(0, DISPLAY_AREA_CENTER_HEIGHT - DISPLAY_CHAR_HEIGHT_X1 - 1);

        display.print(channel);
    }

    static void DrawVolumeNumber(uint8_t volume, uint8_t x0, uint8_t y0)
    {
        x0 = x0 - DISPLAY_CHAR_WIDTH_X2;
        if (volume > 9)
            x0 = x0 - DISPLAY_CHAR_WIDTH_X2 - DISPLAY_CHAR_SPACING_X2;
        if (volume > 99)
            x0 = x0 - DISPLAY_CHAR_WIDTH_X2 - DISPLAY_CHAR_SPACING_X2;

        display.setTextSize(2);
        display.setTextColor(WHITE);
        display.setCursor(x0, y0);

        display.print(volume);
    }

    static void DrawDotGroup(uint8_t index)
    {
        uint8_t px, py, x0, y0, dotSize;

        px = DISPLAY_WIDTH / 2 - DISPLAY_WIDGET_DOTGROUP_WIDTH / 2;
        py = DISPLAY_HEIGHT - DISPLAY_WIDGET_DOTGROUP_HEIGHT / 2;

        x0 = px;
        y0 = py;

        for (uint8_t i = 0; i < DisplayMode::MODE_MAX; i++)
        {
            dotSize = i == index ? DISPLAY_WIDGET_DOT_SIZE_X2 : DISPLAY_WIDGET_DOT_SIZE_X1;
            y0 = py - dotSize / 2;

            display.fillRect(x0, y0, dotSize, dotSize, WHITE);
            x0 += dotSize + DISPLAY_MARGIN_X2;
        }
    }

    static void DrawSelectionArrows(bool leftArrow, bool rightArrow)
    {
        uint8_t x0, y0, x1, y1, x2, y2;

        if (leftArrow)
        {
            x0 = 0;
            y0 = DISPLAY_MARGIN_X2 + DISPLAY_WIDGET_ARROW_SIZE_X1;
            x1 = x0 + DISPLAY_WIDGET_ARROW_SIZE_X1;
            y1 = y0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            x2 = x0 + DISPLAY_WIDGET_ARROW_SIZE_X1;
            y2 = y0 + DISPLAY_WIDGET_ARROW_SIZE_X1;

            display.fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
        }

        if (rightArrow)
        {
            x0 = DISPLAY_WIDTH - 1;
            y0 = DISPLAY_MARGIN_X2 + DISPLAY_WIDGET_ARROW_SIZE_X1;
            x1 = x0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            y1 = y0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            x2 = x0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            y2 = y0 + DISPLAY_WIDGET_ARROW_SIZE_X1;

            display.fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
        }
    }

    //---------------------------------------------------------
    // MaxMix Logo screen
    //---------------------------------------------------------
    void SplashScreen(void)
    {
        display.clearDisplay();
        display.drawBitmap(0, 0, LOGOBMP, LOGO_WIDTH, LOGO_HEIGHT, 1);
        display.display();
    }

    //---------------------------------------------------------
    // Firmware Version screen
    //---------------------------------------------------------
    void InfoScreen(void)
    {
        display.clearDisplay();

        display.setTextColor(WHITE);
        display.setTextSize(1);

        display.setCursor(0, (DISPLAY_HEIGHT / 2) - DISPLAY_CHAR_HEIGHT_X1);
        display.print("FW: ");
        display.print(VERSION_MAJOR);
        display.print(".");
        display.print(VERSION_MINOR);
        display.print(".");
        display.print(VERSION_PATCH);

        display.setCursor(0, (DISPLAY_HEIGHT / 2) + DISPLAY_CHAR_SPACING_X2);
        display.print(F("Built " __DATE__));

        display.display();
    }

    //---------------------------------------------------------
    // Output Mode screens
    //---------------------------------------------------------
    void DeviceSelectScreen(SessionData *item, bool leftArrow, bool rightArrow, uint8_t modeIndex)
    {
        display.clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(item->name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_CHAR_MAX_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        DrawSelectionArrows(leftArrow, rightArrow);
        DrawVolumeBar(item, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2 + DISPLAY_MARGIN_X2, DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1);

        if (item->data.isDefault)
            DrawSelectionChannelName('*');

        display.display();
    }

    void DeviceEditScreen(SessionData *item, const char *label, uint8_t modeIndex)
    {
        display.clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(label, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_CHAR_MAX_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        DrawVolumeBar(item, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2 + DISPLAY_MARGIN_X2, DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1);
        DrawVolumeNumber(item->data.volume, DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH, 0);

        display.display();
    }

    //---------------------------------------------------------
    // Application Mode screens
    //---------------------------------------------------------
    void ApplicationSelectScreen(SessionData *item, bool leftArrow, bool rightArrow, uint8_t modeIndex)
    {
        display.clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(item->name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_CHAR_MAX_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        DrawSelectionArrows(leftArrow, rightArrow);
        DrawVolumeBar(item, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2 + DISPLAY_MARGIN_X2, DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1);

        display.display();
    }

    void ApplicationEditScreen(SessionData *item, uint8_t modeIndex)
    {
        display.clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(item->name, 1, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_HEIGHT_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_CHAR_MAX_X1, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X1);
        DrawVolumeBar(item, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2, DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X2, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2);
        DrawVolumeNumber(item->data.volume, DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2);

        display.display();
    }

    //---------------------------------------------------------
    // Game Mode screens
    //---------------------------------------------------------
    void GameSelectScreen(SessionData *item, char channel, bool leftArrow, bool rightArrow, uint8_t modeIndex)
    {
        display.clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(item->name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_CHAR_MAX_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        DrawSelectionArrows(leftArrow, rightArrow);
        DrawVolumeBar(item, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2 + DISPLAY_MARGIN_X2, DISPLAY_WIDGET_VOLUMEBAR_WIDTH_X1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1);
        DrawSelectionChannelName(channel);

        display.display();
    }

    void GameEditScreen(SessionData *itemA, SessionData *itemB, uint8_t modeIndex)
    {
        display.clearDisplay();

        DrawDotGroup(modeIndex);
        DrawGameEditItem(itemA, DISPLAY_MARGIN_X2, DISPLAY_TIMER_A);
        DrawGameEditItem(itemB, DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2 * 2 + DISPLAY_MARGIN_X1, DISPLAY_TIMER_B);

        display.display();
    }
}; // namespace Display
