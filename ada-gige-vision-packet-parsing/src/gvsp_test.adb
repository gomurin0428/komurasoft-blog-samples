with Ada.Text_IO; use Ada.Text_IO;
with Ada.Numerics.Discrete_Random;
with Interfaces;  use Interfaces;
with Gvsp;        use Gvsp;

procedure Gvsp_Test is

   --  標準 ID の leader（Mono8、640 x 480）
   Leader_Std : constant Byte_Array :=
     [16#00#, 16#00#, 16#00#, 16#2A#, 16#01#, 16#00#, 16#00#, 16#00#,
      16#00#, 16#00#, 16#00#, 16#01#,                   --  先頭の2バイト、Payload Type
      16#00#, 16#00#, 16#00#, 16#01#, 16#00#, 16#00#, 16#00#, 16#02#,
      16#01#, 16#08#, 16#00#, 16#01#,                   --  Pixel Format
      16#00#, 16#00#, 16#02#, 16#80#, 16#00#, 16#00#, 16#01#, 16#E0#,
      16#00#, 16#00#, 16#00#, 16#00#, 16#00#, 16#00#, 16#00#, 16#00#,
      16#00#, 16#00#, 16#00#, 16#00#];

   --  拡張 ID の payload（データ 4 バイト）
   Payload_Ext : constant Byte_Array :=
     [16#00#, 16#00#, 16#00#, 16#00#, 16#83#, 16#00#, 16#00#, 16#00#,
      16#00#, 16#00#, 16#00#, 16#01#, 16#00#, 16#00#, 16#00#, 16#00#,
      16#00#, 16#00#, 16#00#, 16#05#,
      16#10#, 16#20#, 16#30#, 16#40#];

   Unknown : constant Byte_Array :=
     [16#00#, 16#00#, 16#00#, 16#2A#, 16#09#, 16#00#, 16#00#, 16#01#];

   procedure Check (Label : String; Packet : Byte_Array) is
      H : Gvsp_Header;
      L : Image_Leader;
      R : Parse_Result;
   begin
      Parse_Header (Packet, H, R);
      Put (Label & ":" & Packet'Length'Image & " bytes, header " & R'Image);
      if R = Ok then
         Put (", " & H.Format'Image & ", block" & H.Block_Id'Image
              & ", packet" & H.Packet_Id'Image
              & ", data" & Data_Length (Packet, H)'Image);
         if H.Format = Leader then
            Parse_Image_Leader (Packet, H, L, R);
            Put (", leader " & R'Image);
            if R = Ok then
               Put ("," & L.Size_X'Image & " x" & L.Size_Y'Image);
            end if;
         end if;
      end if;
      New_Line;
   end Check;

   subtype Length_Range is Natural range 0 .. 64;
   package Random_Length is new Ada.Numerics.Discrete_Random (Length_Range);
   package Random_Byte is new Ada.Numerics.Discrete_Random (Unsigned_8);
   GL : Random_Length.Generator;
   GB : Random_Byte.Generator;
   Counts : array (Parse_Result) of Natural := [others => 0];

begin
   Check ("standard leader ", Leader_Std);
   Check ("cut to 5 bytes  ", Leader_Std (0 .. 4));
   Check ("cut to 30 bytes ", Leader_Std (0 .. 29));
   Check ("extended payload", Payload_Ext);
   Check ("extended, 12 b  ", Payload_Ext (0 .. 11));
   Check ("unknown format  ", Unknown);

   --  長さと中身がでたらめなパケットを 100 万個読ませる。
   Random_Length.Reset (GL, 1);
   Random_Byte.Reset (GB, 2);
   for I in 1 .. 1_000_000 loop
      declare
         N : constant Length_Range := Random_Length.Random (GL);
         P : Byte_Array (0 .. N - 1);
         H : Gvsp_Header;
         L : Image_Leader;
         R : Parse_Result;
      begin
         for B of P loop
            B := Random_Byte.Random (GB);
         end loop;
         if N > 4 and then I mod 2 = 0 then
            P (4) := P (4) mod 9;  --  形式の値を 0〜8 に寄せる
         end if;
         Parse_Header (P, H, R);
         if R = Ok and then H.Format = Leader then
            Parse_Image_Leader (P, H, L, R);
         end if;
         Counts (R) := Counts (R) + 1;
      end;
   end loop;

   for R in Parse_Result loop
      Put_Line (R'Image & ":" & Counts (R)'Image);
   end loop;
end Gvsp_Test;
