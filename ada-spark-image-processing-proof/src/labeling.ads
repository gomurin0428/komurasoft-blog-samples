pragma Assertion_Policy (Pre => Check);
with Images; use Images;

package Labeling with SPARK_Mode is

   Max_Labels : constant := 2 ** 22;

   subtype Label    is Natural range 0 .. Max_Labels;  --  0 は背景
   subtype Label_Id is Label range 1 .. Max_Labels;

   type Label_Image  is array (Row range <>, Column range <>) of Label;
   type Parent_Table is array (Label_Id) of Label_Id;

   procedure Label_Components
     (Source  : Image;
      Labels  : in out Label_Image;
      Parent  : in out Parent_Table;
      Count   : out Natural;
      Success : out Boolean)
   with
     Pre => Labels'First (1) = Source'First (1)
              and then Labels'Last (1) = Source'Last (1)
              and then Labels'First (2) = Source'First (2)
              and then Labels'Last (2) = Source'Last (2);

end Labeling;
