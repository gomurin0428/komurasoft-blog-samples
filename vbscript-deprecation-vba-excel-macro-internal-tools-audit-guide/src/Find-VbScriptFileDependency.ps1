# 端末・共有フォルダ上のスクリプト依存をざっくり洗う（記事「ファイル検索と構成情報の洗い出し」）
#
# 記事では Windows の C:\Users / C:\ProgramData / C:\Scripts を対象にしています。
# このサンプルでは対象パスと出力先を引数で差し替えられるようにし、
# 検出結果をパイプラインにも出力します（テスト・後続処理用）。
param(
    # 記事の既定値。実環境に合わせて差し替えてください（Windows のパス例）。
    [string[]] $Paths = @("C:\Users", "C:\ProgramData", "C:\Scripts"),

    [string] $OutputCsv = ".\vbscript-dependency-hits.csv"
)

$patterns = @(
  'wscript\.exe',
  'cscript\.exe',
  '\.vbs(\s|$)',
  'VBScript\.RegExp',
  'WScript\.Shell',
  'CreateObject\("VBScript\.RegExp"\)',
  'ExecuteGlobal'
)

$hits = foreach ($path in $Paths) {
  if (Test-Path $path) {
    Get-ChildItem -Path $path -Recurse -File `
      -Include *.vbs,*.ps1,*.bat,*.cmd,*.wsf,*.hta,*.txt `
      -ErrorAction SilentlyContinue |
      Select-String -Pattern $patterns -AllMatches |
      Select-Object Path, LineNumber, Line
  }
}

$hits | Export-Csv $OutputCsv -NoTypeInformation -Encoding UTF8
$hits
