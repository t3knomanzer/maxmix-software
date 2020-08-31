@echo off
echo ==== MaxMix Project Auto Build ====
pause

set BIN_DIR=.
set ROOT_DIR=%CD%\..
set BUILD_DIR=%ROOT_DIR%\build
set RELEASE_DIR=%ROOT_DIR%\release
set MAIN_INO=%ROOT_DIR%\..\Embedded\MaxMix\MaxMix.ino

rmdir /s /q %RELEASE_DIR%
mkdir %RELEASE_DIR%

rmdir /s /q %BUILD_DIR%
%BIN_DIR%\arduino-cli.exe compile -b arduino:avr:nano:cpu=atmega328 -v --build-path %BUILD_DIR% %MAIN_INO%
move %BUILD_DIR%\MaxMix.ino.hex %RELEASE_DIR%\MaxMix.ino.hex 

rmdir /s /q  %BUILD_DIR%
%BIN_DIR%\arduino-cli.exe compile -b arduino:avr:nano:cpu=atmega328 -v --build-path %BUILD_DIR% --build-properties build.extra_flags=-DHALF_STEP %MAIN_INO%
move %BUILD_DIR%\MaxMix.ino.hex %RELEASE_DIR%\MaxMix.halfstep.ino.hex 

rmdir /s /q %BUILD_DIR%

pause
