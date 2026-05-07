cmake_minimum_required(VERSION 3.20)

project(InogeniLoupdeckControlPlugin)

find_program(DOTNET_EXECUTABLE dotnet REQUIRED)

set(PLUGIN_SOLUTION "${CMAKE_SOURCE_DIR}/src/InogeniLoupdeckControlPlugin.sln")
set(PLUGIN_API_DIR "" CACHE PATH "Path to the Logi/Loupedeck Plugin Service MonoBundle directory")
set(DOTNET_BUILD_ARGS "" CACHE STRING "Additional arguments passed to dotnet build. Use a CMake list for multiple arguments.")

set(PLUGIN_DOTNET_BUILD_ARGS ${DOTNET_BUILD_ARGS})
if(PLUGIN_API_DIR)
    list(APPEND PLUGIN_DOTNET_BUILD_ARGS "-p:PluginApiDir=${PLUGIN_API_DIR}")
endif()

add_subdirectory(SerialBridgeDotNet/src)

add_custom_target(loupedeck_plugin ALL
    COMMAND "${DOTNET_EXECUTABLE}" build "${PLUGIN_SOLUTION}"
        -p:SerialBridgePath=$<TARGET_FILE:serial_service>
        ${PLUGIN_DOTNET_BUILD_ARGS}
    WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}/src"
    DEPENDS serial_service
    COMMENT "Building Loupedeck .NET plugin"
    VERBATIM
)
