Attribute VB_Name = "ScanProjectForVbScriptRisks"
' 参照用（Excel 上で使用）: VBA プロジェクト内の VBScript 依存をスキャンする（記事「VBAコード解析」）
' 前提:
'  - [トラスト センター] の「VBA プロジェクト オブジェクト モデルへのアクセスを信頼する」を有効化
'  - 保護されたプロジェクトは別途ソース輸出や担当者確認が必要
Sub ScanProjectForVbScriptRisks()

    Dim comp As Object
    Dim cm As Object
    Dim ws As Worksheet
    Dim nextRow As Long
    Dim patterns As Variant
    Dim p As Variant
    Dim i As Long
    Dim lineText As String

    patterns = Array( _
        "CreateObject(""VBScript.RegExp"")", _
        "VBScript.RegExp", _
        "WScript.Shell", _
        "Shell(", _
        ".vbs", _
        "wscript.exe", _
        "cscript.exe", _
        "ExecuteGlobal", _
        "Execute(" _
    )

    Set ws = ThisWorkbook.Worksheets.Add
    ws.Range("A1:D1").Value = Array("Module", "Line", "Pattern", "Code")
    nextRow = 2

    For Each comp In ThisWorkbook.VBProject.VBComponents
        Set cm = comp.CodeModule

        For i = 1 To cm.CountOfLines
            lineText = cm.Lines(i, 1)
            For Each p In patterns
                If InStr(1, lineText, CStr(p), vbTextCompare) > 0 Then
                    ws.Cells(nextRow, 1).Value = comp.Name
                    ws.Cells(nextRow, 2).Value = i
                    ws.Cells(nextRow, 3).Value = p
                    ws.Cells(nextRow, 4).Value = lineText
                    nextRow = nextRow + 1
                End If
            Next p
        Next i
    Next comp

    ws.Columns.AutoFit
    MsgBox "Scan finished: " & (nextRow - 2) & " hits"

End Sub
