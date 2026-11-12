package body Vision_Units.Axis is

   function "-" (Left, Right : Position) return Length is
     (Length (Long_Float (Left) - Long_Float (Right)));

   function "+" (Left : Position; Right : Length) return Position is
     (Position (Long_Float (Left) + Long_Float (Right)));

   function "-" (Left : Position; Right : Length) return Position is
     (Position (Long_Float (Left) - Long_Float (Right)));

   function "*" (Left : Long_Float; Right : Length) return Length is
     (Length (Left * Long_Float (Right)));

   function "/" (Left : Length; Right : Long_Float) return Length is
     (Length (Long_Float (Left) / Right));

   function "*" (Left : Length; Right : Mm_Per_Px) return Millimeters is
     (Millimeters (Long_Float (Left) * Long_Float (Right)));

   function "*" (Left : Millimeters; Right : Px_Per_Mm) return Length is
     (Length (Long_Float (Left) * Long_Float (Right)));

   function "/" (Left : Millimeters; Right : Length) return Mm_Per_Px is
     (Mm_Per_Px (Long_Float (Left) / Long_Float (Right)));

   function Invert (Scale : Mm_Per_Px) return Px_Per_Mm is
     (Px_Per_Mm (Long_Float'(1.0) / Long_Float (Scale)));

end Vision_Units.Axis;
