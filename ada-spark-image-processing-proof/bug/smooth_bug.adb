package body Smooth_Bug with SPARK_Mode is

   procedure Smooth_3x3
     (Source : Image;
      Target : in out Image)
   is
      Sum : Natural;
   begin
      for Y in Source'Range (1) loop
         for X in Source'Range (2) loop
            Sum := Natural (Source (Y - 1, X - 1))
                 + Natural (Source (Y - 1, X))
                 + Natural (Source (Y - 1, X + 1))
                 + Natural (Source (Y, X - 1))
                 + Natural (Source (Y, X))
                 + Natural (Source (Y, X + 1))
                 + Natural (Source (Y + 1, X - 1))
                 + Natural (Source (Y + 1, X))
                 + Natural (Source (Y + 1, X + 1));
            Target (Y, X) := Pixel (Sum / 9);
         end loop;
      end loop;
   end Smooth_3x3;

   procedure Binarize_Swapped
     (Source    : Image;
      Threshold : Pixel;
      Target    : in out Image)
   is
   begin
      for Y in Source'Range (1) loop
         for X in Source'Range (2) loop
            Target (X, Y) := (if Source (X, Y) >= Threshold then 255 else 0);
         end loop;
      end loop;
   end Binarize_Swapped;

end Smooth_Bug;
