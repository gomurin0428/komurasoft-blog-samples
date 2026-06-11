#define NOMINMAX
#if defined(_MSC_VER)
#  if __has_include("pch.h")
#    include "pch.h"
#  elif __has_include("stdafx.h")
#    include "stdafx.h"
#  endif
#endif
#include <windows.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mferror.h>
#include <mfobjects.h>
#include <propvarutil.h>
#include <wincodec.h>

#include <cerrno>
#include <cstdio>
#include <cstdlib>
#include <cwchar>
#include <cmath>
#include <cstring>
#include <limits>
#include <vector>

#pragma comment(lib, "mfplat.lib")
#pragma comment(lib, "mfreadwrite.lib")
#pragma comment(lib, "mfuuid.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "propsys.lib")
#pragma comment(lib, "windowscodecs.lib")

template <class T>
void SafeRelease(T** pp)
{
    if (pp != nullptr && *pp != nullptr)
    {
        (*pp)->Release();
        *pp = nullptr;
    }
}

class MediaFoundationScope
{
public:
    MediaFoundationScope() : m_comInitialized(false), m_mfStarted(false)
    {
    }

    HRESULT Initialize()
    {
        HRESULT hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
        if (hr == RPC_E_CHANGED_MODE)
        {
            return hr;
        }

        if (SUCCEEDED(hr))
        {
            m_comInitialized = true;
        }

        hr = MFStartup(MF_VERSION);
        if (FAILED(hr))
        {
            if (m_comInitialized)
            {
                CoUninitialize();
                m_comInitialized = false;
            }
            return hr;
        }

        m_mfStarted = true;
        return S_OK;
    }

    ~MediaFoundationScope()
    {
        if (m_mfStarted)
        {
            MFShutdown();
        }

        if (m_comInitialized)
        {
            CoUninitialize();
        }
    }

private:
    bool m_comInitialized;
    bool m_mfStarted;
};

HRESULT GetPresentationDuration(IMFSourceReader* pReader, LONGLONG* phnsDuration)
{
    if (pReader == nullptr || phnsDuration == nullptr)
    {
        return E_POINTER;
    }

    PROPVARIANT var;
    PropVariantInit(&var);

    HRESULT hr = pReader->GetPresentationAttribute(
        MF_SOURCE_READER_MEDIASOURCE,
        MF_PD_DURATION,
        &var);

    if (SUCCEEDED(hr))
    {
        hr = PropVariantToInt64(var, phnsDuration);
    }

    PropVariantClear(&var);
    return hr;
}

HRESULT GetDefaultStride(IMFMediaType* pType, LONG* plStride)
{
    if (pType == nullptr || plStride == nullptr)
    {
        return E_POINTER;
    }

    LONG lStride = 0;
    HRESULT hr = pType->GetUINT32(
        MF_MT_DEFAULT_STRIDE,
        reinterpret_cast<UINT32*>(&lStride));

    if (FAILED(hr))
    {
        GUID subtype = GUID_NULL;
        UINT32 width = 0;
        UINT32 height = 0;

        hr = pType->GetGUID(MF_MT_SUBTYPE, &subtype);
        if (FAILED(hr))
        {
            return hr;
        }

        hr = MFGetAttributeSize(pType, MF_MT_FRAME_SIZE, &width, &height);
        if (FAILED(hr))
        {
            return hr;
        }

        hr = MFGetStrideForBitmapInfoHeader(subtype.Data1, width, &lStride);
        if (FAILED(hr))
        {
            return hr;
        }

        (void)pType->SetUINT32(MF_MT_DEFAULT_STRIDE, static_cast<UINT32>(lStride));
    }

    *plStride = lStride;
    return S_OK;
}

class BufferLock
{
public:
    explicit BufferLock(IMFMediaBuffer* pBuffer)
        : m_pBuffer(pBuffer),
          m_p2DBuffer(nullptr),
          m_locked(false)
    {
        if (m_pBuffer != nullptr)
        {
            m_pBuffer->AddRef();
            (void)m_pBuffer->QueryInterface(IID_PPV_ARGS(&m_p2DBuffer));
        }
    }

    ~BufferLock()
    {
        UnlockBuffer();
        SafeRelease(&m_p2DBuffer);
        SafeRelease(&m_pBuffer);
    }

    HRESULT LockBuffer(
        LONG defaultStride,
        DWORD heightInPixels,
        BYTE** ppScanLine0,
        LONG* plStride)
    {
        if (ppScanLine0 == nullptr || plStride == nullptr)
        {
            return E_POINTER;
        }

        *ppScanLine0 = nullptr;
        *plStride = 0;

        HRESULT hr = S_OK;

        if (m_p2DBuffer != nullptr)
        {
            hr = m_p2DBuffer->Lock2D(ppScanLine0, plStride);
        }
        else
        {
            BYTE* pData = nullptr;
            hr = m_pBuffer->Lock(&pData, nullptr, nullptr);
            if (SUCCEEDED(hr))
            {
                *plStride = defaultStride;

                if (defaultStride < 0)
                {
                    const size_t strideAbs = static_cast<size_t>(-defaultStride);
                    *ppScanLine0 = pData + strideAbs * (heightInPixels - 1);
                }
                else
                {
                    *ppScanLine0 = pData;
                }
            }
        }

        m_locked = SUCCEEDED(hr);
        return hr;
    }

    void UnlockBuffer()
    {
        if (!m_locked)
        {
            return;
        }

        if (m_p2DBuffer != nullptr)
        {
            (void)m_p2DBuffer->Unlock2D();
        }
        else if (m_pBuffer != nullptr)
        {
            (void)m_pBuffer->Unlock();
        }

        m_locked = false;
    }

private:
    IMFMediaBuffer* m_pBuffer;
    IMF2DBuffer* m_p2DBuffer;
    bool m_locked;
};

HRESULT CreateConfiguredSourceReader(PCWSTR inputPath, IMFSourceReader** ppReader)
{
    if (inputPath == nullptr || ppReader == nullptr)
    {
        return E_POINTER;
    }

    *ppReader = nullptr;

    IMFAttributes* pAttributes = nullptr;
    IMFSourceReader* pReader = nullptr;
    IMFMediaType* pRequestedType = nullptr;

    HRESULT hr = MFCreateAttributes(&pAttributes, 1);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, TRUE);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = MFCreateSourceReaderFromURL(inputPath, pAttributes, &pReader);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pReader->SetStreamSelection(MF_SOURCE_READER_ALL_STREAMS, FALSE);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pReader->SetStreamSelection(MF_SOURCE_READER_FIRST_VIDEO_STREAM, TRUE);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = MFCreateMediaType(&pRequestedType);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pRequestedType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pRequestedType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB32);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pReader->SetCurrentMediaType(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        nullptr,
        pRequestedType);
    if (FAILED(hr))
    {
        goto done;
    }

    *ppReader = pReader;
    pReader = nullptr;

done:
    SafeRelease(&pRequestedType);
    SafeRelease(&pReader);
    SafeRelease(&pAttributes);
    return hr;
}

HRESULT SeekSourceReader(IMFSourceReader* pReader, LONGLONG targetHns)
{
    if (pReader == nullptr)
    {
        return E_POINTER;
    }

    PROPVARIANT var;
    PropVariantInit(&var);

    HRESULT hr = InitPropVariantFromInt64(targetHns, &var);
    if (SUCCEEDED(hr))
    {
        hr = pReader->SetCurrentPosition(GUID_NULL, var);
    }

    PropVariantClear(&var);
    return hr;
}

HRESULT ReadNearestVideoSample(
    IMFSourceReader* pReader,
    LONGLONG targetHns,
    IMFSample** ppSample,
    LONGLONG* pChosenTimestampHns)
{
    if (pReader == nullptr || ppSample == nullptr)
    {
        return E_POINTER;
    }

    *ppSample = nullptr;
    if (pChosenTimestampHns != nullptr)
    {
        *pChosenTimestampHns = 0;
    }

    IMFSample* pBefore = nullptr;
    LONGLONG beforeTimestamp = 0;
    bool hasBefore = false;

    HRESULT hr = S_OK;

    for (;;)
    {
        IMFSample* pCurrent = nullptr;
        DWORD flags = 0;
        LONGLONG currentTimestamp = 0;
        LONGLONG diffBefore = 0;
        LONGLONG diffCurrent = 0;

        hr = pReader->ReadSample(
            MF_SOURCE_READER_FIRST_VIDEO_STREAM,
            0,
            nullptr,
            &flags,
            &currentTimestamp,
            &pCurrent);

        if (FAILED(hr))
        {
            SafeRelease(&pCurrent);
            break;
        }

        if ((flags & MF_SOURCE_READERF_ENDOFSTREAM) != 0)
        {
            SafeRelease(&pCurrent);

            if (hasBefore)
            {
                *ppSample = pBefore;
                pBefore = nullptr;

                if (pChosenTimestampHns != nullptr)
                {
                    *pChosenTimestampHns = beforeTimestamp;
                }

                hr = S_OK;
            }
            else
            {
                hr = MF_E_END_OF_STREAM;
            }
            break;
        }

        if ((flags & MF_SOURCE_READERF_STREAMTICK) != 0)
        {
            SafeRelease(&pCurrent);
            continue;
        }

        if (pCurrent == nullptr)
        {
            continue;
        }

        if (currentTimestamp < targetHns)
        {
            SafeRelease(&pBefore);
            pBefore = pCurrent;
            pCurrent = nullptr;
            beforeTimestamp = currentTimestamp;
            hasBefore = true;
            continue;
        }

        if (hasBefore)
        {
            diffBefore = targetHns - beforeTimestamp;
            diffCurrent = currentTimestamp - targetHns;

            if (diffBefore <= diffCurrent)
            {
                *ppSample = pBefore;
                pBefore = nullptr;

                if (pChosenTimestampHns != nullptr)
                {
                    *pChosenTimestampHns = beforeTimestamp;
                }

                SafeRelease(&pCurrent);
            }
            else
            {
                *ppSample = pCurrent;
                pCurrent = nullptr;

                if (pChosenTimestampHns != nullptr)
                {
                    *pChosenTimestampHns = currentTimestamp;
                }
            }
        }
        else
        {
            *ppSample = pCurrent;
            pCurrent = nullptr;

            if (pChosenTimestampHns != nullptr)
            {
                *pChosenTimestampHns = currentTimestamp;
            }
        }

        hr = S_OK;
        break;
    }

    SafeRelease(&pBefore);
    return hr;
}

HRESULT CopyContiguousBufferToTopDownBgra(
    IMFMediaBuffer* pBuffer,
    LONG defaultStride,
    UINT32 width,
    UINT32 height,
    std::vector<BYTE>& pixels,
    UINT32* pStride)
{
    if (pBuffer == nullptr || pStride == nullptr)
    {
        return E_POINTER;
    }

    BufferLock lock(pBuffer);

    BYTE* pScanLine0 = nullptr;
    LONG actualStride = 0;

    HRESULT hr = lock.LockBuffer(defaultStride, height, &pScanLine0, &actualStride);
    if (FAILED(hr))
    {
        return hr;
    }

    if (width > (std::numeric_limits<UINT32>::max() / 4))
    {
        return E_INVALIDARG;
    }

    const UINT32 destStride = width * 4;
    const LONG actualStrideAbs = (actualStride < 0) ? -actualStride : actualStride;
    if (actualStrideAbs < static_cast<LONG>(destStride))
    {
        return E_UNEXPECTED;
    }

    pixels.resize(static_cast<size_t>(destStride) * height);

    BYTE* pDestRow = pixels.data();
    BYTE* pSrcRow = pScanLine0;

    for (UINT32 y = 0; y < height; ++y)
    {
        std::memcpy(pDestRow, pSrcRow, destStride);

        // MFVideoFormat_RGB32 の 4 byte 目は alpha と限らないので、
        // PNG 保存前に不透明へ固定する。
        for (UINT32 x = 0; x < width; ++x)
        {
            pDestRow[static_cast<size_t>(x) * 4 + 3] = 0xFF;
        }

        pDestRow += destStride;
        pSrcRow += actualStride;
    }

    *pStride = destStride;
    return S_OK;
}

HRESULT CopySampleToTopDownBgra(
    IMFSample* pSample,
    IMFMediaType* pCurrentType,
    std::vector<BYTE>& pixels,
    UINT32* pWidth,
    UINT32* pHeight,
    UINT32* pStride)
{
    if (pSample == nullptr || pCurrentType == nullptr ||
        pWidth == nullptr || pHeight == nullptr || pStride == nullptr)
    {
        return E_POINTER;
    }

    *pWidth = 0;
    *pHeight = 0;
    *pStride = 0;

    IMFMediaBuffer* pBuffer = nullptr;

    GUID subtype = GUID_NULL;
    UINT32 width = 0;
    UINT32 height = 0;
    LONG defaultStride = 0;

    HRESULT hr = pCurrentType->GetGUID(MF_MT_SUBTYPE, &subtype);
    if (FAILED(hr))
    {
        goto done;
    }

    if (!IsEqualGUID(subtype, MFVideoFormat_RGB32))
    {
        hr = MF_E_INVALIDMEDIATYPE;
        goto done;
    }

    hr = MFGetAttributeSize(pCurrentType, MF_MT_FRAME_SIZE, &width, &height);
    if (FAILED(hr))
    {
        goto done;
    }

    if (width == 0 || height == 0)
    {
        hr = E_UNEXPECTED;
        goto done;
    }

    hr = GetDefaultStride(pCurrentType, &defaultStride);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pSample->ConvertToContiguousBuffer(&pBuffer);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = CopyContiguousBufferToTopDownBgra(
        pBuffer,
        defaultStride,
        width,
        height,
        pixels,
        pStride);
    if (FAILED(hr))
    {
        goto done;
    }

    *pWidth = width;
    *pHeight = height;

    hr = S_OK;

done:
    SafeRelease(&pBuffer);
    return hr;
}

HRESULT SaveBgraToPng(
    PCWSTR outputPath,
    const BYTE* pixels,
    UINT32 width,
    UINT32 height,
    UINT32 stride)
{
    if (outputPath == nullptr || pixels == nullptr)
    {
        return E_POINTER;
    }

    if (width == 0 || height == 0 || stride < width * 4)
    {
        return E_INVALIDARG;
    }

    const size_t bufferSizeSizeT = static_cast<size_t>(stride) * height;
    if (bufferSizeSizeT > static_cast<size_t>(std::numeric_limits<UINT>::max()))
    {
        return E_INVALIDARG;
    }

    const UINT bufferSize = static_cast<UINT>(bufferSizeSizeT);

    IWICImagingFactory* pFactory = nullptr;
    IWICStream* pStream = nullptr;
    IWICBitmapEncoder* pEncoder = nullptr;
    IWICBitmapFrameEncode* pFrame = nullptr;
    IPropertyBag2* pProps = nullptr;
    WICPixelFormatGUID pixelFormat = GUID_WICPixelFormat32bppBGRA;

    HRESULT hr = CoCreateInstance(
        CLSID_WICImagingFactory,
        nullptr,
        CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&pFactory));
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pFactory->CreateStream(&pStream);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pStream->InitializeFromFilename(outputPath, GENERIC_WRITE);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pFactory->CreateEncoder(GUID_ContainerFormatPng, nullptr, &pEncoder);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pEncoder->Initialize(pStream, WICBitmapEncoderNoCache);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pEncoder->CreateNewFrame(&pFrame, &pProps);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pFrame->Initialize(pProps);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pFrame->SetSize(width, height);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pFrame->SetPixelFormat(&pixelFormat);
    if (FAILED(hr))
    {
        goto done;
    }

    if (!IsEqualGUID(pixelFormat, GUID_WICPixelFormat32bppBGRA))
    {
        hr = WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT;
        goto done;
    }

    hr = pFrame->WritePixels(
        height,
        stride,
        bufferSize,
        const_cast<BYTE*>(pixels));
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pFrame->Commit();
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pEncoder->Commit();

done:
    SafeRelease(&pProps);
    SafeRelease(&pFrame);
    SafeRelease(&pEncoder);
    SafeRelease(&pStream);
    SafeRelease(&pFactory);
    return hr;
}

HRESULT ExtractFrameFromMp4ToPng(
    PCWSTR inputPath,
    LONGLONG targetHns,
    PCWSTR outputPath,
    LONGLONG* pActualTimestampHns)
{
    if (inputPath == nullptr || outputPath == nullptr)
    {
        return E_POINTER;
    }

    if (targetHns < 0)
    {
        return E_INVALIDARG;
    }

    MediaFoundationScope mf;
    HRESULT hr = mf.Initialize();
    if (FAILED(hr))
    {
        return hr;
    }

    IMFSourceReader* pReader = nullptr;
    IMFMediaType* pCurrentType = nullptr;
    IMFSample* pChosenSample = nullptr;

    LONGLONG durationHns = 0;
    UINT32 width = 0;
    UINT32 height = 0;
    UINT32 stride = 0;
    std::vector<BYTE> pixels;

    hr = CreateConfiguredSourceReader(inputPath, &pReader);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pReader->GetCurrentMediaType(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        &pCurrentType);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = GetPresentationDuration(pReader, &durationHns);
    if (FAILED(hr))
    {
        goto done;
    }

    if (targetHns >= durationHns)
    {
        hr = E_INVALIDARG;
        goto done;
    }

    hr = SeekSourceReader(pReader, targetHns);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = ReadNearestVideoSample(
        pReader,
        targetHns,
        &pChosenSample,
        pActualTimestampHns);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = CopySampleToTopDownBgra(
        pChosenSample,
        pCurrentType,
        pixels,
        &width,
        &height,
        &stride);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = SaveBgraToPng(outputPath, pixels.data(), width, height, stride);

done:
    SafeRelease(&pChosenSample);
    SafeRelease(&pCurrentType);
    SafeRelease(&pReader);
    return hr;
}

bool TryParseSeconds(PCWSTR text, LONGLONG* phns)
{
    if (text == nullptr || phns == nullptr)
    {
        return false;
    }

    wchar_t* end = nullptr;
    errno = 0;

    const double seconds = std::wcstod(text, &end);
    if (end == text || *end != L'\0' || errno != 0)
    {
        return false;
    }

    if (!std::isfinite(seconds) || seconds < 0.0)
    {
        return false;
    }

    const long double hns =
        static_cast<long double>(seconds) * 10000000.0L;

    if (hns < 0.0L ||
        hns > static_cast<long double>(std::numeric_limits<LONGLONG>::max()))
    {
        return false;
    }

    *phns = static_cast<LONGLONG>(std::llround(hns));
    return true;
}

double HnsToSeconds(LONGLONG hns)
{
    return static_cast<double>(hns) / 10000000.0;
}

void PrintUsage()
{
    std::fwprintf(stderr, L"Usage:\n");
    std::fwprintf(stderr, L"  ExtractFrameFromMp4.exe <input.mp4> <seconds> <output.png>\n");
    std::fwprintf(stderr, L"\nExample:\n");
    std::fwprintf(stderr, L"  ExtractFrameFromMp4.exe input.mp4 12.345 output.png\n");
}

int wmain(int argc, wchar_t* argv[])
{
    if (argc != 4)
    {
        PrintUsage();
        return 1;
    }

    LONGLONG targetHns = 0;
    if (!TryParseSeconds(argv[2], &targetHns))
    {
        std::fwprintf(stderr, L"Invalid seconds: %ls\n", argv[2]);
        return 1;
    }

    LONGLONG actualHns = 0;
    HRESULT hr = ExtractFrameFromMp4ToPng(
        argv[1],
        targetHns,
        argv[3],
        &actualHns);

    if (FAILED(hr))
    {
        std::fwprintf(stderr, L"Failed. HRESULT = 0x%08lX\n", static_cast<unsigned long>(hr));
        return 1;
    }

    std::wprintf(L"Saved: %ls\n", argv[3]);
    std::wprintf(L"Requested: %.3f sec\n", HnsToSeconds(targetHns));
    std::wprintf(L"Actual: %.3f sec\n", HnsToSeconds(actualHns));
    return 0;
}
