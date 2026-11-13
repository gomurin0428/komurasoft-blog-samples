package Images with SPARK_Mode is

   Max_Width  : constant := 4096;
   Max_Height : constant := 4096;

   subtype Row    is Natural range 0 .. Max_Height - 1;
   subtype Column is Natural range 0 .. Max_Width - 1;

   type Pixel is mod 2 ** 8;

   type Image is array (Row range <>, Column range <>) of Pixel;

   function Same_Shape (A, B : Image) return Boolean is
     (A'First (1) = B'First (1) and then A'Last (1) = B'Last (1)
      and then A'First (2) = B'First (2) and then A'Last (2) = B'Last (2));

end Images;
