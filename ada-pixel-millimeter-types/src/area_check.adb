with Ada.Text_IO; use Ada.Text_IO;
with X_Axis;      use X_Axis;

--  長さ × 長さを面積の型として返す演算子を、別に定義できることを確かめる
procedure Area_Check is
   type Px_Area is new Long_Float;

   function "*" (Left, Right : Length) return Px_Area is
     (Px_Area (Long_Float (Left) * Long_Float (Right)));

   Width  : constant Length  := 510.0;
   Height : constant Length  := 300.0;
   Area   : constant Px_Area := Width * Height;
begin
   Put_Line (Px_Area'Image (Area));
end Area_Check;
