# Sysmon / AppLocker のイベントログから VBScript 実行痕跡を確認する（記事「ログ分析」）
#
# Windows 専用: Get-WinEvent で以下のログを参照します。
#   - Microsoft-Windows-Sysmon/Operational     （Sysmon 導入済み端末。Event ID 7 = Image Load）
#   - Microsoft-Windows-AppLocker/MSI and Script（AppLocker / App Control の監査・許可イベント）
# Linux / macOS では実行できないため、$IsWindows でガードしています。
param(
    [int] $MaxEvents = 2000
)

if (-not $IsWindows) {
    Write-Warning "このスクリプトは Windows 専用です（Windows イベントログを参照します）。"
    return
}

# Sysmon: vbscript.dll を読み込んだプロセスを確認
Get-WinEvent -LogName "Microsoft-Windows-Sysmon/Operational" -MaxEvents $MaxEvents |
  Where-Object { $_.Id -eq 7 -and $_.Message -match 'vbscript\.dll' } |
  Select-Object TimeCreated, MachineName, Message

# AppLocker / App Control: スクリプト・MSI 関連の監査ログを確認
Get-WinEvent -LogName "Microsoft-Windows-AppLocker/MSI and Script" -MaxEvents $MaxEvents |
  Where-Object { $_.Id -in 8005, 8006 } |
  Select-Object TimeCreated, Id, Message
