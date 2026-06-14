-- 05_timing_events.ada
-- タイミングイベント (Ada.Real_Time.Timing_Events)
-- 高優先度タスクをポーリングなしで起床させる仕組み

with Ada.Text_IO;               use Ada.Text_IO;
with Ada.Real_Time.Timing_Events; use Ada.Real_Time.Timing_Events;

package Signal_Pkg is
   protected type Signal_Type is
      entry Wait_For_Event;
      procedure Fire (Event : in out Timing_Event);
   private
      Fired : Boolean := False;
   end Signal_Type;

   S : Signal_Type;
end Signal_Pkg;

package body Signal_Pkg is
   protected body Signal_Type is
      entry Wait_For_Event when Fired is
      begin
         Fired := False;
         Put_Line ("  [Signal] Event handler woke up");
      end Wait_For_Event;

      procedure Fire (Event : in out Timing_Event) is
      begin
         Fired := True;
         Put_Line ("  [Signal] Timing event fired");
      end Fire;
   end Signal_Type;
end Signal_Pkg;

with Signal_Pkg; use Signal_Pkg;

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;
with Ada.Real_Time.Timing_Events; use Ada.Real_Time.Timing_Events;

procedure Timing_Events_Demo is

   pragma Priority (29);

   Timer_1 : Timing_Event;
   Timer_2 : Timing_Event;

   task Reactor is
      pragma Priority (System.Default_Priority + 5);
      pragma Storage_Size (4 * 1024);
   end Reactor;

   task body Reactor is
   begin
      Put_Line ("[Reactor] Waiting for timing events...");

      S.Wait_For_Event;
      Put_Line ("[Reactor] Got event #1");

      S.Wait_For_Event;
      Put_Line ("[Reactor] Got event #2");

      Put_Line ("[Reactor] Done");
   end Reactor;

begin
   Put_Line ("=== Timing Events Demo ===");
   Put_Line ("Scheduling two timers at +100ms and +250ms...");

   Set_Handler (Timer_1, Clock + Milliseconds (100), S.Fire'Access);
   Set_Handler (Timer_2, Clock + Milliseconds (250), S.Fire'Access);

   delay until Clock + Milliseconds (500);
   Put_Line ("Main: done");
end Timing_Events_Demo;
