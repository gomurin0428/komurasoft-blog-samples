--  文字列を連結して返す関数は二次スタックを使うので、
--  gnat.adc の No_Secondary_Stack を付けるとコンパイルエラーになるはず。
function Nss_Violation (Name : String) return String is
begin
   return "inspect: " & Name;
end Nss_Violation;
