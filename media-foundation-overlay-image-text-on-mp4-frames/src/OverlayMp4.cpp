#define NOMINMAX
#include <windows.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <mferror.h>
#include <gdiplus.h>
#include <wrl/client.h>

#include <algorithm>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cwchar>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

#pragma comment(lib, "mfplat.lib")
#pragma comment(lib, "mfreadwrite.lib")
#pragma comment(lib, "mfuuid.lib")
#pragma comment(lib, "mf.lib")
#pragma comment(lib, "gdiplus.lib")

using Microsoft::WRL::ComPtr;

namespace
{
    const wchar_t* kOverlayText = L"HelloWorld";
    const float kMarginRatio = 0.03f;
    const float kImageMaxWidthRatio = 0.20f;
    const float kImageMaxHeightRatio = 0.20f;
    const float kMinFontPx = 24.0f;

    std::string HrToHex(HRESULT hr)
    {
        char buf[32]{};
        std::snprintf(buf, sizeof(buf), "0x%08X", static_cast<unsigned int>(hr));
        return std::string(buf);
    }

    void ThrowIfFailed(HRESULT hr, const char* message)
    {
        if (FAILED(hr))
        {
            throw std::runtime_error(std::string(message) + " failed. HRESULT=" + HrToHex(hr));
        }
    }

    void ThrowIfGdiplusError(Gdiplus::Status status, const char* message)
    {
        if (status != Gdiplus::Ok)
        {
            char buf[128]{};
            std::snprintf(buf, sizeof(buf), "%s failed. GDI+ status=%d", message, static_cast<int>(status));
            throw std::runtime_error(buf);
        }
    }

    BYTE ClampToByte(int value)
    {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return static_cast<BYTE>(value);
    }

    class ScopedGdiplus
    {
    public:
        ScopedGdiplus()
        {
            Gdiplus::GdiplusStartupInput input;
            ThrowIfGdiplusError(Gdiplus::GdiplusStartup(&token_, &input, nullptr), "GdiplusStartup");
        }

        ~ScopedGdiplus()
        {
            if (token_ != 0)
            {
                Gdiplus::GdiplusShutdown(token_);
            }
        }

    private:
        ULONG_PTR token_ = 0;
    };

    class ScopedMf
    {
    public:
        ScopedMf()
        {
            ThrowIfFailed(CoInitializeEx(nullptr, COINIT_MULTITHREADED), "CoInitializeEx");
            comInitialized_ = true;

            ThrowIfFailed(MFStartup(MF_VERSION), "MFStartup");
            mfStarted_ = true;
        }

        ~ScopedMf()
        {
            if (mfStarted_)
            {
                MFShutdown();
            }

            if (comInitialized_)
            {
                CoUninitialize();
            }
        }

    private:
        bool comInitialized_ = false;
        bool mfStarted_ = false;
    };

    class BufferLock
    {
    public:
        explicit BufferLock(IMFMediaBuffer* buffer)
            : buffer_(buffer)
        {
            if (!buffer_)
            {
                throw std::runtime_error("BufferLock received a null buffer.");
            }

            buffer_.As(&buffer2D_);
        }

        HRESULT LockBuffer(LONG defaultStride, DWORD heightInPixels, BYTE** scanline0, LONG* actualStride)
        {
            if (scanline0 == nullptr || actualStride == nullptr)
            {
                return E_POINTER;
            }

            HRESULT hr = S_OK;

            if (buffer2D_)
            {
                hr = buffer2D_->Lock2D(scanline0, actualStride);
            }
            else
            {
                BYTE* data = nullptr;
                hr = buffer_->Lock(&data, nullptr, nullptr);
                if (SUCCEEDED(hr))
                {
                    *actualStride = defaultStride;
                    if (defaultStride < 0)
                    {
                        *scanline0 = data + (static_cast<LONG>(heightInPixels) - 1) * std::abs(defaultStride);
                    }
                    else
                    {
                        *scanline0 = data;
                    }
                }
            }

            locked_ = SUCCEEDED(hr);
            return hr;
        }

        ~BufferLock()
        {
            if (!locked_)
            {
                return;
            }

            if (buffer2D_)
            {
                buffer2D_->Unlock2D();
            }
            else
            {
                buffer_->Unlock();
            }
        }

    private:
        ComPtr<IMFMediaBuffer> buffer_;
        ComPtr<IMF2DBuffer> buffer2D_;
        bool locked_ = false;
    };

    struct VideoFormatInfo
    {
        UINT32 width = 0;
        UINT32 height = 0;
        UINT32 fpsNum = 0;
        UINT32 fpsDen = 0;
        UINT32 parNum = 1;
        UINT32 parDen = 1;
        LONG sourceStride = 0;
        LONGLONG defaultFrameDuration = 0;
        UINT32 bitrate = 0;
    };

    LONG GetDefaultStride(IMFMediaType* type)
    {
        LONG stride = 0;

        HRESULT hr = type->GetUINT32(MF_MT_DEFAULT_STRIDE, reinterpret_cast<UINT32*>(&stride));
        if (SUCCEEDED(hr))
        {
            return stride;
        }

        GUID subtype = GUID_NULL;
        UINT32 width = 0;
        UINT32 height = 0;

        ThrowIfFailed(type->GetGUID(MF_MT_SUBTYPE, &subtype), "GetGUID(MF_MT_SUBTYPE)");
        ThrowIfFailed(MFGetAttributeSize(type, MF_MT_FRAME_SIZE, &width, &height), "MFGetAttributeSize(MF_MT_FRAME_SIZE)");
        ThrowIfFailed(MFGetStrideForBitmapInfoHeader(subtype.Data1, width, &stride), "MFGetStrideForBitmapInfoHeader");
        ThrowIfFailed(type->SetUINT32(MF_MT_DEFAULT_STRIDE, static_cast<UINT32>(stride)), "SetUINT32(MF_MT_DEFAULT_STRIDE)");

        return stride;
    }

    UINT32 ChooseBitrate(IMFMediaType* nativeType, UINT32 width, UINT32 height, UINT32 fpsNum, UINT32 fpsDen)
    {
        UINT32 srcBitrate = 0;
        if (SUCCEEDED(nativeType->GetUINT32(MF_MT_AVG_BITRATE, &srcBitrate)) && srcBitrate > 0)
        {
            return srcBitrate;
        }

        const double fps = static_cast<double>(fpsNum) / static_cast<double>(fpsDen);
        double estimated = static_cast<double>(width) * static_cast<double>(height) * fps * 0.07;

        if (estimated < 1500000.0)
        {
            estimated = 1500000.0;
        }

        if (estimated > 25000000.0)
        {
            estimated = 25000000.0;
        }

        return static_cast<UINT32>(estimated);
    }

    VideoFormatInfo ConfigureSourceReader(IMFSourceReader* reader)
    {
        ThrowIfFailed(reader->SetStreamSelection(MF_SOURCE_READER_ALL_STREAMS, FALSE), "SetStreamSelection(all,false)");
        ThrowIfFailed(reader->SetStreamSelection(MF_SOURCE_READER_FIRST_VIDEO_STREAM, TRUE), "SetStreamSelection(video,true)");

        ComPtr<IMFMediaType> nativeType;
        ThrowIfFailed(reader->GetNativeMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, &nativeType), "GetNativeMediaType(video)");

        ComPtr<IMFMediaType> requestedType;
        ThrowIfFailed(MFCreateMediaType(&requestedType), "MFCreateMediaType(video requested)");
        ThrowIfFailed(requestedType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video), "SetGUID(video requested major)");
        ThrowIfFailed(requestedType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB32), "SetGUID(video requested subtype RGB32)");
        ThrowIfFailed(reader->SetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, nullptr, requestedType.Get()), "SetCurrentMediaType(video RGB32)");

        ComPtr<IMFMediaType> currentType;
        ThrowIfFailed(reader->GetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, &currentType), "GetCurrentMediaType(video)");

        VideoFormatInfo info;
        ThrowIfFailed(MFGetAttributeSize(currentType.Get(), MF_MT_FRAME_SIZE, &info.width, &info.height), "Get video frame size");

        HRESULT hr = MFGetAttributeRatio(currentType.Get(), MF_MT_FRAME_RATE, &info.fpsNum, &info.fpsDen);
        if (FAILED(hr))
        {
            ThrowIfFailed(MFGetAttributeRatio(nativeType.Get(), MF_MT_FRAME_RATE, &info.fpsNum, &info.fpsDen), "Get video frame rate");
        }

        if (info.fpsNum == 0 || info.fpsDen == 0)
        {
            throw std::runtime_error("Video frame rate is zero.");
        }

        hr = MFGetAttributeRatio(currentType.Get(), MF_MT_PIXEL_ASPECT_RATIO, &info.parNum, &info.parDen);
        if (FAILED(hr) || info.parNum == 0 || info.parDen == 0)
        {
            info.parNum = 1;
            info.parDen = 1;
        }

        info.sourceStride = GetDefaultStride(currentType.Get());
        info.defaultFrameDuration = (10000000LL * info.fpsDen) / info.fpsNum;
        if (info.defaultFrameDuration <= 0)
        {
            throw std::runtime_error("Calculated frame duration is invalid.");
        }

        info.bitrate = ChooseBitrate(nativeType.Get(), info.width, info.height, info.fpsNum, info.fpsDen);
        return info;
    }

    ComPtr<IMFSinkWriter> CreateSinkWriter(const std::wstring& outputPath, const VideoFormatInfo& videoInfo, DWORD* streamIndex)
    {
        if (streamIndex == nullptr)
        {
            throw std::runtime_error("streamIndex is null.");
        }

        ComPtr<IMFAttributes> attributes;
        ThrowIfFailed(MFCreateAttributes(&attributes, 1), "MFCreateAttributes(sink)");
        ThrowIfFailed(attributes->SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, TRUE), "SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS)");

        ComPtr<IMFSinkWriter> writer;
        ThrowIfFailed(MFCreateSinkWriterFromURL(outputPath.c_str(), nullptr, attributes.Get(), &writer), "MFCreateSinkWriterFromURL");

        ComPtr<IMFMediaType> outputType;
        ThrowIfFailed(MFCreateMediaType(&outputType), "MFCreateMediaType(video output)");
        ThrowIfFailed(outputType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video), "SetGUID(output major)");
        ThrowIfFailed(outputType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_H264), "SetGUID(output subtype H264)");
        ThrowIfFailed(outputType->SetUINT32(MF_MT_AVG_BITRATE, videoInfo.bitrate), "SetUINT32(output bitrate)");
        ThrowIfFailed(outputType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive), "SetUINT32(output interlace)");
        ThrowIfFailed(MFSetAttributeSize(outputType.Get(), MF_MT_FRAME_SIZE, videoInfo.width, videoInfo.height), "MFSetAttributeSize(output frame size)");
        ThrowIfFailed(MFSetAttributeRatio(outputType.Get(), MF_MT_FRAME_RATE, videoInfo.fpsNum, videoInfo.fpsDen), "MFSetAttributeRatio(output fps)");
        ThrowIfFailed(MFSetAttributeRatio(outputType.Get(), MF_MT_PIXEL_ASPECT_RATIO, videoInfo.parNum, videoInfo.parDen), "MFSetAttributeRatio(output PAR)");
        ThrowIfFailed(writer->AddStream(outputType.Get(), streamIndex), "AddStream(video)");

        ComPtr<IMFMediaType> inputType;
        ThrowIfFailed(MFCreateMediaType(&inputType), "MFCreateMediaType(video input)");
        ThrowIfFailed(inputType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video), "SetGUID(input major)");
        ThrowIfFailed(inputType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_NV12), "SetGUID(input subtype NV12)");
        ThrowIfFailed(inputType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive), "SetUINT32(input interlace)");
        ThrowIfFailed(MFSetAttributeSize(inputType.Get(), MF_MT_FRAME_SIZE, videoInfo.width, videoInfo.height), "MFSetAttributeSize(input frame size)");
        ThrowIfFailed(MFSetAttributeRatio(inputType.Get(), MF_MT_FRAME_RATE, videoInfo.fpsNum, videoInfo.fpsDen), "MFSetAttributeRatio(input fps)");
        ThrowIfFailed(MFSetAttributeRatio(inputType.Get(), MF_MT_PIXEL_ASPECT_RATIO, videoInfo.parNum, videoInfo.parDen), "MFSetAttributeRatio(input PAR)");
        ThrowIfFailed(writer->SetInputMediaType(*streamIndex, inputType.Get(), nullptr), "SetInputMediaType(video)");

        ThrowIfFailed(writer->BeginWriting(), "BeginWriting");
        return writer;
    }

    void CopySampleToTopDownBgra(IMFSample* sample, const VideoFormatInfo& videoInfo, std::vector<BYTE>& bgra)
    {
        ComPtr<IMFMediaBuffer> buffer;
        ThrowIfFailed(sample->ConvertToContiguousBuffer(&buffer), "ConvertToContiguousBuffer");

        BufferLock lock(buffer.Get());

        BYTE* scanline0 = nullptr;
        LONG actualStride = 0;
        ThrowIfFailed(lock.LockBuffer(videoInfo.sourceStride, videoInfo.height, &scanline0, &actualStride), "LockBuffer");

        const size_t dstStride = static_cast<size_t>(videoInfo.width) * 4;
        bgra.resize(dstStride * videoInfo.height);

        for (UINT32 y = 0; y < videoInfo.height; ++y)
        {
            const BYTE* srcRow = scanline0 + static_cast<LONG>(y) * actualStride;
            BYTE* dstRow = bgra.data() + static_cast<size_t>(y) * dstStride;
            std::memcpy(dstRow, srcRow, dstStride);

            for (UINT32 x = 0; x < videoInfo.width; ++x)
            {
                dstRow[static_cast<size_t>(x) * 4 + 3] = 0xFF;
            }
        }
    }

    void DrawOverlay(std::vector<BYTE>& bgra, UINT32 width, UINT32 height, Gdiplus::Image& overlayImage)
    {
        const INT stride = static_cast<INT>(width * 4);

        Gdiplus::Bitmap frameBitmap(
            static_cast<INT>(width),
            static_cast<INT>(height),
            stride,
            PixelFormat32bppPARGB,
            bgra.data());
        ThrowIfGdiplusError(frameBitmap.GetLastStatus(), "Create frame bitmap");

        Gdiplus::Graphics graphics(&frameBitmap);
        ThrowIfGdiplusError(graphics.GetLastStatus(), "Create graphics");

        graphics.SetCompositingMode(Gdiplus::CompositingModeSourceOver);
        graphics.SetCompositingQuality(Gdiplus::CompositingQualityHighQuality);
        graphics.SetInterpolationMode(Gdiplus::InterpolationModeHighQualityBicubic);
        graphics.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);
        graphics.SetTextRenderingHint(Gdiplus::TextRenderingHintAntiAliasGridFit);

        const Gdiplus::REAL margin = std::max<Gdiplus::REAL>(16.0f, static_cast<Gdiplus::REAL>(height) * kMarginRatio);
        const Gdiplus::REAL maxImageW = static_cast<Gdiplus::REAL>(width) * kImageMaxWidthRatio;
        const Gdiplus::REAL maxImageH = static_cast<Gdiplus::REAL>(height) * kImageMaxHeightRatio;

        const Gdiplus::REAL srcW = static_cast<Gdiplus::REAL>(overlayImage.GetWidth());
        const Gdiplus::REAL srcH = static_cast<Gdiplus::REAL>(overlayImage.GetHeight());
        if (srcW <= 0.0f || srcH <= 0.0f)
        {
            throw std::runtime_error("Overlay image has invalid size.");
        }

        const Gdiplus::REAL imageScale =
            std::min<Gdiplus::REAL>(1.0f, std::min(maxImageW / srcW, maxImageH / srcH));

        const Gdiplus::REAL drawW = srcW * imageScale;
        const Gdiplus::REAL drawH = srcH * imageScale;

        Gdiplus::RectF imageRect(margin, margin, drawW, drawH);
        Gdiplus::SolidBrush imagePlate(Gdiplus::Color(96, 0, 0, 0));
        graphics.FillRectangle(
            &imagePlate,
            imageRect.X - 8.0f,
            imageRect.Y - 8.0f,
            imageRect.Width + 16.0f,
            imageRect.Height + 16.0f);

        graphics.DrawImage(&overlayImage, imageRect);

        const Gdiplus::REAL fontPx =
            std::max<Gdiplus::REAL>(kMinFontPx, static_cast<Gdiplus::REAL>(height) * 0.06f);

        Gdiplus::Font font(L"Segoe UI", fontPx, Gdiplus::FontStyleBold, Gdiplus::UnitPixel);
        ThrowIfGdiplusError(font.GetLastStatus(), "Create font");

        Gdiplus::StringFormat stringFormat;
        stringFormat.SetAlignment(Gdiplus::StringAlignmentNear);
        stringFormat.SetLineAlignment(Gdiplus::StringAlignmentNear);

        Gdiplus::RectF measureLayout(
            margin,
            static_cast<Gdiplus::REAL>(height) - margin - fontPx * 2.0f,
            static_cast<Gdiplus::REAL>(width) - margin * 2.0f,
            fontPx * 2.0f);

        Gdiplus::RectF measured;
        graphics.MeasureString(kOverlayText, -1, &font, measureLayout, &stringFormat, &measured);

        Gdiplus::RectF textBg(
            measured.X - 12.0f,
            measured.Y - 8.0f,
            measured.Width + 24.0f,
            measured.Height + 16.0f);

        Gdiplus::SolidBrush textPlate(Gdiplus::Color(128, 0, 0, 0));
        graphics.FillRectangle(&textPlate, textBg);

        Gdiplus::SolidBrush shadowBrush(Gdiplus::Color(220, 0, 0, 0));
        Gdiplus::RectF shadowLayout = measureLayout;
        shadowLayout.X += 2.0f;
        shadowLayout.Y += 2.0f;
        graphics.DrawString(kOverlayText, -1, &font, shadowLayout, &stringFormat, &shadowBrush);

        Gdiplus::SolidBrush textBrush(Gdiplus::Color(235, 255, 255, 255));
        graphics.DrawString(kOverlayText, -1, &font, measureLayout, &stringFormat, &textBrush);
    }

    void BgraToNv12(const BYTE* bgra, UINT32 width, UINT32 height, BYTE* nv12)
    {
        const bool useBt709 = (width > 1024 || height > 576);

        const int yR = useBt709 ? 47 : 66;
        const int yG = useBt709 ? 157 : 129;
        const int yB = useBt709 ? 16 : 25;

        const int uR = useBt709 ? -26 : -38;
        const int uG = useBt709 ? -87 : -74;
        const int uB = 112;

        const int vR = 112;
        const int vG = useBt709 ? -102 : -94;
        const int vB = useBt709 ? -10 : -18;

        BYTE* yPlane = nv12;
        BYTE* uvPlane = nv12 + static_cast<size_t>(width) * height;

        const size_t srcStride = static_cast<size_t>(width) * 4;

        for (UINT32 y = 0; y < height; ++y)
        {
            const BYTE* srcRow = bgra + static_cast<size_t>(y) * srcStride;
            BYTE* dstY = yPlane + static_cast<size_t>(y) * width;

            for (UINT32 x = 0; x < width; ++x)
            {
                const BYTE b = srcRow[x * 4 + 0];
                const BYTE g = srcRow[x * 4 + 1];
                const BYTE r = srcRow[x * 4 + 2];

                const int Y = ((yR * r + yG * g + yB * b + 128) >> 8) + 16;
                dstY[x] = ClampToByte(Y);
            }
        }

        for (UINT32 y = 0; y < height; y += 2)
        {
            const BYTE* row0 = bgra + static_cast<size_t>(y) * srcStride;
            const BYTE* row1 = bgra + static_cast<size_t>(y + 1) * srcStride;
            BYTE* dstUV = uvPlane + static_cast<size_t>(y / 2) * width;

            for (UINT32 x = 0; x < width; x += 2)
            {
                int b = 0;
                int g = 0;
                int r = 0;

                for (UINT32 dy = 0; dy < 2; ++dy)
                {
                    const BYTE* row = (dy == 0) ? row0 : row1;
                    for (UINT32 dx = 0; dx < 2; ++dx)
                    {
                        const UINT32 ix = x + dx;
                        b += row[ix * 4 + 0];
                        g += row[ix * 4 + 1];
                        r += row[ix * 4 + 2];
                    }
                }

                b = (b + 2) / 4;
                g = (g + 2) / 4;
                r = (r + 2) / 4;

                const int U = ((uR * r + uG * g + uB * b + 128) >> 8) + 128;
                const int V = ((vR * r + vG * g + vB * b + 128) >> 8) + 128;

                dstUV[x + 0] = ClampToByte(U);
                dstUV[x + 1] = ClampToByte(V);
            }
        }
    }

    ComPtr<IMFSample> CreateNv12Sample(
        const std::vector<BYTE>& bgra,
        const VideoFormatInfo& videoInfo,
        LONGLONG sampleTime,
        LONGLONG sampleDuration)
    {
        const DWORD bufferSize =
            static_cast<DWORD>(videoInfo.width * videoInfo.height * 3 / 2);

        ComPtr<IMFMediaBuffer> buffer;
        ThrowIfFailed(MFCreateMemoryBuffer(bufferSize, &buffer), "MFCreateMemoryBuffer");

        BYTE* dst = nullptr;
        DWORD maxLength = 0;
        DWORD currentLength = 0;
        ThrowIfFailed(buffer->Lock(&dst, &maxLength, &currentLength), "Lock(NV12 buffer)");

        try
        {
            BgraToNv12(bgra.data(), videoInfo.width, videoInfo.height, dst);
        }
        catch (...)
        {
            buffer->Unlock();
            throw;
        }

        ThrowIfFailed(buffer->Unlock(), "Unlock(NV12 buffer)");
        ThrowIfFailed(buffer->SetCurrentLength(bufferSize), "SetCurrentLength(NV12 buffer)");

        ComPtr<IMFSample> sample;
        ThrowIfFailed(MFCreateSample(&sample), "MFCreateSample");
        ThrowIfFailed(sample->AddBuffer(buffer.Get()), "AddBuffer(output sample)");
        ThrowIfFailed(sample->SetSampleTime(sampleTime), "SetSampleTime");
        ThrowIfFailed(sample->SetSampleDuration(sampleDuration), "SetSampleDuration");

        return sample;
    }
}

int wmain(int argc, wchar_t* argv[])
{
    if (argc != 4)
    {
        std::wcerr << L"Usage: OverlayMp4.exe <input.mp4> <overlayImage.png> <output.mp4>" << std::endl;
        return 1;
    }

    const std::wstring inputPath = argv[1];
    const std::wstring imagePath = argv[2];
    const std::wstring outputPath = argv[3];

    try
    {
        if (_wcsicmp(inputPath.c_str(), outputPath.c_str()) == 0)
        {
            throw std::runtime_error("Input and output paths must be different.");
        }

        ScopedMf mf;
        ScopedGdiplus gdiplus;

        ComPtr<IMFAttributes> readerAttributes;
        ThrowIfFailed(MFCreateAttributes(&readerAttributes, 1), "MFCreateAttributes(reader)");
        ThrowIfFailed(
            readerAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, TRUE),
            "SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING)");

        ComPtr<IMFSourceReader> reader;
        ThrowIfFailed(
            MFCreateSourceReaderFromURL(inputPath.c_str(), readerAttributes.Get(), &reader),
            "MFCreateSourceReaderFromURL");

        VideoFormatInfo videoInfo = ConfigureSourceReader(reader.Get());

        if ((videoInfo.width % 2) != 0 || (videoInfo.height % 2) != 0)
        {
            throw std::runtime_error(
                "This sample requires even video width and height because NV12 is 4:2:0.");
        }

        Gdiplus::Image overlayImage(imagePath.c_str());
        ThrowIfGdiplusError(overlayImage.GetLastStatus(), "Load overlay image");

        DWORD videoStreamIndex = 0;
        ComPtr<IMFSinkWriter> writer =
            CreateSinkWriter(outputPath, videoInfo, &videoStreamIndex);

        std::vector<BYTE> bgra;
        LONGLONG firstTimestamp = -1;
        unsigned long long frameCount = 0;

        while (true)
        {
            DWORD flags = 0;
            LONGLONG timestamp = 0;
            ComPtr<IMFSample> inputSample;

            ThrowIfFailed(
                reader->ReadSample(
                    MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    0,
                    nullptr,
                    &flags,
                    &timestamp,
                    &inputSample),
                "ReadSample(video)");

            if ((flags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0)
            {
                throw std::runtime_error("Dynamic video format change is not supported in this sample.");
            }

            if ((flags & MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED) != 0)
            {
                throw std::runtime_error("Native video format change is not supported in this sample.");
            }

            if ((flags & MF_SOURCE_READERF_STREAMTICK) != 0)
            {
                if (firstTimestamp < 0)
                {
                    firstTimestamp = timestamp;
                }

                ThrowIfFailed(
                    writer->SendStreamTick(videoStreamIndex, timestamp - firstTimestamp),
                    "SendStreamTick");
            }

            if (inputSample)
            {
                if (firstTimestamp < 0)
                {
                    firstTimestamp = timestamp;
                }

                LONGLONG duration = 0;
                if (FAILED(inputSample->GetSampleDuration(&duration)) || duration <= 0)
                {
                    duration = videoInfo.defaultFrameDuration;
                }

                CopySampleToTopDownBgra(inputSample.Get(), videoInfo, bgra);
                DrawOverlay(bgra, videoInfo.width, videoInfo.height, overlayImage);

                ComPtr<IMFSample> outputSample =
                    CreateNv12Sample(bgra, videoInfo, timestamp - firstTimestamp, duration);

                ThrowIfFailed(
                    writer->WriteSample(videoStreamIndex, outputSample.Get()),
                    "WriteSample(video)");

                ++frameCount;
            }

            if ((flags & MF_SOURCE_READERF_ENDOFSTREAM) != 0)
            {
                break;
            }
        }

        ThrowIfFailed(writer->Finalize(), "Finalize");

        std::wcout
            << L"Done. frames=" << frameCount
            << L", output=" << outputPath
            << std::endl;

        return 0;
    }
    catch (const std::exception& ex)
    {
        std::cerr << ex.what() << std::endl;
        return 1;
    }
}
