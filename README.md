![release](https://github.com/t3knomanzer/maxmix-software/workflows/release/badge.svg)
![pre-release](https://github.com/t3knomanzer/maxmix-software/workflows/pre-release/badge.svg)
![GitHub issues](https://img.shields.io/github/issues/t3knomanzer/maxmix-software)
![GitHub pull requests](https://img.shields.io/github/issues-pr/t3knomanzer/maxmix-software)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/t3knomanzer/maxmix-software/latest)


[![Gitter](https://img.shields.io/gitter/room/t3knomanzer/maxmix-software)](https://gitter.im/maxmixproject/developers)
[![Paypal donate](https://img.shields.io/badge/paypal-donate-blue?logo=paypal)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=SQS6XJZBCBZA8&currency_code=USD&source=url)
[![Github sponsor](https://img.shields.io/badge/github-sponsor-blue?logo=github)](https://github.com/sponsors/t3knomanzer)

# Maxmix
**Maxmix** is an open-source volume mixer that allows you to control the volume of any application running on your Windows PC from an external custom device.  

This repository contains the code for all of the software needed for the system.

## Desktop
The desktop directory contains the application, driver installer and firmware installer which are all written in C# and WPF.

Development is done using [Visual Studio 2019 Communitty Edition](https://visualstudio.microsoft.com/downloads/).

The application installer is made with [Advanced Installer](https://www.advancedinstaller.com/).

## Embedded
The embedded directory contains the firmware for the device which uses an Arduino Nano.
You can use your IDE of choice as long as it compiles for that particular chip.

## Contributing
Contributions are *very* welcome!

Feel free to submit bug reports and feature requests.

If you see an issue that you'd like to see fixed, the best way to make it happen is to help out by submitting a pull request implementing it.

Refer to the [CONTRIBUTING.md](https://github.com/rubenhenares/maxmix-software/blob/master/.github/CONTRIBUTING.md) file for more details about the workflow,
and general hints on how to prepare your pull request. You can also ask for clarifications or guidance in GitHub issues directly.

## Community
You can join these groups and chats to discuss your-project related questions:

- Twitter: https://twitter.com/maxmixproject/
- Reddit: https://www.reddit.com/r/maxmixproject/

## License
Your project is Open Source and available under the Apache 2 License.
