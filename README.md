# MinecraftSync

A simple program used for syncing minecraft worlds between multiple servers.

## Installation and usage

The [.NET 5 runtime](https://dotnet.microsoft.com/download/dotnet/current/runtime)
must be installed to run both the client and the server.

The program can be found in the releases section.

To use the client the Config.txt file must be modified
- The first line indicates the server address
- The second line indicates the user token, this should initially be set to ```none```
- The third line indicates the address of the user's own server, this currently isn't used

## Acknowledgements

This program is built using the following open source libraries:
- Giraffe
- Ply