with X_Axis;    use X_Axis;
with Tolerance; use Tolerance;

procedure Hole_Px is
   Width_Px : constant Length := 510.0;
   Ok : constant Boolean := Accept_Rounded (Width_Px, (5.0, 0.1, 0.1));
begin
   null;
end Hole_Px;
