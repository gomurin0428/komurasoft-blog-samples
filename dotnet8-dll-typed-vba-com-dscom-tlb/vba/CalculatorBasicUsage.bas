Attribute VB_Name = "CalculatorBasicUsage"
' 参照用（Excel / Access の VBA で使用）: 型付きで Calculator を使う例（記事 8 章）
'
' 事前準備:
'   1. VBA エディタの ツール -> 参照設定 で VbaTypedComSample.tlb への参照を追加する
'   2. comhost と TLB が登録済みであること（scripts\Register-ComServer.ps1）
Option Explicit

Public Sub UseCalculator()
    Dim calc As VbaTypedComSample.ICalculator
    Set calc = New VbaTypedComSample.Calculator

    Debug.Print calc.Add(10, 20)
    Debug.Print calc.Divide(10, 4)
    Debug.Print calc.Hello("VBA")
End Sub
