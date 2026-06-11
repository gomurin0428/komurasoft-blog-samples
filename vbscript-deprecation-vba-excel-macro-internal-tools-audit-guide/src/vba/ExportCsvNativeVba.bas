Attribute VB_Name = "ExportCsvNativeVba"
' 参照用（Excel 上で使用）: 外部 .vbs を呼ばず VBA ネイティブで CSV を書き出す例（記事「置換の最小サンプル」）
Sub ExportCsvNativeVba()

    Dim f As Integer
    Dim outPath As String

    outPath = ThisWorkbook.Path & "\out.csv"
    f = FreeFile

    Open outPath For Output As #f
    Print #f, "Code,Name"
    Print #f, "1001,Tokyo"
    Print #f, "1002,Osaka"
    Close #f

    MsgBox "CSV exported: " & outPath

End Sub
