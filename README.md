<img src=".github/resources/color-dark-2048.png" width="256">

![release](https://github.com/t3knomanzer/maxmix-software/workflows/release/badge.svg)
![tests](https://github.com/t3knomanzer/maxmix-software/workflows/tests/badge.svg?branch=master)
![GitHub issues](https://img.shields.io/github/issues/t3knomanzer/maxmix-software)
![GitHub pull requests](https://img.shields.io/github/issues-pr/t3knomanzer/maxmix-software)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/t3knomanzer/maxmix-software/latest)


<!-- [![Gitter](https://img.shields.io/gitter/room/t3knomanzer/maxmix-software)](https://gitter.im/maxmixproject/developers) -->
[![Paypal donate](https://img.shields.io/badge/paypal-donate-blue?logo=paypal)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=SQS6XJZBCBZA8&currency_code=USD&source=url)
[![Github sponsor](https://img.shields.io/badge/github-sponsor-blue?logo=github)](https://github.com/sponsors/t3knomanzer)

## Overview
**Maxmix** is an open-source volume mixer that allows you to control the volume applications from an external custom controller.  
It was originally designed to allow to quickly adjust the volume of a game and an external voice chat application like Discord quckly, but it can do so much more...  
You can find out more about the system in the [project website](https://www.maxmixproject.com).

This repository contains the code for all of the software.

## Desktop
The desktop directory contains the **desktop application**, **driver installer** and **firmware installer** which are all written in C# and WPF.  
Development is done using [Visual Studio 2019 Community Edition](https://visualstudio.microsoft.com/downloads/).  
The application installer is made with [Advanced Installer](https://www.advancedinstaller.com/).

## Embedded
The embedded directory contains the **device firmware** which uses an Arduino Nano.  
You can use your IDE of choice as long as it compiles for that particular chip.  

## Contributing
Contributions are *very* welcome!

Refer to the [CONTRIBUTING.md](https://github.com/t3knomanzer/maxmix-software/blob/master/.github/CONTRIBUTING.md) file for more details about the workflow,
and general hints on how to prepare your pull request. You can also ask for clarifications or guidance in GitHub issues directly.

## Community
You can join these groups and chats to discuss your-project related questions:

- Youtube: https://www.youtube.com/channel/UCU5MRTji6emgxk84aEd7Gqg/
- Twitter: https://twitter.com/maxmixproject/
- Reddit: https://www.reddit.com/r/maxmixproject/
