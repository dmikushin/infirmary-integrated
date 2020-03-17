cmake_minimum_required(VERSION 3.7.2)

project(infirmary)

set(PROJECT_VERSION_MAJOR 0)
set(PROJECT_VERSION_MINOR 1)
set(PROJECT_VERSION_PATCH 0)

# Setup build locations.
if(NOT CMAKE_RUNTIME_OUTPUT_DIR)
  set(CMAKE_RUNTIME_OUTPUT_DIR ${CMAKE_CURRENT_BINARY_DIR})
endif()
if(NOT CMAKE_LIBRARY_OUTPUT_DIR)
  set(CMAKE_LIBRARY_OUTPUT_DIR ${CMAKE_CURRENT_BINARY_DIR})
endif()

# Make sure the nested directory structure exists
file(MAKE_DIRECTORY ${CMAKE_RUNTIME_OUTPUT_DIR})
file(MAKE_DIRECTORY ${CMAKE_LIBRARY_OUTPUT_DIR})

# Search path for CMake include files.
set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} ${CMAKE_CURRENT_SOURCE_DIR}/cmake)

# Find C#
find_package(CSharp REQUIRED)
include(${CSHARP_USE_FILE})

# Find MSBuild
find_package(MSBuild REQUIRED)
include(${MSBuild_USE_FILE})

if ("x${CMAKE_BUILD_TYPE}" STREQUAL "x")
set(CMAKE_BUILD_TYPE Release)
endif()
string(TOUPPER ${CMAKE_BUILD_TYPE} CMAKE_BUILD_TYPE_UPPER)

# Set some default build flags.
set(CSBUILDFLAGS "/win32icon:${CMAKE_CURRENT_SOURCE_DIR}/src/core/Resources/Icon_Infirmary.ico")
set(MSBUILDFLAGS "")

# Set up the compiler flag for Debug/Release mode.
if (CMAKE_BUILD_TYPE STREQUAL "Debug")
  list(APPEND CSBUILDFLAGS "/debug:full" "/debug+" "/optimize-")
  list(APPEND MSBUILDFLAGS "/p:Configuration=Debug")
else()
  list(APPEND CSBUILDFLAGS "/debug-" "/optimize+")
  list(APPEND MSBUILDFLAGS "/p:Configuration=Release")
endif()

option(TARGET_DEPENDENCIES "Target dependencies" System.Data System.Drawing System.Windows.Forms)
option(TARGET_FRAMEWORK_VERSION ".NET Target Framework Version" "4.0")

add_subdirectory(src)

# Resolve all target dependencies
csharp_resolve_dependencies()

