package body Frame_Pool with SPARK_Mode => Off is

   use Interfaces;
   use Ada.Real_Time;

   Width  : constant := 640;
   Height : constant := 480;

   type Pixel_Array is array (1 .. Width * Height) of Unsigned_8;

   type Slot_Record is record
      Frame   : Frame_Number := 0;
      Arrival : Time := Time_First;
      Data    : Pixel_Array;
   end record;

   Slots : array (Slot_Index) of Slot_Record;

   procedure Fill
     (Slot    : Slot_Index;
      Frame   : Frame_Number;
      Arrival : Time) is
   begin
      --  受信した画像を書き込む代わりに、フレーム番号で全画素を埋める
      Slots (Slot).Data := [others => Unsigned_8 (Frame mod 256)];
      Slots (Slot).Frame := Frame;
      Slots (Slot).Arrival := Arrival;
   end Fill;

   procedure Inspect
     (Slot     : Slot_Index;
      Frame    : out Frame_Number;
      Arrival  : out Time;
      Checksum : out Unsigned_32) is
   begin
      Frame := Slots (Slot).Frame;
      Arrival := Slots (Slot).Arrival;
      Checksum := 0;
      for P of Slots (Slot).Data loop
         Checksum := Checksum + Unsigned_32 (P);
      end loop;
   end Inspect;

   function Expected_Checksum (Frame : Frame_Number) return Unsigned_32 is
     (Unsigned_32 (Frame mod 256) * Unsigned_32 (Width * Height));

end Frame_Pool;
