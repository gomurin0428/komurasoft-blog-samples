package Vision_Dims is

   --  GNAT 独自のアスペクト。mm と x・y のピクセルを基本の次元にする
   type Vision_Type is new Long_Float
     with Dimension_System =>
       ((Unit_Name => Millimeter, Unit_Symbol => "mm",  Dim_Symbol => 'L'),
        (Unit_Name => Pixel_X,    Unit_Symbol => "pxX", Dim_Symbol => 'X'),
        (Unit_Name => Pixel_Y,    Unit_Symbol => "pxY", Dim_Symbol => 'Y'));

   subtype Mm_Length is Vision_Type
     with Dimension => (Symbol => "mm", Millimeter => 1, others => 0);

   subtype X_Length is Vision_Type
     with Dimension => (Symbol => "pxX", Pixel_X => 1, others => 0);

   subtype Y_Length is Vision_Type
     with Dimension => (Symbol => "pxY", Pixel_Y => 1, others => 0);

   subtype X_Scale is Vision_Type
     with Dimension =>
       (Symbol => "mm/pxX", Millimeter => 1, Pixel_X => -1, others => 0);

   subtype Y_Scale is Vision_Type
     with Dimension =>
       (Symbol => "mm/pxY", Millimeter => 1, Pixel_Y => -1, others => 0);

   pragma Warnings (Off, "*assumed to be*");
   mm  : constant Mm_Length := 1.0;
   pxX : constant X_Length  := 1.0;
   pxY : constant Y_Length  := 1.0;
   pragma Warnings (On, "*assumed to be*");

end Vision_Dims;
