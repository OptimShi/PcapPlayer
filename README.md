# PCAP Player

**PCAP Player is based on [ACEmulator](https://github.com/ACEmulator/ACE), a custom, completely from-scratch open source server implementation for Asheron's Call built on C#. It also borrows elements from [aclogview](https://github.com/ACEmulator/aclogview) to read and decode the PCAPs**
 * Latest client supported.
 * Currently intended for developers, hobbyists, and content creators.
 * No technical support is provided. Use at our own risk.

***
## Disclaimer
**This project is for educational and non-commercial purposes only, use of the game client is for interoperability with the emulated server.**
- Asheron's Call was a registered trademark of Turbine, Inc. and WB Games Inc which has since expired.
- Neither this project nor ACEmulator is associated or affiliated in any way with Turbine, Inc. or WB Games Inc.
***

## Console Commands
* `pcap-load <full-path-to-pcap-file>` - Select the PCAP file to load for playback.
* `pcap-login <login-#>` - Specify a login instance for pcap playback.
* `list` - List the index and pcap line numbers of detected player teleport and login events, along with extended information about the event (location, if available, timestamp, etc). Use with the `pcap-login <login-#>` and `@teleport <index>` command. 

## In-Game Commands
* `@teleport` - Advance the PCAP to the next teleport instance, if one exists.
* `@teleport <index-#>` - Advance to the specified player teleport event (zero-based index). Use `list` in the server console to display the list of valid indexes, if any.
* `@pause` - Pauses the playback of the PCAP. Use `@pause` again to continue playback, or use a `teleport` command

## Recommended Tools
* ACLogView [on Github](https://github.com/ACEmulator/aclogview) to view Pcaps.

## Getting Started
The following sections (Code, and Starting the Server) contain all the required steps to setup your own ACE server and connect to it. Most setup errors can be traced back to not following one or more of these steps. Be sure to follow them carefully. Note that you do not need to create a database or edit the `Config.js` for this to function properly. Do not use in the same folder as an ACE install as your ACE configuration file will be overwritten.

### Code 
1. Install Visual Studio 2017
   * [Visual Studio minimum required version - VS Community 2019](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&rel=16)
   * [.NET 5.0 x64 SDK (Visual Studio 2019) Required](https://www.microsoft.com/net/download/visual-studio-sdks)
   * If using Visual Studio Community Edition, make sure the following two workloads are installed: .NET Core cross-platform development and .NET Desktop Development
3. Open PcapPlayer.sln with Visual Studio and build the solution. 
4. Download and install [Microsoft .NET Core Runtime - 5.0](https://www.microsoft.com/net/download) if you don't already have it.

### Starting the Server
1. Start the server by running the batch file located in the net5.0 output directory: `PcapPlayer.exe`
   * ex. PcapPlayer\Source\ACE.Server\bin\x64\Debug\net5.0
2. Load the PCAP into the console - `pcap-load <full-path-to-pcap-file>`. The Pcap Player works better if the PCAP has a login event, but will attempt to join in-progress using a base login event. The console will notify you if there was np login event found. In the event of multiple login events in the PCAP, use `pcap-login <login-#>` to specify the index of the login to use from 1 to TOTAL_LOGIN_EVENTS.
3. Launch ACClient directly with this command: `acclient.exe -a testaccount -v testpassword -h 127.0.0.1:9000`


## Contributions

* Contributions in the form of issues and pull requests are welcomed and encouraged.
* The preferred way to contribute is to fork the repo and submit a pull request on GitHub.
* Code style information can be found on the [Wiki](https://github.com/ACEmulator/ACE/wiki/Code-Style).

