cmake_minimum_required(VERSION 3.20)
project(media_foundation_yuv_to_rgb_conversion_patterns CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ---------------------------------------------------------------------------
# Media Foundation 部分 (Windows / MSVC 専用)
#   記事のパターンA / パターンB のコードを静的ライブラリとしてビルドします。
#   main は含みません (記事のコードが関数単位のため)。
# ---------------------------------------------------------------------------
if(WIN32)
    add_library(mf_yuv_to_rgb STATIC
        src/PatternA_SourceReaderAutoRgb32.cpp
        src/PatternB_ManualYuvToBgra.cpp
    )
    target_link_libraries(mf_yuv_to_rgb PRIVATE
        mfplat
        mfreadwrite
        mfuuid
        ole32
    )
endif()

# ---------------------------------------------------------------------------
# OS 非依存のピクセル変換テスト (Windows / Linux どちらでもビルド可能)
#   記事 5.6 の 1 pixel 変換 (src/YuvPixelToBgra.h) を assert で検証します。
# ---------------------------------------------------------------------------
enable_testing()

add_executable(test_yuv_pixel
    tests/TestYuvPixelToBgra.cpp
)

add_test(NAME yuv_pixel_conversion COMMAND test_yuv_pixel)
