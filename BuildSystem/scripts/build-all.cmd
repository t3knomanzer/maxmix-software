:: =================================================
:: This script compiles the 2 versions of the firmware
:: for different rotary encoder condfigurations (full/half step)
:: It assumes the arduino-cli is installed and added to the path.
:: If you haven't done so, run install-dependencies.cmd first.
:: =================================================

@echo off
echo ==== MaxMix Project Firmware Builder ====

set ROOT_DIR=%CD%\..
set BUILD_DIR=%ROOT_DIR%\build
set RELEASE_DIR=%ROOT_DIR%\release
set MAIN_INO=%ROOT_DIR%\..\Embedded\MaxMix\MaxMix.ino

rmdir /s /q %RELEASE_DIR%
mkdir %RELEASE_DIR%

rmdir /s /q %BUILD_DIR%
arduino-cli compile -b arduino:avr:nano:cpu=atmega328 -v --build-path %BUILD_DIR% "build.extra_flags=-DVERSION_MAJOR=9 -DVERSION_MINOR=9 -DVERSION_PATCH=9" %MAIN_INO%
move %BUILD_DIR%\MaxMix.ino.hex %RELEASE_DIR%\MaxMix.ino.hex 

rmdir /s /q  %BUILD_DIR%
arduino-cli compile -b arduino:avr:nano:cpu=atmega328 -v --build-path %BUILD_DIR% --build-properties "build.extra_flags=-DHALF_STEP -DVERSION_MAJOR=9 -DVERSION_MINOR=9 -DVERSION_PATCH=9" %MAIN_INO%
move %BUILD_DIR%\MaxMix.ino.hex %RELEASE_DIR%\MaxMix.halfstep.ino.hex 

rmdir /s /q %BUILD_DIR%

pause
