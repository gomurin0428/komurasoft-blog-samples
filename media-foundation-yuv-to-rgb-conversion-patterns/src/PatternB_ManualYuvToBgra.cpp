// パターンB: NV12 / YUY2 を受け取り、自分で BGRA32 へ変換する
// 記事 5.3 - 5.9 のコード。
// CoInitializeEx と MFStartup が済んでいる前提です。
//
// このサンプルが受け付ける前提 (記事 5.2):
//   8-bit SDR / progressive / NV12 or YUY2 / limited range (16..235)
//
// 1 pixel の変換式 (記事 5.6) は OS 非依存の演算なので、
// src/YuvPixelToBgra.h に分けて tests/ から g++ でも検証しています。

#include <windows.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mferror.h>
#include <wrl/client.h>

#pragma comment(lib, "mfplat.lib")
#pragma comment(lib, "mfreadwrite.lib")
#pragma comment(lib, "mfuuid.lib")
#pragma comment(lib, "ole32.lib")

using Microsoft::WRL::ComPtr;

// ---------------------------------------------------------------------------
// 5.3. まずは出力 media type を明示する
//   subtype には MFVideoFormat_NV12 か MFVideoFormat_YUY2 を渡します。
//   要求した subtype がそのまま通るとは限らないので、
//   実際に何が出てくるかは GetCurrentMediaType で確認します。
// ---------------------------------------------------------------------------

HRESULT ConfigureSourceReaderForSubtype(
    IMFSourceReader* reader,
    REFGUID subtype)
{
    if (!reader) return E_POINTER;

    HRESULT hr = reader->SetStreamSelection(MF_SOURCE_READER_ALL_STREAMS, FALSE);
    if (FAILED(hr)) return hr;

    hr = reader->SetStreamSelection(MF_SOURCE_READER_FIRST_VIDEO_STREAM, TRUE);
    if (FAILED(hr)) return hr;

    ComPtr<IMFMediaType> outType;
    hr = MFCreateMediaType(&outType);
    if (FAILED(hr)) return hr;

    hr = outType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
    if (FAILED(hr)) return hr;

    hr = outType->SetGUID(MF_MT_SUBTYPE, subtype);
    if (FAILED(hr)) return hr;

    hr = reader->SetCurrentMediaType(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        nullptr,
        outType.Get());
    if (FAILED(hr)) return hr;

    return S_OK;
}

// ---------------------------------------------------------------------------
// 5.4. 変換前に、対応している色情報だけを受け入れる
//   NV12 / YUY2 だけを受け入れ、matrix は BT.601 か BT.709、
//   range は MFNominalRange_16_235 だけを通します (strict)。
// ---------------------------------------------------------------------------

#include <vector>

struct DecodedFrameInfo
{
    GUID subtype = GUID_NULL;
    UINT32 width = 0;
    UINT32 height = 0;
    LONG defaultStride = 0;
    MFVideoTransferMatrix matrix = MFVideoTransferMatrix_Unknown;
    MFNominalRange nominalRange = MFNominalRange_Unknown;
};

HRESULT GetDefaultStride(
    IMFMediaType* pType,
    LONG* plStride)
{
    if (!pType || !plStride) return E_POINTER;

    LONG stride = 0;
    HRESULT hr = pType->GetUINT32(
        MF_MT_DEFAULT_STRIDE,
        reinterpret_cast<UINT32*>(&stride));

    if (FAILED(hr))
    {
        GUID subtype = GUID_NULL;
        UINT32 width = 0;
        UINT32 height = 0;

        hr = pType->GetGUID(MF_MT_SUBTYPE, &subtype);
        if (FAILED(hr)) return hr;

        hr = MFGetAttributeSize(pType, MF_MT_FRAME_SIZE, &width, &height);
        if (FAILED(hr)) return hr;

        hr = MFGetStrideForBitmapInfoHeader(subtype.Data1, width, &stride);
        if (FAILED(hr)) return hr;

        hr = pType->SetUINT32(MF_MT_DEFAULT_STRIDE, static_cast<UINT32>(stride));
        if (FAILED(hr)) return hr;
    }

    *plStride = stride;
    return S_OK;
}

HRESULT GetStrictDecodedFrameInfo(
    IMFMediaType* pType,
    DecodedFrameInfo* pInfo)
{
    if (!pType || !pInfo) return E_POINTER;

    HRESULT hr = pType->GetGUID(MF_MT_SUBTYPE, &pInfo->subtype);
    if (FAILED(hr)) return hr;

    if (pInfo->subtype != MFVideoFormat_NV12 &&
        pInfo->subtype != MFVideoFormat_YUY2)
    {
        return MF_E_INVALIDMEDIATYPE;
    }

    hr = MFGetAttributeSize(pType, MF_MT_FRAME_SIZE, &pInfo->width, &pInfo->height);
    if (FAILED(hr)) return hr;

    hr = GetDefaultStride(pType, &pInfo->defaultStride);
    if (FAILED(hr)) return hr;

    UINT32 value = 0;

    hr = pType->GetUINT32(MF_MT_YUV_MATRIX, &value);
    if (FAILED(hr)) return hr;

    pInfo->matrix = static_cast<MFVideoTransferMatrix>(value);
    if (pInfo->matrix != MFVideoTransferMatrix_BT601 &&
        pInfo->matrix != MFVideoTransferMatrix_BT709)
    {
        return MF_E_INVALIDMEDIATYPE;
    }

    hr = pType->GetUINT32(MF_MT_VIDEO_NOMINAL_RANGE, &value);
    if (FAILED(hr)) return hr;

    pInfo->nominalRange = static_cast<MFNominalRange>(value);
    if (pInfo->nominalRange != MFNominalRange_16_235)
    {
        return MF_E_INVALIDMEDIATYPE;
    }

    return S_OK;
}

// ---------------------------------------------------------------------------
// 5.5. buffer は stride を信じて読む
//   IMF2DBuffer::Lock2D が使えるならそれを優先し、
//   actual stride をそのまま使います。
// ---------------------------------------------------------------------------

class BufferLock
{
public:
    explicit BufferLock(IMFMediaBuffer* buffer)
        : m_buffer(buffer),
          m_2dBuffer(nullptr),
          m_locked(false)
    {
        if (m_buffer)
        {
            m_buffer->AddRef();
            m_buffer->QueryInterface(IID_PPV_ARGS(&m_2dBuffer));
        }
    }

    ~BufferLock()
    {
        Unlock();

        if (m_2dBuffer)
        {
            m_2dBuffer->Release();
            m_2dBuffer = nullptr;
        }

        if (m_buffer)
        {
            m_buffer->Release();
            m_buffer = nullptr;
        }
    }

    HRESULT Lock(
        LONG defaultStride,
        DWORD heightInPixels,
        BYTE** ppScanline0,
        LONG* pActualStride)
    {
        if (!m_buffer || !ppScanline0 || !pActualStride) return E_POINTER;
        if (m_locked) return MF_E_INVALIDREQUEST;

        if (m_2dBuffer)
        {
            HRESULT hr = m_2dBuffer->Lock2D(ppScanline0, pActualStride);
            if (FAILED(hr)) return hr;

            m_locked = true;
            return S_OK;
        }

        BYTE* pData = nullptr;
        HRESULT hr = m_buffer->Lock(&pData, nullptr, nullptr);
        if (FAILED(hr)) return hr;

        *pActualStride = defaultStride;
        if (defaultStride < 0)
        {
            *ppScanline0 =
                pData + static_cast<size_t>(-defaultStride) * (heightInPixels - 1);
        }
        else
        {
            *ppScanline0 = pData;
        }

        m_locked = true;
        return S_OK;
    }

    void Unlock()
    {
        if (!m_locked) return;

        if (m_2dBuffer)
        {
            m_2dBuffer->Unlock2D();
        }
        else
        {
            m_buffer->Unlock();
        }

        m_locked = false;
    }

private:
    IMFMediaBuffer* m_buffer;
    IMF2DBuffer* m_2dBuffer;
    bool m_locked;
};

// ---------------------------------------------------------------------------
// 5.6. 1 pixel の変換式をコードにする
//   OS 非依存のピクセル演算なのでヘッダーに分けています (内容は記事のまま)。
// ---------------------------------------------------------------------------

#include "YuvPixelToBgra.h"

// ---------------------------------------------------------------------------
// 5.7. NV12 を BGRA32 へ変換する
//   2x2 block の 4 pixel が同じ U/V を共有します。
//   chroma upsampling は nearest-neighbor 的な解釈です。
// ---------------------------------------------------------------------------

HRESULT ConvertNv12ToBgra32(
    IMFMediaBuffer* buffer,
    const DecodedFrameInfo& info,
    std::vector<BYTE>& dstBgra)
{
    if (!buffer) return E_POINTER;
    if (info.subtype != MFVideoFormat_NV12) return MF_E_INVALIDMEDIATYPE;
    if ((info.width & 1u) != 0 || (info.height & 1u) != 0)
    {
        return MF_E_INVALIDMEDIATYPE;
    }

    dstBgra.resize(static_cast<size_t>(info.width) * info.height * 4);

    BufferLock lock(buffer);

    BYTE* scanline0 = nullptr;
    LONG actualStride = 0;
    HRESULT hr = lock.Lock(
        info.defaultStride,
        info.height,
        &scanline0,
        &actualStride);
    if (FAILED(hr)) return hr;

    if (actualStride <= 0)
    {
        lock.Unlock();
        return MF_E_INVALIDMEDIATYPE;
    }

    const BYTE* yPlane = scanline0;
    const BYTE* uvPlane =
        scanline0 + static_cast<size_t>(actualStride) * info.height;

    for (UINT32 y = 0; y < info.height; ++y)
    {
        const BYTE* yRow = yPlane + static_cast<size_t>(actualStride) * y;
        const BYTE* uvRow = uvPlane + static_cast<size_t>(actualStride) * (y / 2);
        BYTE* dstRow =
            dstBgra.data() + static_cast<size_t>(info.width) * 4 * y;

        for (UINT32 x = 0; x < info.width; ++x)
        {
            const BYTE Y = yRow[x];
            const BYTE U = uvRow[(x / 2) * 2 + 0];
            const BYTE V = uvRow[(x / 2) * 2 + 1];

            hr = ConvertLimitedYuvPixelToBgra(
                Y,
                U,
                V,
                info.matrix,
                dstRow + static_cast<size_t>(x) * 4);
            if (FAILED(hr))
            {
                lock.Unlock();
                return hr;
            }
        }
    }

    lock.Unlock();
    return S_OK;
}

// ---------------------------------------------------------------------------
// 5.8. YUY2 を BGRA32 へ変換する
//   packed な 4:2:2。bytes は Y0 U Y1 V で並び、
//   横 2 pixel が 1 組の U/V を共有します。
// ---------------------------------------------------------------------------

#include <cstddef>

HRESULT ConvertYuy2ToBgra32(
    IMFMediaBuffer* buffer,
    const DecodedFrameInfo& info,
    std::vector<BYTE>& dstBgra)
{
    if (!buffer) return E_POINTER;
    if (info.subtype != MFVideoFormat_YUY2) return MF_E_INVALIDMEDIATYPE;
    if ((info.width & 1u) != 0) return MF_E_INVALIDMEDIATYPE;

    dstBgra.resize(static_cast<size_t>(info.width) * info.height * 4);

    BufferLock lock(buffer);

    BYTE* scanline0 = nullptr;
    LONG actualStride = 0;
    HRESULT hr = lock.Lock(
        info.defaultStride,
        info.height,
        &scanline0,
        &actualStride);
    if (FAILED(hr)) return hr;

    for (UINT32 y = 0; y < info.height; ++y)
    {
        const BYTE* src =
            scanline0 +
            static_cast<ptrdiff_t>(actualStride) * static_cast<ptrdiff_t>(y);

        BYTE* dstRow =
            dstBgra.data() + static_cast<size_t>(info.width) * 4 * y;

        for (UINT32 x = 0; x < info.width; x += 2)
        {
            const BYTE Y0 = src[0];
            const BYTE U  = src[1];
            const BYTE Y1 = src[2];
            const BYTE V  = src[3];

            hr = ConvertLimitedYuvPixelToBgra(
                Y0,
                U,
                V,
                info.matrix,
                dstRow + static_cast<size_t>(x) * 4);
            if (FAILED(hr))
            {
                lock.Unlock();
                return hr;
            }

            hr = ConvertLimitedYuvPixelToBgra(
                Y1,
                U,
                V,
                info.matrix,
                dstRow + static_cast<size_t>(x + 1) * 4);
            if (FAILED(hr))
            {
                lock.Unlock();
                return hr;
            }

            src += 4;
        }
    }

    lock.Unlock();
    return S_OK;
}

// ---------------------------------------------------------------------------
// 5.9. sample から呼ぶときの入口
// ---------------------------------------------------------------------------

HRESULT ConvertSampleToBgra32(
    IMFSample* sample,
    const DecodedFrameInfo& info,
    std::vector<BYTE>& dstBgra)
{
    if (!sample) return E_POINTER;

    ComPtr<IMFMediaBuffer> buffer;
    HRESULT hr = sample->ConvertToContiguousBuffer(&buffer);
    if (FAILED(hr)) return hr;

    if (info.subtype == MFVideoFormat_NV12)
    {
        return ConvertNv12ToBgra32(buffer.Get(), info, dstBgra);
    }

    if (info.subtype == MFVideoFormat_YUY2)
    {
        return ConvertYuy2ToBgra32(buffer.Get(), info, dstBgra);
    }

    return MF_E_INVALIDMEDIATYPE;
}

// ---------------------------------------------------------------------------
// 5.9. 実際の呼び出し側の例
//   記事ではコード片として示されている部分を、
//   コンパイルできるように関数で包んだものです (中身は記事のまま)。
// ---------------------------------------------------------------------------

HRESULT ReadAndConvertOneFrameExample(IMFSourceReader* reader)
{
    ComPtr<IMFMediaType> currentType;
    HRESULT hr = reader->GetCurrentMediaType(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        &currentType);
    if (FAILED(hr)) return hr;

    DecodedFrameInfo info;
    hr = GetStrictDecodedFrameInfo(currentType.Get(), &info);
    if (FAILED(hr)) return hr;

    DWORD flags = 0;
    LONGLONG timestamp = 0;
    ComPtr<IMFSample> sample;

    hr = reader->ReadSample(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        0,
        nullptr,
        &flags,
        &timestamp,
        &sample);
    if (FAILED(hr)) return hr;
    if (flags & MF_SOURCE_READERF_ENDOFSTREAM) return MF_E_END_OF_STREAM;
    if (!sample) return MF_E_INVALID_STREAM_DATA;

    std::vector<BYTE> bgra;
    hr = ConvertSampleToBgra32(sample.Get(), info, bgra);
    if (FAILED(hr)) return hr;

    // bgra は top-down / 32bpp BGRA として扱える
    return S_OK;
}
