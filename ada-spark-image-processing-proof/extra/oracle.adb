with Ada.Text_IO; use Ada.Text_IO;
with Ada.Command_Line;
with Ada.Numerics.Discrete_Random;
with Images;     use Images;
with Labeling;   use Labeling;
with Image_Ops;  use Image_Ops;

--  乱数で作った小さな画像（1〜9 × 1〜9、添字の下限も 0〜4 で変える）2万個で、
--  ラベリングを再帰の塗りつぶしと、平滑化を 3×3 の平均の直接の計算と比べる。
procedure Oracle is
   package R is new Ada.Numerics.Discrete_Random (Natural);
   G : R.Generator;
   type Parent_Access is access Parent_Table;
   Parent : constant Parent_Access := new Parent_Table'(others => 1);
   Bad : Natural := 0;
   Smooth_Bad : Natural := 0;
begin
   R.Reset (G, 42);
   for Trial in 1 .. 20_000 loop
      declare
         H    : constant Positive := 1 + R.Random (G) mod 9;
         W    : constant Positive := 1 + R.Random (G) mod 9;
         FY   : constant Natural := R.Random (G) mod 5;
         FX   : constant Natural := R.Random (G) mod 5;
         Dens : constant Natural := 1 + R.Random (G) mod 9;
         Src  : Image (FY .. FY + H - 1, FX .. FX + W - 1);
         Lab  : Label_Image (FY .. FY + H - 1, FX .. FX + W - 1) :=
           (others => (others => 0));
         Ref  : array (Src'Range (1), Src'Range (2)) of Natural :=
           (others => (others => 0));
         Count     : Natural;
         Ok        : Boolean;
         Ref_Count : Natural := 0;
         Good      : Boolean := True;

         procedure Flood (Y, X : Integer; Id : Positive) is
         begin
            if Y not in Src'Range (1) or else X not in Src'Range (2) then
               return;
            end if;
            if Src (Y, X) = 0 or else Ref (Y, X) /= 0 then
               return;
            end if;
            Ref (Y, X) := Id;
            Flood (Y - 1, X, Id);
            Flood (Y + 1, X, Id);
            Flood (Y, X - 1, Id);
            Flood (Y, X + 1, Id);
         end Flood;
      begin
         for Y in Src'Range (1) loop
            for X in Src'Range (2) loop
               Src (Y, X) :=
                 (if R.Random (G) mod 10 < Dens
                  then Pixel (1 + R.Random (G) mod 255) else 0);
            end loop;
         end loop;

         Label_Components (Src, Lab, Parent.all, Count, Ok);
         for Y in Src'Range (1) loop
            for X in Src'Range (2) loop
               if Src (Y, X) /= 0 and then Ref (Y, X) = 0 then
                  Ref_Count := Ref_Count + 1;
                  Flood (Y, X, Ref_Count);
               end if;
            end loop;
         end loop;
         if not Ok or else Count /= Ref_Count then
            Good := False;
         end if;

         --  同じ分け方か: 背景どうしが一致し、ラベルが等しい ⇔ 塗りつぶしの番号が等しい
         for Y in Src'Range (1) loop
            for X in Src'Range (2) loop
               if (Lab (Y, X) = 0) /= (Ref (Y, X) = 0) then
                  Good := False;
               end if;
               for Y2 in Src'Range (1) loop
                  for X2 in Src'Range (2) loop
                     if Ref (Y, X) /= 0 and then Ref (Y2, X2) /= 0
                       and then (Lab (Y, X) = Lab (Y2, X2))
                                /= (Ref (Y, X) = Ref (Y2, X2))
                     then
                        Good := False;
                     end if;
                  end loop;
               end loop;
            end loop;
         end loop;
         if not Good then
            Bad := Bad + 1;
         end if;

         --  平滑化: 縁は元の値、内側は 3×3 の平均（切り捨て）
         declare
            Dst : Image (Src'Range (1), Src'Range (2)) :=
              (others => (others => 0));
            Exp : Natural;
         begin
            Smooth_3x3 (Src, Dst);
            for Y in Src'Range (1) loop
               for X in Src'Range (2) loop
                  if Y = Src'First (1) or else Y = Src'Last (1)
                    or else X = Src'First (2) or else X = Src'Last (2)
                  then
                     Exp := Natural (Src (Y, X));
                  else
                     Exp := 0;
                     for DY in -1 .. 1 loop
                        for DX in -1 .. 1 loop
                           Exp := Exp + Natural (Src (Y + DY, X + DX));
                        end loop;
                     end loop;
                     Exp := Exp / 9;
                  end if;
                  if Natural (Dst (Y, X)) /= Exp then
                     Smooth_Bad := Smooth_Bad + 1;
                  end if;
               end loop;
            end loop;
         end;
      end;
   end loop;

   Put_Line ("labeling mismatches:" & Bad'Image
             & ", smoothing pixel mismatches:" & Smooth_Bad'Image);
   if Bad /= 0 or else Smooth_Bad /= 0 then
      Ada.Command_Line.Set_Exit_Status (Ada.Command_Line.Failure);
   end if;
end Oracle;
