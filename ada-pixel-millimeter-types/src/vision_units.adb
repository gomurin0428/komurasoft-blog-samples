package body Vision_Units is

   function "*" (Left : Long_Float; Right : Millimeters) return Millimeters is
     (Millimeters (Left * Long_Float (Right)));

   function "/" (Left : Millimeters; Right : Long_Float) return Millimeters is
     (Millimeters (Long_Float (Left) / Right));

end Vision_Units;
