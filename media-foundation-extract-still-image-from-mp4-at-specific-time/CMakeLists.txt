cmake_minimum_required(VERSION 3.20)

project(ExtractFrameFromMp4 LANGUAGES CXX)

if(NOT WIN32)
    message(FATAL_ERROR "This sample uses Media Foundation / WIC and builds only on Windows (MSVC).")
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(ExtractFrameFromMp4 src/ExtractFrameFromMp4.cpp)

# wmain を使う Unicode コンソールアプリ
target_compile_definitions(ExtractFrameFromMp4 PRIVATE UNICODE _UNICODE)

if(MSVC)
    target_compile_options(ExtractFrameFromMp4 PRIVATE /W4 /utf-8)
endif()

# ソース側の #pragma comment(lib, ...) でもリンクされるが、CMake 側でも明示しておく
target_link_libraries(ExtractFrameFromMp4 PRIVATE
    mfplat
    mfreadwrite
    mfuuid
    ole32
    propsys
    windowscodecs
)
