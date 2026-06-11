// EcoQoS の有効化・無効化（記事 8 章）── 参照用コード
//
// このファイルは Win32 API（SetThreadInformation / ThreadPowerThrottling）を使う
// Windows 専用のコードであり、このリポジトリではビルド対象外です。
// Windows 上で Visual Studio などの C++ プロジェクトに取り込んで使用してください。
//
// EcoQoS は「この処理は性能が最重要ではないので、省電力寄りに扱ってよい」と
// OS へ伝える仕組みです。バックグラウンドの同期、低優先のインデックス作成、
// 急がないログ集計などに向きます。UI 操作に直結する処理、カメラ取り込み、
// 制御周期に関わる処理などには慎重になるべきです（記事 8 章）。
//
// 参考: https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setthreadinformation

#include <windows.h>

// 現在のスレッドを EcoQoS にする
void EnableEcoQoSForCurrentThread()
{
    THREAD_POWER_THROTTLING_STATE powerThrottling = {};
    powerThrottling.Version = THREAD_POWER_THROTTLING_CURRENT_VERSION;
    powerThrottling.ControlMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED;
    powerThrottling.StateMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED;

    SetThreadInformation(
        GetCurrentThread(),
        ThreadPowerThrottling,
        &powerThrottling,
        sizeof(powerThrottling));
}

// 性能重視に戻す（同じ制御対象に対して StateMask を 0 にする）
void DisableEcoQoSForCurrentThread()
{
    THREAD_POWER_THROTTLING_STATE powerThrottling = {};
    powerThrottling.Version = THREAD_POWER_THROTTLING_CURRENT_VERSION;
    powerThrottling.ControlMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED;
    powerThrottling.StateMask = 0;

    SetThreadInformation(
        GetCurrentThread(),
        ThreadPowerThrottling,
        &powerThrottling,
        sizeof(powerThrottling));
}
