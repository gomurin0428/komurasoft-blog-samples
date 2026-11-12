with Interfaces; use type Interfaces.Integer_64;

package Inspect_Core with SPARK_Mode is

   type Byte is mod 2 ** 8;

   --  'Length が Integer に収まるよう、添字の上限を Natural'Last - 1 にする
   subtype Index is Natural range 0 .. Natural'Last - 1;

   --  1つ目の添字が行 (y)、2つ目が列 (x)。2つ目の長さが stride になる
   type Image is array (Index range <>, Index range <>) of Byte;

   type Summary is record
      Defect_Pixels : Interfaces.Integer_64;
      Min_X, Min_Y  : Integer;   --  欠陥の外接矩形。欠陥がなければ -1
      Max_X, Max_Y  : Integer;
   end record;

   --  各行の先頭から Width 画素だけを見て、Threshold より暗い画素を数える
   procedure Count_Dark
     (Img       : Image;
      Width     : Positive;
      Threshold : Byte;
      S         : out Summary)
   with
     Pre  => Img'Length (1) >= 1 and then Width <= Img'Length (2),
     Post => S.Defect_Pixels >= 0;

end Inspect_Core;
