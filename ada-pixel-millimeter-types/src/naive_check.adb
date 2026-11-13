with Ada.Text_IO; use Ada.Text_IO;

procedure Naive_Check is
   type Px_Length is new Long_Float;
   type Mm_Length is new Long_Float;

   Width : constant Px_Length := 510.0;
   Area  : constant Px_Length := Width * Width;  --  px × px が px になる
   Scale : constant Px_Length := 0.01;           --  mm/px のつもりの値
   Wrong : constant Mm_Length := Mm_Length (Width * Scale);
begin
   Put_Line (Px_Length'Image (Area));
   Put_Line (Mm_Length'Image (Wrong));
end Naive_Check;
