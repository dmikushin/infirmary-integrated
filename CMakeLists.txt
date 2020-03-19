cmake_minimum_required(VERSION 3.7.2)

project(infirmary)

set(PROJECT_VERSION_MAJOR 0)
set(PROJECT_VERSION_MINOR 1)
set(PROJECT_VERSION_PATCH 0)

macro(get_library_name TARGET_DIR LIBRARY_NAME)
get_filename_component(LIBRARY_NAME_ORIG ${TARGET_DIR} NAME)
string(TOLOWER ${LIBRARY_NAME_ORIG} LIBRARY_NAME_LOWER)
string(SUBSTRING ${LIBRARY_NAME_LOWER} 0 1 FIRST_LETTER)
string(TOUPPER ${FIRST_LETTER} FIRST_LETTER)
string(REGEX REPLACE "^.(.*)" "${FIRST_LETTER}\\1" LIBRARY_NAME_CAP "${LIBRARY_NAME_LOWER}")
set(${LIBRARY_NAME} "${LIBRARY_NAME_CAP}.dll")
endmacro(get_library_name)

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
set(CSHARP_FRAMEWORK_VERSION "4.0")
set(CSHARP_SDK_COMPILER "/sdk:5")
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
set(CSBUILDFLAGS "/win32icon:${CMAKE_CURRENT_SOURCE_DIR}/src/Core/Resources/Icon_Infirmary.ico")
set(MSBUILDFLAGS "")

# Set up the compiler flag for Debug/Release mode.
if (CMAKE_BUILD_TYPE STREQUAL "Debug")
  list(APPEND CSBUILDFLAGS "/debug:full" "/debug+" "/optimize-")
  list(APPEND MSBUILDFLAGS "/p:Configuration=Debug")
else()
  list(APPEND CSBUILDFLAGS "/debug-" "/optimize+")
  list(APPEND MSBUILDFLAGS "/p:Configuration=Release")
endif()

set(TARGET_DEPENDENCIES System.Data System.Drawing System.Net.Http System.Windows.Forms)

add_subdirectory(src)

# Resolve all target dependencies
csharp_resolve_dependencies()

