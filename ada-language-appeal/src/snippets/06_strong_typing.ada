--  記事 6 章「強い型付け ── 単位の取り違えをコンパイルエラーにする」のコード断片。
--
--  構造がまったく同じでも、別の名前で宣言した型は別の型。
--  MetersとSecondsはどちらも実体はFloatだが、混ぜて使うと
--  コンパイルエラーになる。変換は明示的に書かせる。

with Ada.Text_IO;

procedure Strong_Typing_Demo is

   type Meters  is new Float;
   type Seconds is new Float;

   Distance : Meters  := 100.0;
   Time     : constant Seconds := 9.58;

   --  以下はどちらもコンパイルエラーになる
   --  Distance := Time;
   --  Distance := Distance + Time;

   --  意図的に変換する場合だけ、明示的に書く
   Speed : constant Float := Float (Distance) / Float (Time);

begin
   Distance := Distance + 1.0;  --  同じ型同士の演算は通常通り書ける
   Ada.Text_IO.Put_Line ("speed:" & Float'Image (Speed));
   Ada.Text_IO.Put_Line ("distance:" & Meters'Image (Distance));
end Strong_Typing_Demo;
