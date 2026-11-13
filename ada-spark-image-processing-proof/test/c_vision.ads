with Interfaces.C;
with System;

--  C で書いた同じ処理（c/vision.c）を呼ぶための宣言
package C_Vision is

   use Interfaces.C;

   procedure Binarize
     (Src, Dst : System.Address; Width, Height : int; Threshold : unsigned_char)
   with Import, Convention => C, External_Name => "c_binarize";

   procedure Smooth_3x3 (Src, Dst : System.Address; Width, Height : int)
   with Import, Convention => C, External_Name => "c_smooth_3x3";

   function Label_Components
     (Src, Labels, Parent : System.Address;
      Width, Height       : int;
      Max_Labels          : int) return int
   with Import, Convention => C, External_Name => "c_label_components";

end C_Vision;
