cmake_minimum_required(VERSION 3.10)
project(serial_service C)

set(CMAKE_C_STANDARD 99)

add_executable(serial_service main.c serial.c)
