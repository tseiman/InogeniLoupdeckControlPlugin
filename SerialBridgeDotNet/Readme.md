# Serial Bridge for .NET (especially on macOS)

Serial libraries are apparently not always reliable in .NET on macOS. Therefore the serial
port handling is implemented as a separate C based process.

## Build from the repository root

The recommended project build uses the root CMake file. From the repository root:

```console
cmake -S . -B build
cmake --build build
```

This builds `serial_service` and then builds the Loupedeck .NET plugin with the generated
bridge path.

## Build the serial bridge only

From `SerialBridgeDotNet/src`:

```console
mkdir build
cd build
cmake ..
make
./serial_service -d /dev/tty.usbserial-123 -b 9600
```

## Run manually

```console
./serial_service -d /dev/tty.usbserial-123 -b 9600
```
