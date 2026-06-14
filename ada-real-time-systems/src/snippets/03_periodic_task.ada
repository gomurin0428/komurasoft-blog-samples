-- 03_periodic_task.ada
-- delay until による周期タスク ── 累積ドリフトを防ぐ

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;

procedure Periodic_Task_Demo is

   Period_MS : constant Time_Span := Milliseconds (200);
   Cycles    : constant Positive  := 5;

   task Sensor_Reader is
      pragma Priority (Priority'Last - 2);
      pragma Storage_Size (4 * 1024);
   end Sensor_Reader;

   task body Sensor_Reader is
      Start_Time  : constant Time := Clock;
      Next_Release : Time := Start_Time + Period_MS;
      Cycle_Count  : Natural := 0;
   begin
      Put_Line ("[Sensor] Periodic task starts, period=" &
                To_Duration (Period_MS)'Image & "s, cycles=" &
                Natural'Image (Cycles));

      for I in 1 .. Cycles loop
         delay until Next_Release;

         Cycle_Count := Cycle_Count + 1;
         Put_Line ("[Sensor] Cycle" & Natural'Image (Cycle_Count) &
                   " at" & Duration'Image (To_Duration (Clock - Start_Time)) & "s");

         Next_Release := Next_Release + Period_MS;
      end loop;

      Put_Line ("[Sensor] Periodic task finished. Actual elapsed:" &
                Duration'Image (To_Duration (Clock - Start_Time)) & "s");
   end Sensor_Reader;

begin
   Put_Line ("=== Periodic Task Demo (delay until) ===");
   Put_Line ("Main: waiting for" & Natural'Image (Cycles) & " cycles...");
   delay until Clock + Milliseconds (1500);
   Put_Line ("Main: done");
end Periodic_Task_Demo;