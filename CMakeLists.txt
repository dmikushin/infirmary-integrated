cmake_minimum_required(VERSION 3.7.2)

project(tanjera)

set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} ${CMAKE_CURRENT_SOURCE_DIR}/cmake)

find_package(Dotnet)

set(PROJECT_VERSION_MAJOR 0)
set(PROJECT_VERSION_MINOR 1)
set(PROJECT_VERSION_PATCH 0)

if ("x${CMAKE_BUILD_TYPE}" STREQUAL "x")
set(CMAKE_BUILD_TYPE Debug)
endif()

add_solution(${PROJECT_NAME})

file(GLOB SUBPROJECT_NAMES RELATIVE "${CMAKE_CURRENT_SOURCE_DIR}/src/" "${CMAKE_CURRENT_SOURCE_DIR}/src/*")
foreach(SUBPROJECT_NAME ${SUBPROJECT_NAMES})
	file(GLOB_RECURSE SOURCE_PATHS ${CMAKE_CURRENT_SOURCE_DIR}/src/${SUBPROJECT_NAME}
		"${CMAKE_CURRENT_SOURCE_DIR}/src/${SUBPROJECT_NAME}/*.cs")
	add_dotnet_project(${PROJECT_NAME} ${SUBPROJECT_NAME} ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src
		"netcoreapp3.1" ${SOURCE_PATHS})
endforeach()

dotnet_project_dependency(Core ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src PdfSharp)

set(CORE ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src/Core/Core.csproj)
dotnet_project_reference(IIDT ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src ${CORE})
dotnet_project_reference(IISE ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src ${CORE})
dotnet_project_reference(Tests ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src ${CORE})
dotnet_project_reference(Windows ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}/src ${CORE})
