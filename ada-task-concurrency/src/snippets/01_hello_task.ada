-- 01_hello_task.ada
-- Basic task creation: declare a task object, it runs in parallel with the main procedure.
-- The main procedure waits for all tasks to complete before exiting (rendezvous with termination).

with Ada.Text_IO; use Ada.Text_IO;

procedure Hello_Task_Demo is

   task Greeter is
      entry Start;
   end Greeter;

   task body Greeter is
   begin
      accept Start;
      Put_Line ("Hello from a task!");
   end Greeter;

begin
   Put_Line ("Main: starting task...");
   Greeter.Start;
   Put_Line ("Main: task has completed.");
end Hello_Task_Demo;