// AnalyzerWrapper.h
#pragma once
#include "NativeLib.hpp"

using namespace System;
using namespace System::Collections::Generic;

public ref class AnalysisOptions
{
public:
    property int Threshold;
    property String^ ModelPath;
};

public ref class AnalysisResult
{
public:
    property bool Ok;
    property String^ Message;
    property List<int>^ Scores;
};

public ref class AnalyzerWrapper : IDisposable
{
public:
    AnalyzerWrapper(String^ licensePath);
    ~AnalyzerWrapper();
    !AnalyzerWrapper();

    AnalysisResult^ Analyze(String^ imagePath, AnalysisOptions^ options);

private:
    NativeLib::Analyzer* _native;
};
