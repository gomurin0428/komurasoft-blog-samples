# Excel（VBA の RunModernPs）から呼び出される CSV 正規化スクリプト（記事「置換の最小サンプル」）
# 外部 .vbs で行っていた CSV 整形を PowerShell へ置き換えた例です。
param(
  [string]$InputFile,
  [string]$OutputFile
)

Import-Csv $InputFile |
  Sort-Object Code |
  Export-Csv $OutputFile -NoTypeInformation -Encoding UTF8
