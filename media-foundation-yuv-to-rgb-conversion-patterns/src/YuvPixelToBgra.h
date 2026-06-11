// 1 pixel の YUV (limited range) -> BGRA 変換
// 記事 5.6 のコード。
//
// この部分は OS 非依存のピクセル演算です。Windows では <windows.h> /
// <mfobjects.h> 等の型 (BYTE, HRESULT, MFVideoTransferMatrix) を、
// Linux でのテストでは tests/MfStub.h の互換定義を、それぞれ
// このヘッダーより前に include して使います。

#pragma once

inline BYTE ClampToByte(double value)
{
    if (value <= 0.0) return 0;
    if (value >= 255.0) return 255;
    return static_cast<BYTE>(value + 0.5);
}

HRESULT ConvertLimitedYuvPixelToBgra(
    BYTE y,
    BYTE u,
    BYTE v,
    MFVideoTransferMatrix matrix,
    BYTE* dstPixel)
{
    if (!dstPixel) return E_POINTER;

    const double c = static_cast<double>(y) - 16.0;
    const double d = static_cast<double>(u) - 128.0;
    const double e = static_cast<double>(v) - 128.0;

    double r = 0.0;
    double g = 0.0;
    double b = 0.0;

    switch (matrix)
    {
    case MFVideoTransferMatrix_BT601:
        r = 1.164383 * c + 1.596027 * e;
        g = 1.164383 * c - 0.391762 * d - 0.812968 * e;
        b = 1.164383 * c + 2.017232 * d;
        break;

    case MFVideoTransferMatrix_BT709:
        r = 1.164383 * c + 1.792741 * e;
        g = 1.164383 * c - 0.213249 * d - 0.532909 * e;
        b = 1.164383 * c + 2.112402 * d;
        break;

    default:
        return MF_E_INVALIDMEDIATYPE;
    }

    dstPixel[0] = ClampToByte(b);
    dstPixel[1] = ClampToByte(g);
    dstPixel[2] = ClampToByte(r);
    dstPixel[3] = 255;

    return S_OK;
}
