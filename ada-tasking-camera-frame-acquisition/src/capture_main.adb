with Ada.Text_IO;   use Ada.Text_IO;
with Ada.Real_Time; use Ada.Real_Time;
with Ada.Containers.Generic_Array_Sort;
with GNAT.OS_Lib;
with Capture_Config; use Capture_Config;
with Capture_Stats;  use Capture_Stats;
with Capture_Tasks;

procedure Capture_Main with SPARK_Mode => Off is
   pragma Priority (1);

   type Ms_Array is array (Positive range <>) of Duration;
   procedure Sort is new Ada.Containers.Generic_Array_Sort (Positive, Duration, Ms_Array);

   function Ms (D : Duration) return String is
      Img : constant String := Integer'Image (Integer (D * 10_000.0));
      V   : constant Integer := Integer'Value (Img);
   begin
      return Integer'Image (V / 10) & "." & Integer'Image (abs V mod 10) (2 .. 2);
   end Ms;

   procedure Report (Name : String; Values : in out Ms_Array) is
      Sum : Duration := 0.0;
   begin
      if Values'Length = 0 then
         Put_Line (Name & ": none");
         return;
      end if;
      Sort (Values);
      for V of Values loop
         Sum := Sum + V;
      end loop;
      Put_Line (Name & " ms: avg" & Ms (Sum / Values'Length)
                & " p50" & Ms (Values (Values'First + (Values'Length - 1) / 2))
                & " p99" & Ms (Values (Values'First + (Values'Length - 1) * 99 / 100))
                & " max" & Ms (Values (Values'Last)));
   end Report;

   N_Frames  : Natural := 0;
   N_Dropped : Natural := 0;
   N_Done    : Natural := 0;
   N_Broken  : Natural := 0;
begin
   Stats.Wait_Done;
   N_Frames := Natural (Frames);
   declare
      Late : Ms_Array (1 .. N_Frames);
      Lat  : Ms_Array (1 .. N_Frames);
   begin
      for F in 1 .. Frames loop
         Late (Positive (F)) := To_Duration (Stats.Lateness (F));
         if Stats.Dropped (F) then
            N_Dropped := N_Dropped + 1;
         end if;
         if Stats.Done_Frame (F) then
            N_Done := N_Done + 1;
            Lat (N_Done) := To_Duration (Stats.Latency (F));
            if not Stats.Intact (F) then
               N_Broken := N_Broken + 1;
            end if;
         end if;
      end loop;
      Put_Line ("frames" & N_Frames'Image & " processed" & N_Done'Image
                & " dropped" & N_Dropped'Image & " broken" & N_Broken'Image
                & " ready_peak" & Capture_Tasks.Ready.Max_Length'Image);
      Report ("camera lateness", Late);
      Report ("queue latency", Lat (1 .. N_Done));
   end;
   GNAT.OS_Lib.OS_Exit (0);
end Capture_Main;
