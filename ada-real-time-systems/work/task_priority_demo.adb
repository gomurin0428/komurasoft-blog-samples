-- 01_task_priority.ada
-- タスク優先度と FIFO_Within_Priorities の基本形

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;

procedure Task_Priority_Demo is

   task High_Priority_Task is
      pragma Priority (Priority'Last);
      pragma Storage_Size (4 * 1024);
   end High_Priority_Task;

   task Low_Priority_Task is
      pragma Priority (Priority'First);
      pragma Storage_Size (4 * 1024);
   end Low_Priority_Task;

   task body High_Priority_Task is
   begin
      Put_Line ("[T=0.0s] High priority task started");
      delay until Clock + Milliseconds (100);
      Put_Line ("[T=0.1s] High priority task completed");
   end High_Priority_Task;

   task body Low_Priority_Task is
   begin
      Put_Line ("[T=0.0s] Low priority task started");
      delay until Clock + Milliseconds (500);
      Put_Line ("[T=0.5s] Low priority task completed");
   end Low_Priority_Task;

begin
   Put_Line ("=== Task Priority Demo (FIFO_Within_Priorities) ===");
   Put_Line ("Main: waiting for tasks to complete...");
   delay until Clock + Milliseconds (800);
   Put_Line ("Main: done");
end Task_Priority_Demo;