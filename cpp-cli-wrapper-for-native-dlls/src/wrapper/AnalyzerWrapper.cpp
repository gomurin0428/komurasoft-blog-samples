// AnalyzerWrapper.cpp
#include "AnalyzerWrapper.h"
#include <msclr/marshal_cppstd.h>

using msclr::interop::marshal_as;

AnalyzerWrapper::AnalyzerWrapper(String^ licensePath)
{
    _native = new NativeLib::Analyzer(marshal_as<std::wstring>(licensePath));
}

AnalyzerWrapper::~AnalyzerWrapper()
{
    this->!AnalyzerWrapper();
}

AnalyzerWrapper::!AnalyzerWrapper()
{
    delete _native;
    _native = nullptr;
}

AnalysisResult^ AnalyzerWrapper::Analyze(String^ imagePath, AnalysisOptions^ options)
{
    NativeLib::AnalyzeOptions nativeOptions{};
    nativeOptions.threshold = options->Threshold;
    nativeOptions.modelPath = marshal_as<std::wstring>(options->ModelPath);

    try
    {
        auto nativeResult = _native->Analyze(
            marshal_as<std::wstring>(imagePath),
            nativeOptions);

        auto managed = gcnew AnalysisResult();
        managed->Ok = nativeResult.ok;
        managed->Message = gcnew String(nativeResult.message.c_str());
        managed->Scores = gcnew List<int>();

        for (int score : nativeResult.scores)
        {
            managed->Scores->Add(score);
        }

        return managed;
    }
    catch (const std::exception& ex)
    {
        throw gcnew InvalidOperationException(gcnew String(ex.what()));
    }
}
