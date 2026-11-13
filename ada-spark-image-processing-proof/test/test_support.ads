with Interfaces;
with Images;   use Images;
with Labeling; use Labeling;

--  試験と速度の測定で使う、画像の確保と生成
package Test_Support is

   type Image_Access       is access Image;
   type Label_Image_Access is access Label_Image;
   type Parent_Access      is access Parent_Table;

   type C_Parent_Table is
     array (0 .. Max_Labels) of Interfaces.Integer_32
   with Convention => C;
   type C_Parent_Access is access C_Parent_Table;

   function New_Image (Width, Height : Positive) return Image_Access;
   function New_Labels (Width, Height : Positive) return Label_Image_Access;

   --  決まった種から擬似乱数の画像を作る
   procedure Fill_Random (Img : in out Image; Seed : Interfaces.Unsigned_32);

end Test_Support;
