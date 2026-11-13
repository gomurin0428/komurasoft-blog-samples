with Vision_Dims; use Vision_Dims;

procedure Err_Dim is
   Scale_X : constant X_Scale  := 10.0 * mm / (500.0 * pxX);
   Width   : constant X_Length := 510.0 * pxX;
   Height  : constant Y_Length := 300.0 * pxY;

   Area    : constant X_Length  := Width * Width;            --  px × px
   Forgot  : constant Mm_Length := Width;                    --  係数の掛け忘れ
   Mixed   : constant Mm_Length := Height * Scale_X;         --  x と y
   Inverse : constant Mm_Length := Width * (1.0 / Scale_X);  --  係数の向き
begin
   null;
end Err_Dim;
