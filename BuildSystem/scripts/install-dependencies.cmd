@echo off

set BIN_DIR=.
set ROOT_DIR=%CD%\..

%BIN_DIR%\arduino-cli.exe core install arduino:avr

pause
