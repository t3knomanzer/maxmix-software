@echo off
set ROOT_DIR=%CD%\..
set BIN_DIR=%ROOT_DIR%\bin

%BIN_DIR%\arduino-cli.exe core install arduino:avr

pause