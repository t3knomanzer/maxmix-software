:: =================================================
:: This script sets up the arduino toolchain needed
:: to compile the firmware.
:: It uses the windows package manager chocolatey (https://chocolatey.org/)
:: and it assumes it is already installed.
:: It needs to be executed from an console with admin rights.
:: =================================================

@echo off

choco install arduino-cli --version=0.12.1 -y
arduino-cli core install arduino:avr

pause
