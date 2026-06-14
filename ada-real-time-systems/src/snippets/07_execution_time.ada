-- 07_execution_time.ada
-- 実行時間制御 (Execution_Time)
-- タスクごとの CPU 消費時間を計測する

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;
with Ada.Execution_Time;
use type Ada.Execution_Time.CPU_Time;

procedure Execution_Time_Demo is

   package ET renames Ada.Execution_Time;

   task Busy_Worker is
      pragma Priority (System.Default_Priority + 1);
      pragma Storage_Size (4 * 1024);
   end Busy_Worker;

   task body Busy_Worker is
      Wall_Start : Time;
      Cpu_Start  : ET.CPU_Time;
      Dummy      : Integer := 0;
   begin
      Wall_Start := Clock;
      Cpu_Start := ET.Clock;

      Put_Line ("[Worker] Starting compute-bound work...");
      for I in 1 .. 20_000_000 loop
         Dummy := Dummy + 1;
      end loop;

      declare
         Wall_Elapsed : constant Duration :=
            To_Duration (Clock - Wall_Start);
         Cpu_Span     : constant Time_Span :=
            ET.Clock - Cpu_Start;
      begin
         Put_Line ("[Worker] Done, wall time:" &
                   Duration'Image (Wall_Elapsed) & "s");
         Put_Line ("[Worker] CPU time consumed:" &
                   Duration'Image (To_Duration (Cpu_Span)) & "s");
      end;
   end Busy_Worker;

   Cpu_Start_Main : constant ET.CPU_Time := ET.Clock;

begin
   Put_Line ("=== Execution Time Demo ===");

   delay until Clock + Milliseconds (500);

   declare
      Cpu_Span : constant Time_Span := ET.Clock - Cpu_Start_Main;
   begin
      Put_Line ("Main: CPU time consumed after 500ms:" &
                Duration'Image (To_Duration (Cpu_Span)) & "s");
   end;

   Put_Line ("Main: done");
end Execution_Time_Demo;