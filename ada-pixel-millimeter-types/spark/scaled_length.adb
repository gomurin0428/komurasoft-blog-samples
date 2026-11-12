package body Scaled_Length with SPARK_Mode is

   function To_Mm (L : Px_Length; S : Mm_Per_Px) return Mm is
   begin
      return Mm (L * S);
   end To_Mm;

end Scaled_Length;
