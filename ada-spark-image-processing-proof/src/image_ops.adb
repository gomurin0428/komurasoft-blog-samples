package body Image_Ops with SPARK_Mode is

   procedure Binarize
     (Source    : Image;
      Threshold : Pixel;
      Target    : in out Image)
   is
   begin
      for Y in Source'Range (1) loop
         for X in Source'Range (2) loop
            Target (Y, X) := (if Source (Y, X) >= Threshold then 255 else 0);

            pragma Loop_Invariant
              (for all C in Source'First (2) .. X =>
                 Target (Y, C) =
                   (if Source (Y, C) >= Threshold then 255 else 0));
            pragma Loop_Invariant
              (for all R in Source'First (1) .. Y - 1 =>
                 (for all C in Source'Range (2) =>
                    Target (R, C) =
                      (if Source (R, C) >= Threshold then 255 else 0)));
         end loop;

         pragma Loop_Invariant
           (for all R in Source'First (1) .. Y =>
              (for all C in Source'Range (2) =>
                 Target (R, C) =
                   (if Source (R, C) >= Threshold then 255 else 0)));
      end loop;
   end Binarize;

   procedure Smooth_3x3
     (Source : Image;
      Target : in out Image)
   is
      Sum : Natural;
   begin
      Target := Source;

      for Y in Source'First (1) + 1 .. Source'Last (1) - 1 loop
         for X in Source'First (2) + 1 .. Source'Last (2) - 1 loop
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

end Image_Ops;
