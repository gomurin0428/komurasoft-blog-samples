-- 04_ravenscar_profile.ada
-- Ravenscar プロファイルの基本形
-- コンパイル時に gnat.adc で pragma Profile (Ravenscar); を指定する

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;

package Ravenscar_State is

   protected Signal is
      pragma Priority (System.Default_Priority + 5);
      entry Wait_For_Release;
      procedure Release;
   private
      Pending : Natural := 0;
   end Signal;

   task Periodic_Worker is
      pragma Priority (System.Default_Priority + 1);
      pragma Storage_Size (4 * 1024);
   end Periodic_Worker;

   task Monitor is
      pragma Priority (System.Default_Priority);
      pragma Storage_Size (4 * 1024);
   end Monitor;

end Ravenscar_State;

package body Ravenscar_State is

   protected body Signal is
      entry Wait_For_Release when Pending > 0 is
      begin
         Pending := Pending - 1;
      end Wait_For_Release;

      procedure Release is
      begin
         Pending := Pending + 1;
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

end Ravenscar_State;

with Ravenscar_State; use Ravenscar_State;
with Ada.Text_IO;     use Ada.Text_IO;
with Ada.Real_Time;   use Ada.Real_Time;

procedure Ravenscar_Demo is
begin
   Put_Line ("=== Ravenscar Profile Demo ===");
   Put_Line ("(compile with: gnatmake -gnatec=gnat.adc ravenscar_demo)");
   Put_Line ("Main: waiting for Ravenscar tasks...");
   delay until Clock + Milliseconds (800);
   Put_Line ("Main: done");
end Ravenscar_Demo;
