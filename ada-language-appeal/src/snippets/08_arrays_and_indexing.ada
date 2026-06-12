--  記事 8 章「配列と添字 ── 境界チェックと列挙型添字」のコード断片。
--
--  Adaの配列は添字の型を自由に選べる。列挙型を添字にすれば
--  「数値の添字が何を意味するのか」を覚えておく必要がない。
--  配列アクセスは常に境界チェックされ、範囲外アクセスは
--  未定義動作ではなく定義された例外(Constraint_Error)になる。

with Ada.Text_IO;

procedure Arrays_And_Indexing_Demo is

   use Ada.Text_IO;

   type Day is (Mon, Tue, Wed, Thu, Fri, Sat, Sun);

   type Hours_Array is array (Day) of Natural;

   Work_Hours : constant Hours_Array := (Mon .. Fri => 8, others => 0);

   Buffer : String (1 .. 10) := (others => ' ');

   Index : constant Integer := 11;

begin
   --  'Range属性で配列の境界に沿ってループする。
   --  境界をハードコードしないので、サイズ変更がループに波及しない
   for D in Work_Hours'Range loop
      Put_Line (Day'Image (D) & ":" & Natural'Image (Work_Hours (D)));
   end loop;

   --  範囲外アクセスは実行時にConstraint_Error。
   --  メモリを黙って破壊せず、問題の発生地点で止まる
   Buffer (Index) := 'x';
   Put_Line ("ここには到達しない");
exception
   when Constraint_Error =>
      Put_Line ("Constraint_Error: 配列の境界違反を検出した");
end Arrays_And_Indexing_Demo;
