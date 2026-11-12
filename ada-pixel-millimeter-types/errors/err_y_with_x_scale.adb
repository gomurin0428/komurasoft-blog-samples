with Vision_Units; use Vision_Units;
with X_Axis;
with Y_Axis;

procedure Err_Y_With_X_Scale is
   Scale_X   : constant X_Axis.Mm_Per_Px := 0.020;
   Height    : constant Y_Axis.Length := 300.0;
   Height_Mm : constant Millimeters := Height * Scale_X;
begin
   null;
end Err_Y_With_X_Scale;
