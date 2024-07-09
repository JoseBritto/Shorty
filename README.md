This is still a WIP.

A live demo is hosted at [https://shorty.britto.tech/](https://shorty.britto.tech) *(might be outdated sometimes. CD not yet setup)*


### Build Instructions

Make sure `dotnet 8` is installed on your system. Get it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or your distribution repositories.

Run the following commands in src/Shorty to build Shorty from source:
```
❯ dotnet restore
❯ dotnet publish -o output-dir
```
Replace output-dir with the target output directory name.

I have only tested on arch linux. There's no platform specific features utilised so it should work fine on all donet supported platforms.
