with Vision_Units;

package Tolerance is

   --  0.001 mm 刻みの10進固定小数点型（-9999.999 .. 9999.999 mm）
   type Mm is delta 0.001 digits 7;

   type Spec is record
      Nominal : Mm;
      Minus   : Mm;
      Plus    : Mm;
   end record;

   --  下限と上限を含めて合格とする
   function Accept_Fixed (Measured : Mm; S : Spec) return Boolean is
     (Measured in S.Nominal - S.Minus .. S.Nominal + S.Plus);

   --  ミリメートルの測定値を図面の桁に丸めてから判定する
   function Accept_Rounded (Measured : Vision_Units.Millimeters; S : Spec) return Boolean is
     (Accept_Fixed (Mm'Round (Measured), S));

end Tolerance;
