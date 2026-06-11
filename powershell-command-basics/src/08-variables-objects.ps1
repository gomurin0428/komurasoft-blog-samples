#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 14〜15 章のコード集。
    変数・配列・ハッシュテーブル（スプラッティング）と PSCustomObject を確認します。

    実行例:
        pwsh -NoProfile -File ./src/08-variables-objects.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/08-variables')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 14 章: 変数 ---

    # 記事は $path = "C:\Logs"。クロスプラットフォーム実行のためワークスペース内に調整
    $path = "./logs"
    Get-ChildItem $path

    # PowerShell の変数は $ で始まる
    $today = Get-Date
    $limit = (Get-Date).AddDays(-30)
    $today
    $limit

    # --- 記事 14 章: 配列 ---

    $extensions = @("*.log", "*.txt", "*.csv")

    foreach ($ext in $extensions) {
      Get-ChildItem ./logs -Filter $ext
    }

    # --- 記事 14 章: ハッシュテーブル（スプラッティング） ---

    $params = @{
      Path = "./logs"
      Filter = "*.log"
      Recurse = $true
    }

    Get-ChildItem @params

    # --- 記事 15 章: PSCustomObject で結果を整える ---

    # ※ $env:COMPUTERNAME / $env:USERNAME は Windows の環境変数。
    #    Windows 以外では空欄になる（エラーにはならない）
    [pscustomobject]@{
      ComputerName = $env:COMPUTERNAME
      UserName     = $env:USERNAME
      CheckedAt    = Get-Date
    }

    # 複数の結果を作って CSV にする
    Get-ChildItem ./logs -Filter *.log |
      ForEach-Object {
        [pscustomobject]@{
          Name          = $_.Name
          FullName      = $_.FullName
          SizeMB        = [math]::Round($_.Length / 1MB, 2)
          LastWriteTime = $_.LastWriteTime
        }
      } |
      Export-Csv ./log-files.csv -NoTypeInformation -Encoding UTF8
}
finally {
    Pop-Location
}
