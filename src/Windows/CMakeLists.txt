file(GLOB_RECURSE LIBRARY_SRC "*.cs" "*.resx")
get_library_name(${CMAKE_CURRENT_SOURCE_DIR} LIBRARY_NAME)
csharp_add_library(${LIBRARY_NAME} ${LIBRARY_SRC})
csharp_add_dependencies(${LIBRARY_NAME} ${TARGET_DEPENDENCIES} ${CMAKE_BINARY_DIR}/Core.dll)
