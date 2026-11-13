with Vision_Units; use Vision_Units;
with X_Axis;       use X_Axis;

procedure Err_Inverse_Scale is
   Scale    : constant Px_Per_Mm := 50.0;
   Width_Px : constant Length := 510.0;
   Width_Mm : constant Millimeters := Width_Px * Scale;
begin
   null;
end Err_Inverse_Scale;
