# InogeniLoupdeckControlPloginPlugin
A plugin to control the Imogene Toggle via R232

## Build

This repository contains two build targets:

- `SerialBridgeDotNet/src`: a small C based serial bridge (`serial_service`)
- `src`: the Loupedeck plugin built with .NET

The plugin build expects the serial bridge binary at `SerialBridgeDotNet/src/build/serial_service`
and copies it into the plugin output folder during the post-build step.

### Prerequisites

- .NET 8 SDK
- CMake and a C compiler
- Logi/Loupedeck Plugin Service installed, so `PluginApi.dll` and `SkiaSharp.dll` are available

On macOS the project file expects the Logi Plugin Service libraries here:

```console
/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/
```

If your installation is in a different location, pass `PluginApiDir` when building the plugin.

### Build the serial bridge

From the repository root:

```console
cd SerialBridgeDotNet/src
mkdir -p build
cd build
cmake ..
make
```

You can test the bridge manually with a serial device path:

```console
./serial_service -d /dev/tty.usbserial-123 -b 9600
```

### Build the Loupedeck plugin

From the repository root, change into the .NET solution directory:

```console
cd src
dotnet build InogeniLoupdeckControlPlugin.sln
```

If `PluginApi.dll` is not in the default macOS location, provide the Logi Plugin Service
`MonoBundle` path explicitly:

```console
cd src
dotnet build InogeniLoupdeckControlPlugin.sln -p:PluginApiDir=/path/to/LogiPluginService/MonoBundle/
```

The Debug output is written to:

```console
bin/Debug/mac
```

The project also writes a plugin link file into the Logi Plugin Service plugin directory as
part of the post-build step.
