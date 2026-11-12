package Scaled_Length with SPARK_Mode is

   --  画像の幅の上限を 1000000 px に広げた場合
   type Px_Length is delta 0.01 range 0.0 .. 1_000_000.0
     with Small => 0.01;

   --  0.00001 mm/px 刻みの校正係数
   type Mm_Per_Px is delta 0.000_01 range 0.000_01 .. 1.0
     with Small => 0.000_01;

   --  0.001 mm 刻みの長さ
   type Mm is delta 0.001 range -100_000.0 .. 100_000.0
     with Small => 0.001;

   function To_Mm (L : Px_Length; S : Mm_Per_Px) return Mm;

end Scaled_Length;
