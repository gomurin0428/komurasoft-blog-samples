--  記事 11 章「ジェネリクス」のコード断片。
--
--  Adaは1983年の最初の標準からジェネリクスを備えていた。
--  特徴は「要求する操作を明示する」こと。with function "<" のように
--  仕様に書くため、C++テンプレートの「インスタンス化して初めて
--  エラーが分かる」問題が最初から起きない。

with Ada.Text_IO;

procedure Generics_Demo is

   use Ada.Text_IO;

   generic
      type Element is private;
   procedure Swap (Left, Right : in out Element);

   procedure Swap (Left, Right : in out Element) is
      Temp : constant Element := Left;
   begin
      Left  := Right;
      Right := Temp;
   end Swap;

   --  要求する操作("<")を仕様に明示するジェネリック関数。
   --  is <> は「見えている同名の演算子をデフォルトで使う」の意味
   generic
      type Element is private;
      with function "<" (Left, Right : Element) return Boolean is <>;
   function Max (Left, Right : Element) return Element;

   function Max (Left, Right : Element) return Element is
   begin
      if Left < Right then
         return Right;
      else
         return Left;
      end if;
   end Max;

   --  具体的な型でインスタンス化する
   procedure Swap_Integers is new Swap (Element => Integer);
   procedure Swap_Floats   is new Swap (Element => Float);

   function Max_Integer is new Max (Element => Integer);

   A : Integer := 1;
   B : Integer := 2;

   X : Float := 1.5;
   Y : Float := 2.5;

begin
   Swap_Integers (A, B);
   Swap_Floats (X, Y);

   Put_Line ("a:" & Integer'Image (A) & "  b:" & Integer'Image (B));
   Put_Line ("x:" & Float'Image (X) & "  y:" & Float'Image (Y));
   Put_Line ("max:" & Integer'Image (Max_Integer (10, 20)));
end Generics_Demo;
