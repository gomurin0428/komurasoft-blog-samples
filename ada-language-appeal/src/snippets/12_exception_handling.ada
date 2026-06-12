--  記事 12 章「例外処理」のコード断片。
--
--  ブロックの最後に exception 部を書き、例外の種類ごとに
--  ハンドラーを並べる。範囲制約や境界チェックの違反は
--  Constraint_Error としてこの例外機構に統合されている。
--  つまり、型に書いた制約は「自動生成される実行時アサーション」
--  として機能する。

with Ada.Text_IO;
with Ada.Exceptions;

procedure Exception_Handling_Demo is

   use Ada.Text_IO;

   procedure Load_File (Path : String) is
      Input : File_Type;
   begin
      Open (Input, In_File, Path);
      Put_Line ("opened: " & Path);
      Close (Input);
   end Load_File;

begin
   Load_File ("config.txt");
exception
   when Ada.Text_IO.Name_Error =>
      Put_Line ("設定ファイルが見つかりません");
   when E : others =>
      --  例外オブジェクトから名前やメッセージを取得できる
      Put_Line (Ada.Exceptions.Exception_Information (E));
      raise;
end Exception_Handling_Demo;
