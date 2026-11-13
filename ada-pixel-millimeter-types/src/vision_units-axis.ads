generic
package Vision_Units.Axis is

   type Position  is new Long_Float;  --  画像上の座標（px）
   type Length    is new Long_Float;  --  2点の間の長さ（px）
   type Mm_Per_Px is new Long_Float;  --  校正係数（mm/px）
   type Px_Per_Mm is new Long_Float;  --  その逆数（px/mm）

   --  座標どうしの足し算・掛け算・割り算は意味がないので消す
   function "+" (Left, Right : Position) return Position is abstract;
   function "*" (Left, Right : Position) return Position is abstract;
   function "/" (Left, Right : Position) return Position is abstract;

   --  座標を返す引き算とべき乗も座標には意味がない
   function "-" (Left, Right : Position) return Position is abstract;
   function "**" (Left : Position; Right : Integer) return Position is abstract;

   --  px × px や px のべき乗を px として返す演算も消す
   function "*" (Left, Right : Length) return Length is abstract;
   function "/" (Left, Right : Length) return Length is abstract;
   function "**" (Left : Length; Right : Integer) return Length is abstract;

   --  校正係数どうしの掛け算・割り算・べき乗も消す
   function "*" (Left, Right : Mm_Per_Px) return Mm_Per_Px is abstract;
   function "/" (Left, Right : Mm_Per_Px) return Mm_Per_Px is abstract;
   function "**" (Left : Mm_Per_Px; Right : Integer) return Mm_Per_Px is abstract;
   function "*" (Left, Right : Px_Per_Mm) return Px_Per_Mm is abstract;
   function "/" (Left, Right : Px_Per_Mm) return Px_Per_Mm is abstract;
   function "**" (Left : Px_Per_Mm; Right : Integer) return Px_Per_Mm is abstract;

   --  座標と長さの関係
   function "-" (Left, Right : Position) return Length;
   function "+" (Left : Position; Right : Length) return Position;
   function "-" (Left : Position; Right : Length) return Position;

   --  長さを何倍するか、何分の一にするか
   function "*" (Left : Long_Float; Right : Length) return Length;
   function "/" (Left : Length; Right : Long_Float) return Length;

   --  単位の変換は、この2つの向きだけ
   function "*" (Left : Length; Right : Mm_Per_Px) return Millimeters;
   function "*" (Left : Millimeters; Right : Px_Per_Mm) return Length;

   --  既知の寸法を写した画像から校正係数を求める
   function "/" (Left : Millimeters; Right : Length) return Mm_Per_Px;

   --  係数の向きを変える
   function Invert (Scale : Mm_Per_Px) return Px_Per_Mm;

end Vision_Units.Axis;
