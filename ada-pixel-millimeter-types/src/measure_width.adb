with Ada.Text_IO;  use Ada.Text_IO;
with Vision_Units; use Vision_Units;
with X_Axis;       use X_Axis;

procedure Measure_Width is
   --  10 mm のゲージを写すと、x 方向に 500 px だった
   Gauge_Mm : constant Millimeters := 10.0;
   Gauge_Px : constant X_Axis.Length := 500.0;
   Scale    : constant Mm_Per_Px := Gauge_Mm / Gauge_Px;

   --  部品の左右のエッジの座標
   Left_Edge  : constant Position := 120.25;
   Right_Edge : constant Position := 630.75;

   Width_Px : constant X_Axis.Length := Right_Edge - Left_Edge;
   Width_Mm : constant Millimeters := Width_Px * Scale;

   --  中点は「左端 + 長さの半分」で求める
   Center : constant Position := Left_Edge + Width_Px / 2.0;

   --  0.1 mm を px にして、エッジを探す範囲を左へ広げる
   Margin      : constant X_Axis.Length := Millimeters'(0.1) * Invert (Scale);
   Search_From : constant Position := Left_Edge - 2.0 * Margin;
begin
   Put_Line ("scale  =" & Mm_Per_Px'Image (Scale) & " mm/px");
   Put_Line ("width  =" & X_Axis.Length'Image (Width_Px) & " px");
   Put_Line ("width  =" & Millimeters'Image (Width_Mm) & " mm");
   Put_Line ("center =" & Position'Image (Center) & " px");
   Put_Line ("search =" & Position'Image (Search_From) & " px");
end Measure_Width;
