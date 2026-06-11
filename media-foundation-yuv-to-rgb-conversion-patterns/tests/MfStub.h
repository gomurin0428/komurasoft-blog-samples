// Linux (g++) で src/YuvPixelToBgra.h をコンパイル・テストするための
// 最小限の Windows / Media Foundation 型定義。
//
// 値は Windows SDK の定義に合わせています。
//   - E_POINTER:             0x80004003 (winerror.h)
//   - MF_E_INVALIDMEDIATYPE: 0xC00D36B4 (mferror.h)
//   - MFVideoTransferMatrix: mfobjects.h の enum 値
//
// Windows でビルドする場合、このファイルは使いません。

#pragma once

#include <cstdint>

typedef unsigned char BYTE;
typedef int32_t HRESULT;

#ifndef S_OK
#define S_OK ((HRESULT)0L)
#endif

#ifndef E_POINTER
#define E_POINTER ((HRESULT)0x80004003L)
#endif

#ifndef MF_E_INVALIDMEDIATYPE
#define MF_E_INVALIDMEDIATYPE ((HRESULT)0xC00D36B4L)
#endif

#ifndef SUCCEEDED
#define SUCCEEDED(hr) (((HRESULT)(hr)) >= 0)
#endif

#ifndef FAILED
#define FAILED(hr) (((HRESULT)(hr)) < 0)
#endif

enum MFVideoTransferMatrix
{
    MFVideoTransferMatrix_Unknown = 0,
    MFVideoTransferMatrix_BT709 = 1,
    MFVideoTransferMatrix_BT601 = 2,
    MFVideoTransferMatrix_SMPTE240M = 3,
};
