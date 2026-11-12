with Interfaces; use Interfaces;
with System;

package Gvsp_Layout is

   type Format_Code is
     (Leader, Trailer, Payload, All_In, H264, Multi_Zone, Multi_Part, Gen_DC);
   for Format_Code use
     (Leader => 1, Trailer => 2, Payload => 3, All_In => 4,
      H264 => 5, Multi_Zone => 6, Multi_Part => 7, Gen_DC => 8);
   for Format_Code'Size use 7;

   type Packet_Id_24 is mod 2 ** 24;

   --  標準 ID のヘッダー（8 バイト）
   type Standard_Header is record
      Status      : Unsigned_16;
      Block_Id    : Unsigned_16;
      Extended_Id : Boolean;
      Format      : Format_Code;
      Packet_Id   : Packet_Id_24;
   end record
     with Bit_Order            => System.High_Order_First,
          Scalar_Storage_Order => System.High_Order_First,
          Size                 => 64;

   for Standard_Header use record
      Status      at 0 range 0 .. 15;
      Block_Id    at 2 range 0 .. 15;
      Extended_Id at 4 range 0 .. 0;
      Format      at 4 range 1 .. 7;
      Packet_Id   at 5 range 0 .. 23;
   end record;

end Gvsp_Layout;
