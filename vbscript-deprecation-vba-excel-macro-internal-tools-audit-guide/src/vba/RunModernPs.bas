Attribute VB_Name = "RunModernPs"
' 参照用（Excel 上で使用）: 外部 .vbs の代わりに PowerShell スクリプト（Normalize.ps1）を起動する例
' （記事「置換の最小サンプル」）
' 注意: VBA の Shell は既定で非同期です。順序制御が必要な処理は待機処理を別途検討してください。
Sub RunModernPs()

    Dim cmd As String
    cmd = "powershell.exe -NoProfile -File """ & ThisWorkbook.Path & "\Normalize.ps1""" & _
          " -InputFile """ & ThisWorkbook.Path & "\in.csv""" & _
          " -OutputFile """ & ThisWorkbook.Path & "\out.csv"""

    Shell cmd, vbNormalFocus

End Sub
