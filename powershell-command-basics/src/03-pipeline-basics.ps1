#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 6〜7 章のコード集。
    パイプライン（Where-Object / Sort-Object / Select-Object）と、
    Format 系コマンドを最後に使う理由を確認します。

    実行例:
        pwsh -NoProfile -File ./src/03-pipeline-basics.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/03-pipeline')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 6 章: パイプラインの基本 ---

    # プロセス一覧を取得 → CPU 使用量で降順に並べる → 上位10件だけ表示
    Get-Process | Sort-Object CPU -Descending | Select-Object -First 10

    # --- Where-Object ── 条件で絞る ---

    # 停止中のサービスだけ表示（Get-Service は Windows 専用）
    if ($IsWindows) {
        Get-Service | Where-Object { $_.Status -eq "Stopped" }

        # PowerShell 3.0 以降の簡略構文
        Get-Service | Where-Object Status -EQ "Stopped"
    }

    # 100MB を超えるファイルだけ表示（このワークスペースには無いので何も出ない）
    Get-ChildItem -File |
      Where-Object { $_.Length -gt 100MB }

    # 名前に backup を含むファイルだけ表示
    Get-ChildItem -File |
      Where-Object { $_.Name -like "*backup*" }

    # --- Sort-Object ── 並べる ---

    # メモリ使用量の大きい順
    Get-Process |
      Sort-Object WorkingSet -Descending |
      Select-Object -First 10 Name, Id, WorkingSet

    # 更新日時の新しい順
    Get-ChildItem -File |
      Sort-Object LastWriteTime -Descending |
      Select-Object -First 20 Name, LastWriteTime

    # --- Select-Object ── 必要な列だけ選ぶ ---

    Get-Process |
      Select-Object Name, Id, CPU, WorkingSet

    # 件数を絞る
    Get-Process | Select-Object -First 5
    Get-Process | Select-Object -Last 5

    # CSV に出す前は、Select-Object で必要な列だけに整理する
    if ($IsWindows) {
        Get-Service |
          Select-Object Name, DisplayName, Status, StartType |
          Export-Csv ./services.csv -NoTypeInformation -Encoding UTF8
    }

    # --- 記事 7 章: Format 系コマンドは最後に使う ---

    # Format-Table / Format-List / Format-Wide は「画面表示用」
    Get-Process | Select-Object -First 5 | Format-Table Name, CPU

    # 避ける: CSV に表示用情報が混ざる
    # Get-Process |
    #   Format-Table Name, CPU |
    #   Export-Csv ./process.csv -NoTypeInformation

    # 正しい: 先にプロパティを選んで CSV 出力
    Get-Process |
      Select-Object Name, CPU |
      Export-Csv ./process.csv -NoTypeInformation -Encoding UTF8
}
finally {
    Pop-Location
}
