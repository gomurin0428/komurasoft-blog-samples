with Ada.Text_IO; use Ada.Text_IO;

procedure Small_Check is
   --  delta だけを書くと、small は delta 以下の2のべき乗になる
   type Mm_Binary is delta 0.001 range -1000.0 .. 1000.0;

   --  small を指定すると、0.001 の倍数をそのまま表せる
   type Mm_Exact is delta 0.001 range -1000.0 .. 1000.0
     with Small => 0.001;

   A : constant Mm_Binary := 0.1;
   B : constant Mm_Exact  := 0.1;
begin
   Put_Line ("Mm_Binary'Small =" & Long_Float'Image (Mm_Binary'Small));
   Put_Line ("Mm_Exact'Small  =" & Long_Float'Image (Mm_Exact'Small));
   Put_Line ("Mm_Binary 0.1   =" & Long_Float'Image (Long_Float (A)));
   Put_Line ("Mm_Exact  0.1   =" & Long_Float'Image (Long_Float (B)));
end Small_Check;
