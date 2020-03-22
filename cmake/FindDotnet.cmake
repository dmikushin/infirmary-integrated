set(DOTNET dotnet)
set(PROJECT_MANAGER ProjectManager)
set(PROJECT_MANAGER_BINARY ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE}/bin/${PROJECT_MANAGER}.dll)

add_custom_command(
    OUTPUT ${PROJECT_MANAGER_BINARY}
    DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_MANAGER}/${PROJECT_MANAGER}.sln
	COMMAND ${DOTNET} build ${PROJECT_MANAGER}/${PROJECT_MANAGER}.sln -o ${CMAKE_CURRENT_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}
	WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
    COMMENT "Building solution ${PROJECT_MANAGER}")

add_custom_command(
    DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_MANAGER}/${PROJECT_MANAGER}/${PROJECT_MANAGER}.csproj
    OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_MANAGER}/${PROJECT_MANAGER}.sln
    COMMAND ${DOTNET} new sln -o ${PROJECT_MANAGER}
    COMMAND ${DOTNET} sln ${PROJECT_MANAGER}/${PROJECT_MANAGER}.sln add
        ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_MANAGER}/${PROJECT_MANAGER}/${PROJECT_MANAGER}.csproj
    WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
    COMMENT "Creating solution ${PROJECT_MANAGER}")

add_custom_command(
    DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/cmake/${PROJECT_MANAGER}.cs
    OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_MANAGER}/${PROJECT_MANAGER}/${PROJECT_MANAGER}.csproj
    COMMAND	${DOTNET} new console -o ${PROJECT_MANAGER}/${PROJECT_MANAGER}
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/cmake/${PROJECT_MANAGER}.cs
        ${PROJECT_MANAGER}/${PROJECT_MANAGER}/Program.cs
    WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
    COMMENT "Creating project ${PROJECT_MANAGER}")

macro(add_solution SOL_NAME)
    add_custom_target(${SOL_NAME} ALL
        DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${SOL_NAME}/${SOL_NAME}.sln
        COMMAND dotnet build ${SOL_NAME}/${SOL_NAME}.sln -o ${CMAKE_CURRENT_BINARY_DIR}/bin/${CMAKE_BUILD_TYPE}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
        COMMENT "Building solution ${SOL_NAME}")

    add_custom_command(
        OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${SOL_NAME}/${SOL_NAME}.sln
        COMMAND dotnet new sln -o ${SOL_NAME}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
        COMMENT "Creating solution ${SOL_NAME}")
endmacro()

macro(add_dotnet_project SOL_NAME PROJ_NAME PROJ_PATH TARGET_FRAMEWORK SOURCE)
    add_custom_target(${PROJ_NAME}
        DEPENDS ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj)
    add_custom_command(
        OUTPUT ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj
        DEPENDS ${PROJECT_MANAGER_BINARY} ${SOURCE} ${ARGN}
        COMMAND ${DOTNET} run -- create ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}
            framework ${TARGET_FRAMEWORK} ${SOURCE} ${ARGN}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_MANAGER}/${PROJECT_MANAGER}
        COMMENT "Creating project ${PROJ_NAME}")
	add_custom_command(
        OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${SOL_NAME}/${SOL_NAME}.sln
        APPEND COMMAND dotnet sln ${CMAKE_CURRENT_BINARY_DIR}/${SOL_NAME}/${SOL_NAME}.sln
            add ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj)
    add_dependencies(${SOL_NAME} ${PROJ_NAME})
endmacro()

macro(dotnet_project_package PROJ_NAME PROJ_PATH PACK_NAME)
    add_custom_command(
        OUTPUT ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj
        APPEND COMMAND dotnet add ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj package ${PACK_NAME}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME})
endmacro()

macro(dotnet_project_dependency PROJ_NAME PROJ_PATH DEP_NAME)
    add_custom_command(
        OUTPUT ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj
        APPEND COMMAND ${DOTNET} run -- modify ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}
            dependency ${DEP_NAME} ${ARGN}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME})
endmacro()

macro(dotnet_project_reference PROJ_NAME PROJ_PATH REF_NAME)
    add_custom_command(
        OUTPUT ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj
        DEPENDS ${REF_NAME}
        APPEND COMMAND dotnet add ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj reference ${REF_NAME}
        WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME})
    foreach(ARG ${ARGN})
        add_custom_command(
            OUTPUT ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj
            DEPENDS ${ARG}
            APPEND COMMAND dotnet add ${PROJ_PATH}/${PROJ_NAME}/${PROJ_NAME}.csproj reference ${ARG}
            WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME})        
    endforeach(ARG ${ARGN})
endmacro()
