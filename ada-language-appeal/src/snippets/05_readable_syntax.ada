--  記事 5 章「読みやすさを重視した構文」のコード断片。
--
--  for ループ、if / elsif、case 文、名前付き引数関連付けの例。
--  case 文はすべての値を網羅しないとコンパイルエラーになり、
--  C 系言語のような暗黙のフォールスルーは存在しない。
--  列挙型に値を追加すると、網羅していない case 文がすべて
--  コンパイルエラーになるため、仕様変更の影響箇所をコンパイラが
--  列挙してくれる。

with Ada.Text_IO;

procedure Readable_Syntax_Demo is

   use Ada.Text_IO;

   type Day is (Mon, Tue, Wed, Thu, Fri, Sat, Sun);

   Today : constant Day := Wed;

   Temperature : constant Float := 50.0;

   procedure Start_Cooling is
   begin
      Put_Line ("cooling...");
   end Start_Cooling;

   procedure Start_Heating is
   begin
      Put_Line ("heating...");
   end Start_Heating;

   procedure Keep_Current_State is
   begin
      Put_Line ("keep current state");
   end Keep_Current_State;

   procedure Draw_Rectangle (Left, Top, Width, Height : Integer) is
   begin
      Put_Line ("rect:"
                & Integer'Image (Left) & Integer'Image (Top)
                & Integer'Image (Width) & Integer'Image (Height));
   end Draw_Rectangle;

begin
   --  forループ。I は宣言不要で、ループ内では定数として扱われる
   for I in 1 .. 5 loop
      Put_Line (Integer'Image (I * I));
   end loop;

   --  if / elsif / else
   if Temperature > 80.0 then
      Start_Cooling;
   elsif Temperature < 20.0 then
      Start_Heating;
   else
      Keep_Current_State;
   end if;

   --  case文。Dayのすべての値を網羅しないとコンパイルエラー
   case Today is
      when Mon .. Fri =>
         Put_Line ("Weekday");
      when Sat | Sun =>
         Put_Line ("Weekend");
   end case;

   --  名前付き引数関連付け。引数の取り違えを防ぎ、呼び出しが
   --  そのままドキュメントになる
   Draw_Rectangle (Left => 10, Top => 20, Width => 100, Height => 50);
end Readable_Syntax_Demo;
