package Vision_Units is

   type Millimeters is new Long_Float;

   --  mm × mm、mm ÷ mm、mm のべき乗を mm として返す演算は意味がないので消す
   function "*" (Left, Right : Millimeters) return Millimeters is abstract;
   function "/" (Left, Right : Millimeters) return Millimeters is abstract;
   function "**" (Left : Millimeters; Right : Integer) return Millimeters is abstract;

   --  何倍するか、何分の一にするかは無次元の数で書く
   function "*" (Left : Long_Float; Right : Millimeters) return Millimeters;
   function "/" (Left : Millimeters; Right : Long_Float) return Millimeters;

end Vision_Units;
