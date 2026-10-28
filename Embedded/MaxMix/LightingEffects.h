#pragma once
#include "Config.h"

// Pure frame generation: no waits, no I/O and no heap allocation.
namespace LightingEffects
{
    inline Color Scale(Color color, uint8_t amount)
    {
        return Color(uint16_t(color.r) * amount / 255, uint16_t(color.g) * amount / 255, uint16_t(color.b) * amount / 255);
    }
    inline Color Blend(Color a, Color b, uint8_t amount)
    {
        return Color((uint16_t(a.r) * (255 - amount) + uint16_t(b.r) * amount) / 255,
                     (uint16_t(a.g) * (255 - amount) + uint16_t(b.g) * amount) / 255,
                     (uint16_t(a.b) * (255 - amount) + uint16_t(b.b) * amount) / 255);
    }
    inline uint8_t Pulse(uint32_t now)
    {
        uint16_t phase = now % 1800;
        uint16_t triangle = phase <= 900 ? phase : 1800 - phase;
        return 30 + uint32_t(triangle) * 150 / 900;
    }
    inline uint8_t Level(uint8_t volume, uint8_t count, uint8_t index)
    {
        uint16_t total = uint32_t(volume > 100 ? 100 : volume) * count * 255 / 100;
        uint16_t used = uint16_t(index) * 255;
        return total <= used ? 0 : (total - used >= 255 ? 255 : total - used);
    }
    inline void Render(DisplayMode mode, const SessionData &current, const SessionData &alternate,
                       const DeviceSettings &settings, const LightingSettings &palette, bool asleep, uint32_t now, Color *pixels)
    {
        for (uint8_t i = 0; i < PIXELS_COUNT; i++) pixels[i] = Color();
        if (asleep) return;
        if (mode == DisplayMode::MODE_SPLASH)
        {
            uint8_t head = (now / 160) % PIXELS_COUNT;
            pixels[head] = Scale(palette.disconnectedColor, 180);
            pixels[(head + PIXELS_COUNT - 1) % PIXELS_COUNT] = Scale(palette.disconnectedColor, 32);
            return;
        }
        if (mode == DisplayMode::MODE_GAME)
        {
            for (uint8_t i = 0; i < PIXELS_COUNT / 2; i++)
            {
                pixels[i] = current.data.isMuted ? Scale(palette.muteColor, Pulse(now)) : Scale(settings.mixChannelAColor, Level(current.data.volume, PIXELS_COUNT / 2, i));
                pixels[PIXELS_COUNT - 1 - i] = alternate.data.isMuted ? Scale(palette.muteColor, Pulse(now)) : Scale(settings.mixChannelBColor, Level(alternate.data.volume, PIXELS_COUNT / 2, i));
            }
            return;
        }
        if (current.data.isMuted)
        {
            for (uint8_t i = 0; i < PIXELS_COUNT; i++) pixels[i] = Scale(palette.muteColor, Pulse(now));
            return;
        }
        Color color = Blend(settings.volumeMinColor, settings.volumeMaxColor, uint16_t(current.data.volume > 100 ? 100 : current.data.volume) * 255 / 100);
        if (mode == DisplayMode::MODE_INPUT) color = palette.inputColor;
        if (mode == DisplayMode::MODE_APPLICATION) color = palette.applicationColor;
        for (uint8_t i = 0; i < PIXELS_COUNT; i++)
        {
            uint8_t index = mode == DisplayMode::MODE_INPUT ? PIXELS_COUNT - 1 - i : i;
            if (mode == DisplayMode::MODE_APPLICATION)
                index = i < PIXELS_COUNT / 2 ? i : PIXELS_COUNT - 1 - i;
            pixels[i] = Scale(color, Level(current.data.volume, mode == DisplayMode::MODE_APPLICATION ? PIXELS_COUNT / 2 : PIXELS_COUNT, index));
        }
        // Small mode markers remain visible at zero volume.
        if (mode == DisplayMode::MODE_INPUT) pixels[0] = Scale(palette.inputColor, 70);
        else if (mode == DisplayMode::MODE_APPLICATION) pixels[0] = Scale(palette.applicationColor, 70);
    }
}
