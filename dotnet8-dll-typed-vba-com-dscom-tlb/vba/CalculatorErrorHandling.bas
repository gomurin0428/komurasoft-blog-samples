Attribute VB_Name = "CalculatorErrorHandling"
' 参照用（Excel / Access の VBA で使用）: .NET 側の例外を COM エラーとして受ける例（記事 8.1）
'
' .NET 側で例外が投げられると、VBA 側では COM エラー（Err.Number / Err.Description）として見えます。
Option Explicit

Public Sub UseCalculatorWithErrorHandling()
    On Error GoTo EH

    Dim calc As VbaTypedComSample.ICalculator
    Set calc = New VbaTypedComSample.Calculator

    Debug.Print calc.Divide(10, 0)
    Exit Sub

EH:
    Debug.Print Err.Number
    Debug.Print Err.Description
End Sub
