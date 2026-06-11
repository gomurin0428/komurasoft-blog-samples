// src/YuvPixelToBgra.h (記事 5.6 の 1 pixel 変換) の assert ベースのテスト。
//
// OS 非依存のピクセル演算だけを対象にしています。
// ビルド・実行 (Linux / g++):
//   g++ -std=c++17 -Wall -Wextra -o test_yuv_pixel tests/TestYuvPixelToBgra.cpp
//   ./test_yuv_pixel

#include <cassert>
#include <cstdio>
#include <initializer_list>

#include "MfStub.h"
#include "../src/YuvPixelToBgra.h"

namespace
{

struct Bgra
{
    BYTE b;
    BYTE g;
    BYTE r;
    BYTE a;
};

Bgra Convert(BYTE y, BYTE u, BYTE v, MFVideoTransferMatrix matrix)
{
    BYTE pixel[4] = { 1, 2, 3, 4 };
    HRESULT hr = ConvertLimitedYuvPixelToBgra(y, u, v, matrix, pixel);
    assert(SUCCEEDED(hr));
    return Bgra{ pixel[0], pixel[1], pixel[2], pixel[3] };
}

int Abs(int value)
{
    return value < 0 ? -value : value;
}

// 誤差 ±1 まで許容して比較する
void AssertNear(BYTE actual, int expected, const char* what)
{
    if (Abs(static_cast<int>(actual) - expected) > 1)
    {
        std::printf("FAILED: %s actual=%d expected=%d\n",
                    what, static_cast<int>(actual), expected);
        assert(false);
    }
}

void TestClampToByte()
{
    assert(ClampToByte(-1.0) == 0);
    assert(ClampToByte(0.0) == 0);
    assert(ClampToByte(0.4) == 0);
    assert(ClampToByte(0.6) == 1);
    assert(ClampToByte(127.5) == 128);  // +0.5 で四捨五入
    assert(ClampToByte(254.4) == 254);
    assert(ClampToByte(255.0) == 255);
    assert(ClampToByte(300.0) == 255);
}

void TestLimitedRangeBlackAndWhite()
{
    // limited range の黒: Y=16, U=V=128 -> (0, 0, 0)
    for (MFVideoTransferMatrix m :
         { MFVideoTransferMatrix_BT601, MFVideoTransferMatrix_BT709 })
    {
        const Bgra black = Convert(16, 128, 128, m);
        assert(black.b == 0 && black.g == 0 && black.r == 0);
        assert(black.a == 255);

        // limited range の白: Y=235, U=V=128 -> (255, 255, 255)
        const Bgra white = Convert(235, 128, 128, m);
        assert(white.b == 255 && white.g == 255 && white.r == 255);
        assert(white.a == 255);
    }
}

void TestBelow16IsClippedToZero()
{
    // Y=0 (super black) は 0 に clip される
    const Bgra p = Convert(0, 128, 128, MFVideoTransferMatrix_BT601);
    assert(p.b == 0 && p.g == 0 && p.r == 0 && p.a == 255);
}

void TestGrayHasNoColorCast()
{
    // U=V=128 なら R=G=B (色がつかない)
    for (int y = 16; y <= 235; y += 73)
    {
        const Bgra p = Convert(
            static_cast<BYTE>(y), 128, 128, MFVideoTransferMatrix_BT601);
        assert(p.b == p.g && p.g == p.r);
    }
}

void TestBt601PrimaryColors()
{
    // BT.601 limited range の代表値 (100% color bars)
    // 赤: Y=81, Cb=90, Cr=240
    const Bgra red = Convert(81, 90, 240, MFVideoTransferMatrix_BT601);
    AssertNear(red.r, 254, "BT601 red R");
    AssertNear(red.g, 0, "BT601 red G");
    AssertNear(red.b, 0, "BT601 red B");
    assert(red.a == 255);

    // 緑: Y=145, Cb=54, Cr=34
    const Bgra green = Convert(145, 54, 34, MFVideoTransferMatrix_BT601);
    AssertNear(green.r, 0, "BT601 green R");
    AssertNear(green.g, 255, "BT601 green G");
    AssertNear(green.b, 1, "BT601 green B");

    // 青: Y=41, Cb=240, Cr=110
    const Bgra blue = Convert(41, 240, 110, MFVideoTransferMatrix_BT601);
    AssertNear(blue.r, 0, "BT601 blue R");
    AssertNear(blue.g, 0, "BT601 blue G");
    AssertNear(blue.b, 255, "BT601 blue B");
}

void TestBt709PrimaryColors()
{
    // BT.709 limited range の代表値 (100% color bars)
    // 赤: Y=63, Cb=102, Cr=240
    const Bgra red = Convert(63, 102, 240, MFVideoTransferMatrix_BT709);
    AssertNear(red.r, 255, "BT709 red R");
    AssertNear(red.g, 0, "BT709 red G");
    AssertNear(red.b, 0, "BT709 red B");

    // 緑: Y=173, Cb=42, Cr=26
    const Bgra green = Convert(173, 42, 26, MFVideoTransferMatrix_BT709);
    AssertNear(green.r, 0, "BT709 green R");
    AssertNear(green.g, 255, "BT709 green G");
    AssertNear(green.b, 1, "BT709 green B");

    // 青: Y=32, Cb=240, Cr=118
    const Bgra blue = Convert(32, 240, 118, MFVideoTransferMatrix_BT709);
    AssertNear(blue.r, 1, "BT709 blue R");
    AssertNear(blue.g, 0, "BT709 blue G");
    AssertNear(blue.b, 255, "BT709 blue B");
}

void TestMatrixMatters()
{
    // 同じ Y/U/V でも matrix で結果が変わる
    // (601 と 709 を取り違えると色がズレる、の最小確認)
    const Bgra p601 = Convert(81, 90, 240, MFVideoTransferMatrix_BT601);
    const Bgra p709 = Convert(81, 90, 240, MFVideoTransferMatrix_BT709);
    assert(p601.g != p709.g || p601.b != p709.b || p601.r != p709.r);
}

void TestUnsupportedMatrixIsRejected()
{
    BYTE pixel[4] = {};
    HRESULT hr = ConvertLimitedYuvPixelToBgra(
        128, 128, 128, MFVideoTransferMatrix_Unknown, pixel);
    assert(hr == MF_E_INVALIDMEDIATYPE);

    hr = ConvertLimitedYuvPixelToBgra(
        128, 128, 128, MFVideoTransferMatrix_SMPTE240M, pixel);
    assert(hr == MF_E_INVALIDMEDIATYPE);
}

void TestNullDestinationIsRejected()
{
    HRESULT hr = ConvertLimitedYuvPixelToBgra(
        128, 128, 128, MFVideoTransferMatrix_BT601, nullptr);
    assert(hr == E_POINTER);
}

} // namespace

int main()
{
    TestClampToByte();
    TestLimitedRangeBlackAndWhite();
    TestBelow16IsClippedToZero();
    TestGrayHasNoColorCast();
    TestBt601PrimaryColors();
    TestBt709PrimaryColors();
    TestMatrixMatters();
    TestUnsupportedMatrixIsRejected();
    TestNullDestinationIsRejected();

    std::printf("All tests passed.\n");
    return 0;
}
