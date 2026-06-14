
procedure Timing_Events_Demo is

   Timer_1 : Timing_Event;
   Timer_2 : Timing_Event;

   task Reactor is
      pragma Priority (System.Default_Priority + 5);
      pragma Storage_Size (4 * 1024);
   end Reactor;

   task body Reactor is
   begin
      Put_Line ("[Reactor] Waiting for timing events...");

      Signal.Wait_For_Event;
      Put_Line ("[Reactor] Got event #1");

      Signal.Wait_For_Event;
      Put_Line ("[Reactor] Got event #2");

      Put_Line ("[Reactor] Done");
   end Reactor;

begin
   Put_Line ("=== Timing Events Demo ===");
   Put_Line ("Scheduling two timers at +100ms and +250ms...");

   Set_Handler (Timer_1, Clock + Milliseconds (100), Signal.Fire'Access);
   Set_Handler (Timer_2, Clock + Milliseconds (250), Signal.Fire'Access);

   delay until Clock + Milliseconds (500);
   Put_Line ("Main: done");
end Timing_Events_Demo;