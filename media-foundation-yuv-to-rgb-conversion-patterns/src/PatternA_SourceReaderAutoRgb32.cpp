// パターンA: IMFSourceReader に RGB32 まで自動で持っていかせる
// 記事 4.3 のコード。
// CoInitializeEx と MFStartup が済んでいる前提です。

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

HRESULT CreateSourceReaderWithAutoRgb(
    const wchar_t* path,
    IMFSourceReader** ppReader)
{
    if (!path || !ppReader) return E_POINTER;
    *ppReader = nullptr;

    ComPtr<IMFAttributes> attrs;
    HRESULT hr = MFCreateAttributes(&attrs, 2);
    if (FAILED(hr)) return hr;

    hr = attrs->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, TRUE);
    if (FAILED(hr)) return hr;

    hr = MFCreateSourceReaderFromURL(path, attrs.Get(), ppReader);
    if (FAILED(hr)) return hr;

    hr = (*ppReader)->SetStreamSelection(MF_SOURCE_READER_ALL_STREAMS, FALSE);
    if (FAILED(hr)) return hr;

    hr = (*ppReader)->SetStreamSelection(MF_SOURCE_READER_FIRST_VIDEO_STREAM, TRUE);
    if (FAILED(hr)) return hr;

    ComPtr<IMFMediaType> outType;
    hr = MFCreateMediaType(&outType);
    if (FAILED(hr)) return hr;

    hr = outType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
    if (FAILED(hr)) return hr;

    hr = outType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB32);
    if (FAILED(hr)) return hr;

    hr = (*ppReader)->SetCurrentMediaType(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        nullptr,
        outType.Get());
    if (FAILED(hr)) return hr;

    return S_OK;
}

HRESULT ReadOneRgb32Sample(
    IMFSourceReader* reader,
    IMFSample** ppSample,
    LONGLONG* pTimestamp100ns)
{
    if (!reader || !ppSample) return E_POINTER;
    *ppSample = nullptr;
    if (pTimestamp100ns) *pTimestamp100ns = 0;

    DWORD streamIndex = 0;
    DWORD flags = 0;
    LONGLONG timestamp = 0;

    HRESULT hr = reader->ReadSample(
        MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        0,
        &streamIndex,
        &flags,
        &timestamp,
        ppSample);

    if (FAILED(hr)) return hr;
    if (flags & MF_SOURCE_READERF_ENDOFSTREAM) return MF_E_END_OF_STREAM;
    if (*ppSample == nullptr) return MF_E_INVALID_STREAM_DATA;

    if (pTimestamp100ns) *pTimestamp100ns = timestamp;
    return S_OK;
}
