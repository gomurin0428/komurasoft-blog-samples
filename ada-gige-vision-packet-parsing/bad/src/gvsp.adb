package body Gvsp with SPARK_Mode is

   function Read_U16 (P : Byte_Array; Offset : Natural) return Unsigned_16 is
     (Shift_Left (Unsigned_16 (P (P'First + Offset)), 8)
      or Unsigned_16 (P (P'First + Offset + 1)));

   function Read_U32 (P : Byte_Array; Offset : Natural) return Unsigned_32 is
     (Shift_Left (Unsigned_32 (P (P'First + Offset)), 24)
      or Shift_Left (Unsigned_32 (P (P'First + Offset + 1)), 16)
      or Shift_Left (Unsigned_32 (P (P'First + Offset + 2)), 8)
      or Unsigned_32 (P (P'First + Offset + 3)));

   function Read_U64 (P : Byte_Array; Offset : Natural) return Unsigned_64 is
     (Shift_Left (Unsigned_64 (Read_U32 (P, Offset)), 32)
      or Unsigned_64 (Read_U32 (P, Offset + 4)));

   procedure Parse_Header
     (Packet : Byte_Array;
      Header : out Gvsp_Header;
      Result : out Parse_Result)
   is
      Format_Byte : Unsigned_8;
      Format_Code : Unsigned_8;
   begin
      Header := (others => <>);

      if Packet'Length < Standard_Header_Length then
         Result := Too_Short;
         return;
      end if;

      Header.Status_Code := Read_U16 (Packet, 0);
      Format_Byte        := Packet (Packet'First + 4);
      Header.Extended_Id := (Format_Byte and 16#80#) /= 0;
      Format_Code        := Format_Byte and 16#7F#;

      if Format_Code not in 1 .. 8 then
         Result := Unknown_Format;
         return;
      end if;
      Header.Format := Packet_Format'Val (Format_Code - 1);

      if Header.Extended_Id then
         Header.Flags       := Read_U16 (Packet, 2);
         Header.Block_Id    := Read_U64 (Packet, 8);
         Header.Packet_Id   := Read_U32 (Packet, 16);
         Header.Data_Offset := Extended_Header_Length;
      else
         Header.Block_Id    := Unsigned_64 (Read_U16 (Packet, 2));
         Header.Packet_Id   := Read_U32 (Packet, 4) and 16#00FF_FFFF#;
         Header.Data_Offset := Standard_Header_Length;
      end if;

      Result := Ok;
   end Parse_Header;

   procedure Parse_Image_Leader
     (Packet : Byte_Array;
      Header : Gvsp_Header;
      Leader : out Image_Leader;
      Result : out Parse_Result)
   is
      D : constant Natural := Header.Data_Offset;
   begin
      Leader := (others => <>);

      if Header.Format /= Gvsp.Leader then
         Result := Not_Leader;
         return;
      end if;

      if Packet'Length - D < Image_Leader_Length then
         Result := Too_Short;
         return;
      end if;

      --  先頭の2バイトはこの例では読まない。
      Leader.Payload_Type := Read_U16 (Packet, D + 2);
      if (Leader.Payload_Type and 16#3FFF#) /= 16#0001# then
         Result := Not_Image;
         return;
      end if;

      Leader.Timestamp    := Read_U64 (Packet, D + 4);
      Leader.Pixel_Format := Read_U32 (Packet, D + 12);
      Leader.Size_X       := Read_U32 (Packet, D + 16);
      Leader.Size_Y       := Read_U32 (Packet, D + 20);
      Leader.Offset_X     := Read_U32 (Packet, D + 24);
      Leader.Offset_Y     := Read_U32 (Packet, D + 28);
      Leader.Padding_X    := Read_U16 (Packet, D + 32);
      Leader.Padding_Y    := Read_U16 (Packet, D + 34);
      Result := Ok;
   end Parse_Image_Leader;

end Gvsp;
