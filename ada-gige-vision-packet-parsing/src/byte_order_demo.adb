--  Scalar_Storage_Order を付けない型と付けた型で、同じ 4 バイトを読み比べる。
with Ada.Text_IO;            use Ada.Text_IO;
with Ada.Unchecked_Conversion;
with Interfaces;             use Interfaces;
with System;

procedure Byte_Order_Demo is

   type Without_SSO is record
      Status   : Unsigned_16;
      Block_Id : Unsigned_16;
   end record
     with Bit_Order => System.High_Order_First, Size => 32;

   for Without_SSO use record
      Status   at 0 range 0 .. 15;
      Block_Id at 2 range 0 .. 15;
   end record;

   type With_SSO is record
      Status   : Unsigned_16;
      Block_Id : Unsigned_16;
   end record
     with Bit_Order            => System.High_Order_First,
          Scalar_Storage_Order => System.High_Order_First,
          Size                 => 32;

   for With_SSO use record
      Status   at 0 range 0 .. 15;
      Block_Id at 2 range 0 .. 15;
   end record;

   type Four_Bytes is array (0 .. 3) of Unsigned_8;

   function To_Without is new Ada.Unchecked_Conversion (Four_Bytes, Without_SSO);
   function To_With is new Ada.Unchecked_Conversion (Four_Bytes, With_SSO);

   Bytes : constant Four_Bytes := [16#00#, 16#00#, 16#00#, 16#2A#];

begin
   Put_Line ("Scalar_Storage_Order なし: Block_Id =" & To_Without (Bytes).Block_Id'Image);
   Put_Line ("Scalar_Storage_Order あり: Block_Id =" & To_With (Bytes).Block_Id'Image);
end Byte_Order_Demo;
