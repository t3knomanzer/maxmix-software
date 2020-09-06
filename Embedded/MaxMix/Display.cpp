#include "Display.h"

#include "Config.h"
#include "Logo.h"

namespace Display
{
    Adafruit_SSD1306 *display;
    SQ15x16 displayTimer[] = {0, 0};

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

    static SQ15x16 Scroll(char *name, SQ15x16 time, SQ15x16 scrollMin, SQ15x16 scrollMax, SQ15x16 speed)
    {
        uint8_t nameLength = strlen(name);
        if (nameLength <= DISPLAY_CHAR_MAX_X2)
            return 0;

        // Value mapping: (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min
        SQ15x16 timeMax = nameLength / speed;
        SQ15x16 scroll = (time - 0) * (scrollMax - scrollMin) / (timeMax - 0) + scrollMin;

        return scroll;
    }

    void Initialize(void)
    {
        Wire.setClock(DISPLAY_SPEED);
        display = new Adafruit_SSD1306(DISPLAY_WIDTH, DISPLAY_HEIGHT, &Wire, DISPLAY_RESET);
        display->begin(SSD1306_SWITCHCAPVCC, DISPLAY_ADDRESS);
        display->setRotation(2);
        display->setTextWrap(false);
    }

    void Sleep(void)
    {
        // TODO: replace
        display->clearDisplay();
        display->display();
    }

    void SplashScreen(void)
    {
        // LOGO is 128v32, no need to clear display, just draw and go
        display->drawBitmap(0, 0, LOGOBMP, LOGO_WIDTH, LOGO_HEIGHT, 1);
        display->display();
    }

    void DrawItemName(char *name, uint8_t fontSize, uint8_t charWidth, uint8_t charHeight, uint8_t charSpacing, uint8_t x, uint8_t y, uint8_t timerIndex, SQ15x16 scrollSpeed)
    {
        uint8_t nameCopies = 1;
        uint8_t nameLength = strlen(name);
        SQ15x16 scrollMin = 0;
        SQ15x16 scrollMax = (nameLength + 1) * (charWidth + charSpacing);
        SQ15x16 scroll = Scroll(name, max(0, displayTimer[timerIndex] - DISPLAY_SCROLL_IDLE_TIME), scrollMin, scrollMax, scrollSpeed);

        if (abs(scroll) >= abs(scrollMax))
            displayTimer[timerIndex] = 0;

        if (abs(scroll) > 0)
            nameCopies = 2;

        display->setTextSize(fontSize);
        display->setTextColor(WHITE);
        display->setCursor(x - scroll.getInteger(), y);
        while (nameCopies > 0)
        {
            for (size_t i = 0; i < nameLength; i++)
                display->print(name[i]);

            display->print(' ');
            nameCopies--;
        }
    }

    void DrawSelectionChannelName(char channel)
    {
        display->setTextSize(1);
        display->setTextColor(WHITE);
        display->setCursor(0, DISPLAY_AREA_CENTER_HEIGHT - DISPLAY_CHAR_HEIGHT_X1 - 1);

        display->print(channel);
    }

    void DrawSelectionVolumeBar(Item *item)
    {
        uint8_t x0, y0, y1;

        // Min limit
        x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE;
        y0 = DISPLAY_CHAR_HEIGHT_X2 + DISPLAY_MARGIN_X2;
        y1 = y0 + DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1 - 1;

        display->drawLine(x0, y0, x0, y1, WHITE);

        // Bar
        x0 += 1 + DISPLAY_MARGIN_X1;
        uint8_t width = DISPLAY_AREA_CENTER_WIDTH - 2 - DISPLAY_MARGIN_X1 * 2;
        width = map(item->volume, 0, 100, 0, width);

        if (width > 0)
        {
            if (item->isMuted)
                display->drawRect(x0, y0, width, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1, WHITE);
            else
                display->fillRect(x0, y0, width, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X1, WHITE);
        }

        // Max limit
        x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - 1;

        display->drawLine(x0, y0, x0, y1, WHITE);
    }

    void DrawSelectionItemVolume(uint8_t volume)
    {
        uint8_t x0;

        if (volume < 10)
            x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2;
        else if (volume < 100)
            x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2 * 2 - DISPLAY_CHAR_SPACING_X2 * 2;
        else if (volume == 100)
            x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_AREA_CENTER_WIDTH - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 3;

        display->setTextSize(2);
        display->setTextColor(WHITE);
        display->setCursor(x0, 0);

        display->print(volume);
    }

    //---------------------------------------------------------
    // Draws a row of the Game mode screen
    //---------------------------------------------------------
    void DrawGameEditItem(Item *item, uint8_t px, uint8_t py, uint8_t timerIndex)
    {
        // Name
        display->setTextSize(1);
        display->setTextColor(WHITE);
        display->setCursor(px, py);

        // Item name
        DrawItemName(item->name, 1, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_HEIGHT_X1, DISPLAY_CHAR_SPACING_X1, px, py, timerIndex, DISPLAY_SCROLL_SPEED_X1);

        // Clear sides
        display->fillRect(0, py, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);
        display->fillRect(DISPLAY_AREA_CENTER_MARGIN_SIDE + DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH, py, DISPLAY_WIDTH, DISPLAY_CHAR_HEIGHT_X1, BLACK);

        // Volume bar min indicator
        px += DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH + DISPLAY_MARGIN_X2;
        display->drawLine(px, py, px, py + DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT - 1, WHITE);

        // Volume bar
        px += 1 + DISPLAY_MARGIN_X1;
        uint8_t maxWidth = DISPLAY_AREA_CENTER_WIDTH - DISPLAY_GAME_EDIT_CHAR_MAX_WIDTH - DISPLAY_MARGIN_X2 - 2 - DISPLAY_MARGIN_X1 * 2;
        uint8_t width = map(item->volume, 0, 100, 0, maxWidth);

        if (width > 0)
        {
            if (item->isMuted)
                display->drawRect(px, py, width, DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT, WHITE);
            else
                display->fillRect(px, py, width, DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT, WHITE);
        }

        // Volume bar min indicator
        px += maxWidth + DISPLAY_MARGIN_X1;
        display->drawLine(px, py, px, py + DISPLAY_GAME_WIDGET_VOLUMEBAR_HEIGHT - 1, WHITE);
    }

    //---------------------------------------------------------
    // Draw Mode Indicator
    // Horizontal alignment: display center
    // Vertical alignment: display bottom
    //---------------------------------------------------------
    void DrawDotGroup(uint8_t index)
    {
        uint8_t px, py, x0, y0, dotSize;

        px = DISPLAY_WIDTH / 2 - DISPLAY_WIDGET_DOTGROUP_WIDTH / 2;
        py = DISPLAY_HEIGHT - DISPLAY_WIDGET_DOTGROUP_HEIGHT / 2;

        x0 = px;
        y0 = py;

        for (uint8_t i = 0; i < MODE_COUNT; i++)
        {
            dotSize = i == index ? DISPLAY_WIDGET_DOT_SIZE_X2 : DISPLAY_WIDGET_DOT_SIZE_X1;
            y0 = py - dotSize / 2;

            display->fillRect(x0, y0, dotSize, dotSize, WHITE);
            x0 += dotSize + DISPLAY_MARGIN_X2;
        }
    }

    //---------------------------------------------------------
    // Draw Selection Arrows
    //---------------------------------------------------------
    void DrawSelectionArrows(uint8_t leftArrow, uint8_t rightArrow)
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

            display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
        }

        if (rightArrow)
        {
            x0 = DISPLAY_WIDTH - 1;
            y0 = DISPLAY_MARGIN_X2 + DISPLAY_WIDGET_ARROW_SIZE_X1;
            x1 = x0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            y1 = y0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            x2 = x0 - DISPLAY_WIDGET_ARROW_SIZE_X1;
            y2 = y0 + DISPLAY_WIDGET_ARROW_SIZE_X1;

            display->fillTriangle(x0, y0, x1, y1, x2, y2, WHITE);
        }
    }

    void DrawEditVolumeBar(Item *item)
    {
        uint8_t x0, y0, x1, y1;

        // Min limit
        x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE;
        y0 = DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2;
        y1 = y0 + DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2 - 1;

        display->drawLine(x0, y0, x0, y1, WHITE);

        // Bar
        x0 += 1 + DISPLAY_MARGIN_X1;
        x1 = DISPLAY_AREA_CENTER_WIDTH - 2 - DISPLAY_MARGIN_X1 * 3 - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 2;
        x1 = map(item->volume, 0, 100, 0, x1);

        if (x1 > 0)
        {
            if (item->isMuted)
                display->drawRect(x0, y0, x1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2, WHITE);
            else
                display->fillRect(x0, y0, x1, DISPLAY_WIDGET_VOLUMEBAR_HEIGHT_X2, WHITE);
        }

        // Max limit
        x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 3 - DISPLAY_CHAR_SPACING_X2 * 2 - DISPLAY_MARGIN_X1;

        display->drawLine(x0, y0, x0, y1, WHITE);
    }

    void DrawEditVolume(uint8_t volume)
    {
        uint8_t x0, y0;

        x0 = DISPLAY_AREA_CENTER_MARGIN_SIDE;
        y0 = DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2;

        if (volume < 10)
            x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2;
        else if (volume < 100)
            x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 2;
        else if (volume == 100)
            x0 = DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE - DISPLAY_CHAR_WIDTH_X2 * 3;

        display->setTextSize(2);
        display->setTextColor(WHITE);
        display->setCursor(x0, y0);
        display->print(volume);
    }

    //---------------------------------------------------------
    // Draws the output mode screen
    //---------------------------------------------------------
    void OutputSelectScreen(Item *item, bool isDefaultEndpoint, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex)
    {
        display->clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(item->name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
        display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
        DrawSelectionArrows(leftArrow, rightArrow);
        DrawSelectionVolumeBar(item);

        if (isDefaultEndpoint)
            DrawSelectionChannelName('O');

        display->display();
    }

    void OutputEditScreen(Item *item, uint8_t modeIndex)
    {
        display->clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName("VOL", 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        DrawSelectionItemVolume(item->volume);
        DrawSelectionVolumeBar(item);

        display->display();
    }

    //---------------------------------------------------------
    // Draws the Application mode selection screen
    //---------------------------------------------------------
    void ApplicationSelectScreen(Item *item, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex)
    {
        display->clearDisplay();

        DrawDotGroup(modeIndex);

        DrawItemName(item->name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        // Clear sides
        display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
        display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);

        DrawSelectionArrows(leftArrow, rightArrow);
        DrawSelectionVolumeBar(item);

        display->display();
    }

    void ApplicationEditScreen(Item *item, uint8_t modeIndex)
    {
        display->clearDisplay();

        DrawDotGroup(modeIndex);
        DrawItemName(item->name, 1, DISPLAY_CHAR_WIDTH_X1, DISPLAY_CHAR_HEIGHT_X1, DISPLAY_CHAR_SPACING_X1, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X1);
        // Clear sides
        display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);
        display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X1, BLACK);

        DrawEditVolumeBar(item);
        DrawEditVolume(item->volume);
        display->display();
    }

    //---------------------------------------------------------
    // Draws the Application mode selection screen
    //---------------------------------------------------------
    void GameSelectScreen(Item *item, char channel, uint8_t leftArrow, uint8_t rightArrow, uint8_t modeIndex)
    {
        display->clearDisplay();

        DrawDotGroup(modeIndex);

        DrawItemName(item->name, 2, DISPLAY_CHAR_WIDTH_X2, DISPLAY_CHAR_HEIGHT_X2, DISPLAY_CHAR_SPACING_X2, DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_TIMER_A, DISPLAY_SCROLL_SPEED_X2);
        // Clear sides
        display->fillRect(0, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);
        display->fillRect(DISPLAY_WIDTH - DISPLAY_AREA_CENTER_MARGIN_SIDE, 0, DISPLAY_AREA_CENTER_MARGIN_SIDE, DISPLAY_CHAR_HEIGHT_X2, BLACK);

        DrawSelectionArrows(leftArrow, rightArrow);
        DrawSelectionVolumeBar(item);
        DrawSelectionChannelName(channel);

        display->display();
    }
    
    void GameEditScreen(Item *itemA, Item *itemB, uint8_t modeIndex)
    {
        uint8_t py;

        display->clearDisplay();

        DrawDotGroup(modeIndex);

        py = DISPLAY_MARGIN_X2;
        DrawGameEditItem(itemA, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, DISPLAY_TIMER_A);

        py = DISPLAY_MARGIN_X2 + DISPLAY_CHAR_HEIGHT_X1 + DISPLAY_MARGIN_X2 + DISPLAY_MARGIN_X1;
        DrawGameEditItem(itemB, DISPLAY_AREA_CENTER_MARGIN_SIDE, py, DISPLAY_TIMER_B);

        display->display();
    }
}; // namespace Display