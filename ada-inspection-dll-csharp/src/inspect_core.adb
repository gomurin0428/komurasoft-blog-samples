package body Inspect_Core with SPARK_Mode is

   procedure Count_Dark
     (Img       : Image;
      Width     : Positive;
      Threshold : Byte;
      S         : out Summary)
   is
      X_First : constant Natural := Img'First (2);
      X_Last  : constant Natural := Img'First (2) + (Width - 1);
      Total   : Interfaces.Integer_64 := 0;
      Row     : Natural;
   begin
      S := (Defect_Pixels => 0, Min_X | Min_Y | Max_X | Max_Y => -1);

      for Y in Img'Range (1) loop
         Row := 0;
         for X in X_First .. X_Last loop
            if Img (Y, X) < Threshold then
               Row := Row + 1;
               declare
                  DX : constant Natural := X - X_First;
                  DY : constant Natural := Y - Img'First (1);
               begin
                  if S.Min_X < 0 then
                     S.Min_X := DX;
                     S.Min_Y := DY;
                     S.Max_X := DX;
                     S.Max_Y := DY;
                  else
                     S.Min_X := Integer'Min (S.Min_X, DX);
                     S.Max_X := Integer'Max (S.Max_X, DX);
                     S.Max_Y := Integer'Max (S.Max_Y, DY);
                  end if;
               end;
            end if;
            pragma Loop_Invariant (Row <= X - X_First + 1);
         end loop;

         Total := Total + Interfaces.Integer_64 (Row);
         pragma Loop_Invariant
           (Total >= 0
            and then Total <=
              (Interfaces.Integer_64 (Y) - Interfaces.Integer_64 (Img'First (1)) + 1)
              * Interfaces.Integer_64 (Natural'Last));
      end loop;

      S.Defect_Pixels := Total;
   end Count_Dark;

end Inspect_Core;
