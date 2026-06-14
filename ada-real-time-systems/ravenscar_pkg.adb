with Ada.Text_IO;  use Ada.Text_IO;
with Ada.Real_Time; use Ada.Real_Time;

package body Ravenscar_Pkg is
   protected body Signal is
      entry Wait_For_Release when Released is
      begin
         Released := False;
      end Wait_For_Release;

      procedure Release is
      begin
         Released := True;
      end Release;
   end Signal;

   task body Periodic_Worker is
      Start_Time   : constant Time := Clock;
      Next_Release : Time := Start_Time + Milliseconds (100);
      Period       : constant Time_Span := Milliseconds (100);
      Cycle_Count  : Natural := 0;
   begin
      Put_Line ("[Worker] Ravenscar periodic task starts");

      for I in 1 .. 4 loop
         delay until Next_Release;

         Cycle_Count := Cycle_Count + 1;
         Put_Line ("[Worker] Cycle" & Natural'Image (Cycle_Count) &
                   " at" & Duration'Image (To_Duration (Clock - Start_Time)) & "s");
         Signal.Release;
         Next_Release := Next_Release + Period;
      end loop;

      Put_Line ("[Worker] Finished");
   end Periodic_Worker;

   task body Monitor is
   begin
      Put_Line ("[Monitor] Waiting for signals...");

      for I in 1 .. 4 loop
         Signal.Wait_For_Release;
         Put_Line ("[Monitor] Received signal" & Natural'Image (I));
      end loop;

      Put_Line ("[Monitor] All signals received");
   end Monitor;
end Ravenscar_Pkg;
