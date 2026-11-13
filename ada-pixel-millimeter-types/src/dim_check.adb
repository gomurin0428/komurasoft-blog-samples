with Ada.Text_IO; use Ada.Text_IO;
with System.Dim.Float_IO;
with Vision_Dims; use Vision_Dims;

procedure Dim_Check is
   package Vision_IO is new System.Dim.Float_IO (Vision_Type);
   use Vision_IO;

   Scale_X  : constant X_Scale   := 10.0 * mm / (500.0 * pxX);
   Width    : constant X_Length  := 510.5 * pxX;
   Width_Mm : constant Mm_Length := Width * Scale_X;
begin
   Put (Scale_X, Aft => 4, Exp => 0);
   New_Line;
   Put (Width_Mm, Aft => 3, Exp => 0);
   New_Line;
end Dim_Check;
