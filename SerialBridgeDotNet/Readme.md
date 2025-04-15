# Serial Bridge for Dot net (especially on OSX)

Serial libraries are aparently not working reliable in .NET on MacOSX. Therefore I decided to put that into a separeted C base process.


## Build

```console
mkdir build
cd build
cmake ..
make
./serial_service -d /dev/tty.usbserial-123 -b 9600
``` 

## Build

```console
./serial_service -d /dev/tty.usbserial-123 -b 9600
``` 
