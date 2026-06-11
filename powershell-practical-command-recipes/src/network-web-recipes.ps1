<#
ネットワーク・Web のレシピ集（記事 26・27 章）

Test-Connection による疎通確認、Test-NetConnection / Resolve-DnsName（Windows 固有）、
Invoke-WebRequest / Invoke-RestMethod による Web の確認をまとめて実行します。

このスクリプトは外部ネットワーク（example.com、api.github.com）へ接続するため、
ネットワークに接続できる環境で手動実行してください（Pester テストでは構文解析のみ行います）。
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# ===== 26. ネットワークを確認する =====

# 疎通確認（Windows / Linux / macOS で使える）
Test-Connection example.com -Count 4

if ($IsWindows) {
    # TCP ポートの疎通確認（Test-NetConnection は Windows 固有）
    Test-NetConnection example.com -Port 443

    # DNS の解決を確認する（Resolve-DnsName は Windows 固有）
    Resolve-DnsName example.com
}
else {
    Write-Warning 'Test-NetConnection と Resolve-DnsName は Windows 固有のためスキップしました。'
}

# ===== 27. Web の結果を見る =====

# Web ページの取得
Invoke-WebRequest https://example.com |
  Select-Object StatusCode, StatusDescription

# JSON API の結果はオブジェクトとして扱える
Invoke-RestMethod https://api.github.com/repos/PowerShell/PowerShell |
  Select-Object full_name, stargazers_count, forks_count
