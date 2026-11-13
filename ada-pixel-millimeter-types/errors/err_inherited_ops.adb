with Vision_Units; use Vision_Units;
with X_Axis;       use X_Axis;

procedure Err_Inherited_Ops is
   Left_Edge  : constant Position := 120.25;
   Right_Edge : constant Position := 630.75;
   Width      : constant Length   := 510.0;

   Area      : constant Length      := Width ** 2;              --  px² を px として持つ
   Wrong_Pos : constant Position    := Right_Edge - Left_Edge;  --  長さを座標として持つ
   Mm2       : constant Millimeters := Millimeters'(3.0) ** 2;  --  mm² を mm として持つ
begin
   null;
end Err_Inherited_Ops;
