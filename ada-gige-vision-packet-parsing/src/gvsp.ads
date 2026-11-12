with Interfaces; use Interfaces;

package Gvsp with SPARK_Mode is

   --  UDP のデータ部の最大長より大きい値を、添字の上限にする。
   subtype Packet_Index is Natural range 0 .. 65_535;
   type Byte_Array is array (Packet_Index range <>) of Unsigned_8;

   type Packet_Format is
     (Leader, Trailer, Payload, All_In, H264, Multi_Zone, Multi_Part, Gen_DC);

   type Parse_Result is (Ok, Too_Short, Unknown_Format, Not_Leader, Not_Image);

   Standard_Header_Length : constant := 8;
   Extended_Header_Length : constant := 20;
   Image_Leader_Length    : constant := 36;

   type Gvsp_Header is record
      Status_Code : Unsigned_16   := 0;
      Flags       : Unsigned_16   := 0;  --  拡張 ID のときだけ
      Extended_Id : Boolean       := False;
      Format      : Packet_Format := Leader;
      Block_Id    : Unsigned_64   := 0;
      Packet_Id   : Unsigned_32   := 0;
      Data_Offset : Natural       := 0;  --  データ部の先頭（パケットの先頭から）
   end record;

   type Image_Leader is record
      Payload_Type : Unsigned_16 := 0;
      Timestamp    : Unsigned_64 := 0;
      Pixel_Format : Unsigned_32 := 0;
      Size_X       : Unsigned_32 := 0;
      Size_Y       : Unsigned_32 := 0;
      Offset_X     : Unsigned_32 := 0;
      Offset_Y     : Unsigned_32 := 0;
      Padding_X    : Unsigned_16 := 0;
      Padding_Y    : Unsigned_16 := 0;
   end record;

   function Read_U16 (P : Byte_Array; Offset : Natural) return Unsigned_16
   with Pre => P'Length >= 2 and then Offset <= P'Length - 2;

   function Read_U32 (P : Byte_Array; Offset : Natural) return Unsigned_32
   with Pre => P'Length >= 4 and then Offset <= P'Length - 4;

   function Read_U64 (P : Byte_Array; Offset : Natural) return Unsigned_64
   with Pre => P'Length >= 8 and then Offset <= P'Length - 8;

   procedure Parse_Header
     (Packet : Byte_Array;
      Header : out Gvsp_Header;
      Result : out Parse_Result)
   with
     Post =>
       (if Result = Ok then
          Header.Data_Offset in Standard_Header_Length | Extended_Header_Length
          and then Header.Data_Offset <= Packet'Length);

   procedure Parse_Image_Leader
     (Packet : Byte_Array;
      Header : Gvsp_Header;
      Leader : out Image_Leader;
      Result : out Parse_Result)
   with
     Pre => Header.Data_Offset <= Packet'Length;

   function Data_Length (Packet : Byte_Array; Header : Gvsp_Header) return Natural
   is (Packet'Length - Header.Data_Offset)
   with Pre => Header.Data_Offset <= Packet'Length;

end Gvsp;
