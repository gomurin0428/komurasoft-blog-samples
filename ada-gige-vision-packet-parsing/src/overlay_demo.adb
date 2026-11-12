with Ada.Text_IO;            use Ada.Text_IO;
with Ada.Unchecked_Conversion;
with Gvsp;                   use Gvsp;
with Gvsp_Layout;            use Gvsp_Layout;

procedure Overlay_Demo is

   subtype Header_Bytes is Byte_Array (0 .. 7);

   function To_Header is
     new Ada.Unchecked_Conversion (Header_Bytes, Standard_Header);

   procedure Show (Label : String; Packet : Byte_Array) is
   begin
      declare
         H : constant Standard_Header :=
           To_Header (Packet (Packet'First .. Packet'First + 7));
      begin
         Put (Label & ": Block_Id =" & H.Block_Id'Image
              & " Packet_Id =" & H.Packet_Id'Image);
         if H.Format'Valid then
            Put_Line (" Format = " & H.Format'Image);
         else
            Put_Line (" Format は不正な値");
         end if;
      end;
   exception
      when Constraint_Error =>
         Put_Line (Label & ": Constraint_Error");
   end Show;

   Good   : constant Byte_Array :=
     [16#00#, 16#00#, 16#00#, 16#2A#, 16#03#, 16#00#, 16#01#, 16#02#];
   Broken : constant Byte_Array :=
     [16#00#, 16#00#, 16#00#, 16#2A#, 16#09#, 16#00#, 16#01#, 16#02#];

begin
   Show ("Good  ", Good);
   Show ("Broken", Broken);
   Show ("Short ", Good (0 .. 4));
end Overlay_Demo;
