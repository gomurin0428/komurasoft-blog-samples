with Ada.Text_IO;  use Ada.Text_IO;
with Vision_Units; use Vision_Units;
with X_Axis;
with Y_Axis;
with Tolerance;    use Tolerance;

procedure Hole_Literals is
   use type X_Axis.Position, X_Axis.Length, Y_Axis.Length;
   Left_Edge  : constant X_Axis.Position := 120.25;
   Right_Edge : constant X_Axis.Position := 630.75;
   Width_Px   : constant X_Axis.Length   := Right_Edge - Left_Edge;
   Height_Px  : constant Y_Axis.Length   := 300.0;
   --  型のないリテラルは、式が解決できる型になる
   W_Mm : constant Millimeters   := Width_Px * 2.0;          --  2.0 が X_Axis.Mm_Per_Px になる
   L_Px : constant X_Axis.Length := Millimeters'(1.0) * 50.0; --  50.0 が X_Axis.Px_Per_Mm になる
   H_Mm : constant Millimeters   := Height_Px * 0.02;        --  0.02 が Y_Axis.Mm_Per_Px になる
   Ok   : constant Boolean := Accept_Rounded (W_Mm, (5.0, 0.1, 0.1));
begin
   Put_Line (Millimeters'Image (W_Mm) & X_Axis.Length'Image (L_Px)
             & Millimeters'Image (H_Mm) & " " & Boolean'Image (Ok));
end Hole_Literals;
