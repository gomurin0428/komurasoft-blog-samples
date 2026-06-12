--  記事 10 章「レコードと判別子」のコード断片。
--
--  レコードはフィールドにデフォルト値を持たせられ、
--  集成体(aggregate)で名前付き初期化ができる。
--  判別子(discriminant)はレコードの「形」を決めるパラメーターで、
--  内部配列のサイズが宣言時に決まり、以後変わらない。
--  サイズと実体の不整合という、Cでよくあるバグの入り口が
--  最初から存在しない。

with Ada.Text_IO;

procedure Records_And_Discriminants_Demo is

   use Ada.Text_IO;

   type Point is record
      X : Float := 0.0;
      Y : Float := 0.0;
   end record;

   P : constant Point := (X => 1.0, Y => 2.0);

   --  判別子付きレコード
   type Buffer (Size : Positive) is record
      Data   : String (1 .. Size);
      Length : Natural := 0;
   end record;

   Small : Buffer (Size => 16);
   Large : Buffer (Size => 4096);

begin
   Put_Line ("point:" & Float'Image (P.X) & Float'Image (P.Y));

   Small.Data (1 .. 5) := "hello";
   Small.Length := 5;

   Put_Line ("small buffer size:" & Positive'Image (Small.Size));
   Put_Line ("large buffer size:" & Positive'Image (Large.Size));
   Put_Line ("small content: " & Small.Data (1 .. Small.Length));
end Records_And_Discriminants_Demo;
