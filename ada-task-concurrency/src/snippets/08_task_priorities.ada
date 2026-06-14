-- 08_task_priorities.ada
-- Task priorities (Real-Time Systems Annex D).
-- Higher-priority tasks preempt lower-priority ones.
-- Uses pragma Priority and the priority ceiling protocol.

--  Note: This demonstrates FIFO_Within_Priorities scheduling (Annex D).
--  pragma Task_Dispatching_Policy (FIFO_Within_Priorities);

--  If Annex D is not fully supported by the runtime, this may behave
--  like a standard task without real-time guarantees.

with Ada.Text_IO;       use Ada.Text_IO;
with System;

procedure Task_Priorities_Demo is

   task Low_Task is
      pragma Priority (System.Default_Priority);
   end Low_Task;

   task High_Task is
      pragma Priority (System.Default_Priority + 5);
   end High_Task;

   task body Low_Task is
   begin
      Put_Line ("[Low_Task]  Started.");
      Put_Line ("[Low_Task]  Working...");
      Put_Line ("[Low_Task]  Finished.");
   end Low_Task;

   task body High_Task is
   begin
      Put_Line ("[High_Task] Started.");
      Put_Line ("[High_Task] Working...");
      Put_Line ("[High_Task] Finished.");
   end High_Task;

begin
   Put_Line ("Main: waiting for tasks...");
end Task_Priorities_Demo;