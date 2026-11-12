with Vision_Units; use Vision_Units;
with X_Axis;       use X_Axis;

procedure Err_Position_As_Length is
   Scale      : constant Mm_Per_Px := 0.020;
   Right_Edge : constant Position := 630.75;
   Width_Mm   : constant Millimeters := Right_Edge * Scale;
begin
   null;
end Err_Position_As_Length;
