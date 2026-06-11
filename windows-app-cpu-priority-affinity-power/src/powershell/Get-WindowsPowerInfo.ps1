# CPU 情報・電源プラン・CPU 性能状態を観察する Windows 専用の関数群（記事 10 章: 現場での確認コマンド）。
# 記事のコマンドをテーマ別の関数に整理したもの:
#   Get-CimInstance Win32_Processor | Select-Object Name, NumberOfCores, ...
#   powercfg /getactivescheme
#   powercfg /q SCHEME_CURRENT SUB_PROCESSOR
#   powercfg /energy
#   Get-Counter '\Processor Information(_Total)\% Processor Performance'
#   Get-Counter '\Processor Information(_Total)\% Processor Utility'
#
# powercfg・Win32_Processor・Processor Information カウンターはいずれも Windows 専用のため、
# 各関数の先頭で $IsWindows を確認します。

function Assert-WindowsPlatform {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $FunctionName
    )

    if (-not $IsWindows) {
        throw "$FunctionName is supported only on Windows."
    }
}

# CPU 名・コア数・論理プロセッサ数・定格クロックを見る（記事 10 章「CPU情報を見る」）
function Get-CpuInfo {
    [CmdletBinding()]
    param()

    Assert-WindowsPlatform -FunctionName $MyInvocation.MyCommand.Name

    Get-CimInstance Win32_Processor |
        Select-Object Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed
}

# アクティブな電源プランを見る（記事 10 章「アクティブな電源プランを見る」）
function Get-ActivePowerScheme {
    [CmdletBinding()]
    param()

    Assert-WindowsPlatform -FunctionName $MyInvocation.MyCommand.Name

    powercfg /getactivescheme
}

# プロセッサ電源管理（PPM）の設定を見る（記事 10 章「プロセッサ電源管理の設定を見る」）。
# 最小・最大プロセッサ状態、EPP、ブースト、Core Parking 関連の設定を確認できる。
# 出力はかなり長く、環境によって見える項目は異なる。
function Get-ProcessorPowerSetting {
    [CmdletBinding()]
    param()

    Assert-WindowsPlatform -FunctionName $MyInvocation.MyCommand.Name

    powercfg /q SCHEME_CURRENT SUB_PROCESSOR
}

# 電源効率の診断レポートを作る（記事 10 章「電源効率の診断レポートを作る」）。
# 管理者権限のターミナルで実行する。一定時間の計測後、HTML レポートが出力される。
function New-PowerEnergyReport {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        # レポートの出力先（既定: カレントディレクトリの energy-report.html）
        [string] $OutputPath = 'energy-report.html'
    )

    Assert-WindowsPlatform -FunctionName $MyInvocation.MyCommand.Name

    if ($PSCmdlet.ShouldProcess($OutputPath, 'powercfg /energy')) {
        powercfg /energy /output $OutputPath
    }
}

# CPU の性能状態を Performance Counter で見る（記事 10 章「CPUの性能状態を見る」）。
# % Processor Performance と % Processor Utility をまとめて取得する。
# 環境によって利用できるカウンター名は変わる。
function Get-CpuPerformanceCounter {
    [CmdletBinding()]
    param()

    Assert-WindowsPlatform -FunctionName $MyInvocation.MyCommand.Name

    Get-Counter `
        '\Processor Information(_Total)\% Processor Performance',
        '\Processor Information(_Total)\% Processor Utility'
}
