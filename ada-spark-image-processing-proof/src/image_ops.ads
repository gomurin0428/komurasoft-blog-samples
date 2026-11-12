pragma Assertion_Policy (Pre => Check);
with Images; use Images;

package Image_Ops with SPARK_Mode is

   procedure Binarize
     (Source    : Image;
      Threshold : Pixel;
      Target    : in out Image)
   with
     Pre  => Same_Shape (Source, Target),
     Post =>
       (for all Y in Source'Range (1) =>
          (for all X in Source'Range (2) =>
             Target (Y, X) =
               (if Source (Y, X) >= Threshold then 255 else 0)));

   procedure Smooth_3x3
     (Source : Image;
      Target : in out Image)
   with
     Pre => Source'Length (1) > 0
              and then Source'Length (2) > 0
              and then Same_Shape (Source, Target);

end Image_Ops;
