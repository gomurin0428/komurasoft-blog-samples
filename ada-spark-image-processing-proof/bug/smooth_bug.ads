with Images; use Images;

package Smooth_Bug with SPARK_Mode is

   procedure Smooth_3x3
     (Source : Image;
      Target : in out Image)
   with
     Pre => Same_Shape (Source, Target);

   procedure Binarize_Swapped
     (Source    : Image;
      Threshold : Pixel;
      Target    : in out Image)
   with
     Pre => Same_Shape (Source, Target);

end Smooth_Bug;
