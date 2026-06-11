function New-SampleWorkspace {
    <#
        各テーマ別スクリプトが使う練習用ワークスペースを作成します。
        記事の例に登場する C:\Logs や C:\Temp、C:\Work\Reports の代わりに、
        ここで作るフォルダー（logs / temp / reports / input）を使います。
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null

    # logs: ログ調査用（ERROR / WARN を含む .log と、古い .log / .tmp）
    $logs = Join-Path $Path 'logs'
    New-Item -ItemType Directory -Path $logs | Out-Null

    @(
        '2026-05-01 10:00:00 INFO  application started'
        '2026-05-01 10:05:00 WARN  disk usage 85%'
        '2026-05-01 10:10:00 ERROR failed to connect to database'
        '2026-05-01 10:15:00 INFO  retrying'
        '2026-05-01 10:20:00 ERROR timeout while reading response'
    ) | Set-Content -LiteralPath (Join-Path $logs 'app.log') -Encoding UTF8

    @(
        '2026-05-02 02:00:00 INFO  nightly batch started'
        '2026-05-02 02:30:00 ERROR export step failed'
    ) | Set-Content -LiteralPath (Join-Path $logs 'batch.log') -Encoding UTF8

    'temporary cache' | Set-Content -LiteralPath (Join-Path $logs 'cache.tmp') -Encoding UTF8

    (Get-Item -LiteralPath (Join-Path $logs 'batch.log')).LastWriteTime = (Get-Date).AddDays(-60)
    (Get-Item -LiteralPath (Join-Path $logs 'cache.tmp')).LastWriteTime = (Get-Date).AddDays(-45)

    # temp: 古い .tmp の削除手順用（記事 19 章）
    $temp = Join-Path $Path 'temp'
    New-Item -ItemType Directory -Path $temp | Out-Null
    'old temp 1' | Set-Content -LiteralPath (Join-Path $temp 'old1.tmp') -Encoding UTF8
    'old temp 2' | Set-Content -LiteralPath (Join-Path $temp 'old2.tmp') -Encoding UTF8
    'keep this' | Set-Content -LiteralPath (Join-Path $temp 'recent.tmp') -Encoding UTF8
    (Get-Item -LiteralPath (Join-Path $temp 'old1.tmp')).LastWriteTime = (Get-Date).AddDays(-40)
    (Get-Item -LiteralPath (Join-Path $temp 'old2.tmp')).LastWriteTime = (Get-Date).AddDays(-90)

    # reports: アーカイブ手順用（記事 22 章）
    $reports = Join-Path $Path 'reports'
    New-Item -ItemType Directory -Path $reports | Out-Null
    'dummy report 2025' | Set-Content -LiteralPath (Join-Path $reports 'sales-2025.xlsx') -Encoding UTF8
    'dummy report 2026' | Set-Content -LiteralPath (Join-Path $reports 'sales-2026.xlsx') -Encoding UTF8
    (Get-Item -LiteralPath (Join-Path $reports 'sales-2025.xlsx')).LastWriteTime = (Get-Date).AddDays(-120)

    # input: エラー処理サンプル用（記事 18 章）
    $inputDir = Join-Path $Path 'input'
    New-Item -ItemType Directory -Path $inputDir | Out-Null
    'input a' | Set-Content -LiteralPath (Join-Path $inputDir 'a.txt') -Encoding UTF8
    'input b' | Set-Content -LiteralPath (Join-Path $inputDir 'b.txt') -Encoding UTF8

    # users.csv（記事 10 章の例と同じ内容）
    @(
        'Name,Department,Enabled'
        'Suzuki,Sales,true'
        'Tanaka,Accounting,false'
        'Sato,Sales,true'
    ) | Set-Content -LiteralPath (Join-Path $Path 'users.csv') -Encoding UTF8

    # settings.json（記事 11 章）
    '{ "Name": "BatchJob", "Enabled": true, "Retry": 3 }' |
        Set-Content -LiteralPath (Join-Path $Path 'settings.json') -Encoding UTF8

    # Where-Object -like の例で引っかかるファイル（記事 6 章）
    'backup data' | Set-Content -LiteralPath (Join-Path $Path 'report-backup.txt') -Encoding UTF8

    Get-Item -LiteralPath $Path
}
