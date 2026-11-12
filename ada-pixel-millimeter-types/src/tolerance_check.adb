with Ada.Text_IO;            use Ada.Text_IO;
with Ada.Long_Float_Text_IO; use Ada.Long_Float_Text_IO;
with Vision_Units;           use Vision_Units;
with X_Axis;                 use X_Axis;
with Tolerance;              use Tolerance;

procedure Tolerance_Check is

   --  図面は 5.00 ± 0.10 mm。上限の 5.10 mm ちょうどは合格
   Drawing : constant Spec := (Nominal => 5.000, Minus => 0.100, Plus => 0.100);

   procedure Judge (Px : Length; Scale : Mm_Per_Px) is
      Measured : constant Millimeters := Px * Scale;
      Upper    : constant Long_Float :=
        Long_Float (Drawing.Nominal) + Long_Float (Drawing.Plus);
   begin
      Put ("measured (Long_Float) = ");
      Put (Long_Float (Measured), Fore => 1, Aft => 17, Exp => 0);
      New_Line;
      Put ("upper    (Long_Float) = ");
      Put (Upper, Fore => 1, Aft => 17, Exp => 0);
      New_Line;
      Put_Line ("Long_Float の比較     : " & Boolean'Image (Long_Float (Measured) <= Upper));
      Put_Line ("Mm に丸めてから比較   : "
                & Boolean'Image (Accept_Rounded (Measured, Drawing)));
   end Judge;

   --  型変換は切り捨て、'Round は四捨五入
   procedure Show_Conversion (X : Long_Float) is
   begin
      Put_Line ("Mm (X)       =" & Mm'Image (Mm (X)));
      Put_Line ("Mm'Round (X) =" & Mm'Image (Mm'Round (X)));
   end Show_Conversion;

begin
   Judge (Px => 510.0, Scale => 0.01);
   Show_Conversion (5.0996);
end Tolerance_Check;
