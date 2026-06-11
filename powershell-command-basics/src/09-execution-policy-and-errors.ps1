#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 17〜18 章のコード集。
    実行ポリシーの確認と、try/catch によるエラー処理を確認します。

    実行例:
        pwsh -NoProfile -File ./src/09-execution-policy-and-errors.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/09-errors')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 17 章: 実行ポリシーの基本 ---

    # 現在の実行ポリシーを確認する
    # （実行ポリシーが意味を持つのは Windows。Windows 以外では常に Unrestricted 扱い）
    Get-ExecutionPolicy
    Get-ExecutionPolicy -List

    # 個人の開発端末で、現在ユーザーのスコープを RemoteSigned にする例
    # （環境設定を変更し、Windows 以外ではサポートされないためコメントにしている）
    # Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

    # --- 記事 18 章: エラー処理の基本 ---

    # 重要な処理では -ErrorAction Stop と try/catch を使う
    "source content" | Set-Content ./source.txt -Encoding UTF8
    New-Item -ItemType Directory -Path ./backup -Force | Out-Null

    try {
      Copy-Item ./source.txt ./backup/source.txt -ErrorAction Stop
      Write-Host "Copy succeeded"
    }
    catch {
      Write-Error "Copy failed: $($_.Exception.Message)"
    }

    # 複数のファイルを処理する場合も、失敗を記録すると後で追跡できる
    $results = foreach ($file in Get-ChildItem ./input -File) {
      try {
        Copy-Item $file.FullName ./backup -ErrorAction Stop

        [pscustomobject]@{
          FileName = $file.Name
          Status   = "OK"
          Message  = ""
        }
      }
      catch {
        [pscustomobject]@{
          FileName = $file.Name
          Status   = "NG"
          Message  = $_.Exception.Message
        }
      }
    }

    $results | Export-Csv ./copy-result.csv -NoTypeInformation -Encoding UTF8
}
finally {
    Pop-Location
}
