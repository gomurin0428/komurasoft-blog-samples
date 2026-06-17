-- 04_statistics.ada
-- Generic with formal floating-point type.
-- Demonstrates that generics work with Ada's type categories (digits <>).

generic
   type Real is digits <>;
   type Real_Array is array (Positive range <>) of Real;
package Generic_Statistics is
   function Mean (Values : Real_Array) return Real;
   function Variance (Values : Real_Array) return Real;
end Generic_Statistics;

package body Generic_Statistics is
   function Mean (Values : Real_Array) return Real is
      Sum : Real := 0.0;
   begin
      for V of Values loop
         Sum := Sum + V;
      end loop;
      return Sum / Real (Values'Length);
   end Mean;

   function Variance (Values : Real_Array) return Real is
      M    : constant Real := Mean (Values);
      Sum2 : Real := 0.0;
   begin
      for V of Values loop
         Sum2 := Sum2 + (V - M) * (V - M);
      end loop;
      return Sum2 / Real (Values'Length);
   end Variance;
end Generic_Statistics;

with Ada.Text_IO;           use Ada.Text_IO;
with Generic_Statistics;

procedure Statistics_Demo is
   type Float_Array is array (Positive range <>) of Float;
   type Long_Array  is array (Positive range <>) of Long_Float;

   package Float_Stats is new Generic_Statistics (Float, Float_Array);
   package Long_Stats  is new Generic_Statistics (Long_Float, Long_Array);

   Samples_F : Float_Array := (1.0, 2.0, 3.0, 4.0, 5.0);
   Samples_L : Long_Array  := (10.0, 20.0, 30.0);
begin
   Put_Line ("Float  mean:     " & Float'Image (Float_Stats.Mean (Samples_F)));
   Put_Line ("Float  variance:  " & Float'Image (Float_Stats.Variance (Samples_F)));

   Put_Line ("Long   mean:     " & Long_Float'Image (Long_Stats.Mean (Samples_L)));
   Put_Line ("Long   variance:  " & Long_Float'Image (Long_Stats.Variance (Samples_L)));
end Statistics_Demo;
