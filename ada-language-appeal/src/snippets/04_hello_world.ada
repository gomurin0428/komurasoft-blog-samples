--  記事 4 章「まずはHello, World」のコード断片。
--
--  with でライブラリユニットを取り込み、手続き(procedure)が
--  プログラムの本体になる。end の後に名前を繰り返すのが Ada らしい
--  ところで、ブロックの閉じ間違いはコンパイルエラーになる。

with Ada.Text_IO;

procedure Hello is
begin
   Ada.Text_IO.Put_Line ("Hello, Ada!");
end Hello;
