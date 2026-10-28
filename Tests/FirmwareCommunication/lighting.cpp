#include "../../Embedded/MaxMix/LightingEffects.h"
#include <cstdio>
#include <cstring>
int main()
{
    int failures = 0;
    auto check = [&](bool result, const char *name) { std::printf("%s %s\n", result ? "PASS" : "FAIL", name); if (!result) failures++; };
    DeviceSettings settings; LightingSettings palette;
    SessionData a, b;
    Color output[8], input[8], apps[8], asleep[8];
    a.data.volume = 50;
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, false, 0, output);
    LightingEffects::Render(MODE_INPUT, a, b, settings, palette, false, 0, input);
    LightingEffects::Render(MODE_APPLICATION, a, b, settings, palette, false, 0, apps);
    check(std::memcmp(output, input, sizeof(output)) && std::memcmp(output, apps, sizeof(output)) && std::memcmp(input, apps, sizeof(output)), "audio modes have different LED patterns");
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, true, 0, asleep);
    Color black[8] = {};
    check(std::memcmp(asleep, black, sizeof(black)) == 0, "sleep turns all pixels off");
    a.data.volume = 0;
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, false, 0, output);
    check(std::memcmp(output, black, sizeof(black)) == 0, "zero output volume is dark");
    a.data.volume = 100;
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, false, 0, output);
    check(output[7].r == settings.volumeMaxColor.r, "full volume fills all eight LEDs");
    a.data.volume = 100; b.data.volume = 0;
    LightingEffects::Render(MODE_GAME, a, b, settings, palette, false, 0, output);
    check(output[0].b != 0 && output[4].r == 0 && output[7].b == 0, "game A and B show independent volumes");
    a.data.isMuted = true;
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, false, 0, output);
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, false, 900, input);
    check(output[0].r != input[0].r && input[0].g == 0 && input[0].b == 0, "mute has a slow red pulse");
    LightingEffects::Render(MODE_SPLASH, a, b, settings, palette, false, 0, output);
    LightingEffects::Render(MODE_SPLASH, a, b, settings, palette, false, 160, input);
    check(std::memcmp(output, input, sizeof(output)) != 0, "disconnected indicator rotates");
    for (unsigned volume = 0; volume < 128; volume++)
        for (unsigned i = 0; i < 8; i++)
            if (LightingEffects::Level(volume, 8, i) > 255) failures++;
    check(LightingEffects::Level(127, 8, 7) == 255, "volumes above 100 are clamped");
    palette.inputColor = Color(255, 0, 0);
    palette.applicationColor = Color(0, 255, 0);
    palette.muteColor = Color(0, 0, 255);
    palette.disconnectedColor = Color(255, 0, 255);
    a.data.isMuted = false; a.data.volume = 100;
    LightingEffects::Render(MODE_INPUT, a, b, settings, palette, false, 0, output);
    check(output[7].r == 255 && output[7].g == 0 && output[7].b == 0, "microphone uses the selected color");
    LightingEffects::Render(MODE_APPLICATION, a, b, settings, palette, false, 0, output);
    check(output[3].g == 255 && output[3].r == 0, "applications use the selected color");
    a.data.isMuted = true;
    LightingEffects::Render(MODE_OUTPUT, a, b, settings, palette, false, 900, output);
    check(output[0].b == 180 && output[0].r == 0, "mute pulse uses selected blue instead of hardcoded red");
    LightingEffects::Render(MODE_SPLASH, a, b, settings, palette, false, 0, output);
    check(output[0].r == 180 && output[0].b == 180 && output[0].g == 0, "disconnected indicator uses selected magenta");
    return failures ? 1 : 0;
}
