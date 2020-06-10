[url-repo-issues]:https://github.com/t3knomanzer/maxmix-software/issues
[url-repo-pulls]:https://github.com/t3knomanzer/maxmix-software/pulls
[url-shield-issues]:https://img.shields.io/github/issues/t3knomanzer/maxmix-software.svg
[url-shield-pulls]:https://img.shields.io/github/issues-pr/t3knomanzer/maxmix-software.svg

# Maxmix
[![github: t3knomanzer/maxmix-software/issues][url-shield-issues]][url-repo-issues]
[![github: t3knomanzer/maxmix-software/pulls][url-shield-pulls]][url-repo-pulls]

**Maxmix** is an open-source volume mixer that allows you to control the volume of any application running on your Windows PC from an external custom device.  

This repository contains the code for all of the software needed for the system.

## Desktop
The desktop directory contains the application, driver installer and firmware installer which are all written in C# and WPF.

Development is done using [Visual Studio 2019 Communitty Edition](https://visualstudio.microsoft.com/downloads/).

The application installer is made with [Advanced Installer] (https://www.advancedinstaller.com/).

## Embedded
The embedded directory contains the firmware for the device which uses an Arduino Nano.
You can use your IDE of choice as long as it compiles for that particular chip.

## Community
You can join these groups and chats to discuss and ask your-project related questions:

- Twitter: https://twitter.com/maxmixproject/
- Reddit: https://www.reddit.com/r/maxmix/

## Contributing
Contributions are *very* welcome!

Feel free to submit bug reports and feature requests.

If you see an issue that you'd like to see fixed, the best way to make it happen is to help out by submitting a pull request implementing it.

Refer to the [CONTRIBUTING.md](https://github.com/rubenhenares/maxmix-software/blob/master/.github/CONTRIBUTING.md) file for more details about the workflow,
and general hints on how to prepare your pull request. You can also ask for clarifications or guidance in GitHub issues directly.

## License

Your project is Open Source and available under the Apache 2 License.
