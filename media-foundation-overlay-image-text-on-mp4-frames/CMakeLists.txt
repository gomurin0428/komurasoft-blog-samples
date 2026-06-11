cmake_minimum_required(VERSION 3.20)

project(OverlayMp4 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(NOT WIN32)
    message(FATAL_ERROR "This sample uses Media Foundation / GDI+ and builds only on Windows (MSVC).")
endif()

add_executable(OverlayMp4 src/OverlayMp4.cpp)

target_compile_definitions(OverlayMp4 PRIVATE UNICODE _UNICODE)

if(MSVC)
    target_compile_options(OverlayMp4 PRIVATE /W4 /utf-8)
endif()

# ソース内の #pragma comment(lib, ...) でもリンクされるが、明示しておく
target_link_libraries(OverlayMp4 PRIVATE
    mfplat
    mfreadwrite
    mfuuid
    mf
    gdiplus
    ole32
)
