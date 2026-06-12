--  記事 7 章「範囲制約 ── 不正な値をデータ型のレベルで防ぐ」のコード断片。
--
--  型に値の範囲を持たせると、範囲外の値の代入は実行時に
--  Constraint_Error 例外になる(コンパイル時に分かる違反は
--  コンパイル時に検出される)。
--  「0〜100のはずの値」という暗黙の前提を、コメントではなく
--  型で表現できる。

with Ada.Text_IO;

procedure Range_Constraints_Demo is

   use Ada.Text_IO;

   subtype Percentage is Integer range 0 .. 100;

   Progress : Percentage := 50;

   --  Ada 2012以降は任意の条件を述語として付けられる
   subtype Even is Integer
     with Dynamic_Predicate => Even mod 2 = 0;

   E : Even := 4;

   --  固定小数点型。ハードウェア制御向けの型も言語に組み込まれている
   type Temperature is delta 0.1 range -50.0 .. 150.0;

   Current : constant Temperature := 23.5;

   Invalid_Input : constant Integer := 120;

begin
   Put_Line ("progress:" & Integer'Image (Progress));
   Put_Line ("even:" & Integer'Image (E));
   Put_Line ("temperature:" & Temperature'Image (Current));

   E := E + 2;

   --  範囲外の値を入れようとすると実行時にConstraint_Error
   Progress := Invalid_Input;
   Put_Line ("ここには到達しない");
exception
   when Constraint_Error =>
      Put_Line ("Constraint_Error: 範囲制約違反を検出した");
end Range_Constraints_Demo;
