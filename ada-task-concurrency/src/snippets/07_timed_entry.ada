-- 07_timed_entry.ada
-- Timed entry call: wait for a rendezvous with a timeout.
-- Uses `select ... or delay` to avoid indefinite blocking.

with Ada.Text_IO;       use Ada.Text_IO;
with Ada.Real_Time;     use Ada.Real_Time;

procedure Timed_Entry_Demo is

   task Slow_Worker is
      entry Do_Work (Result : out Boolean);
   end Slow_Worker;

   task body Slow_Worker is
   begin
      Put_Line ("  [Slow_Worker] Starting 2-second task...");
      delay 2.0;
      Put_Line ("  [Slow_Worker] Ready.");
      accept Do_Work (Result : out Boolean) do
         Result := True;
      end Do_Work;
   end Slow_Worker;

   use type Ada.Real_Time.Time;

   Deadline : constant Ada.Real_Time.Time_Span := Milliseconds (500);
   Start    : constant Ada.Real_Time.Time := Ada.Real_Time.Clock;
   Result   : Boolean := False;
begin
   Put_Line ("Main: calling Slow_Worker.Do_Work with 500ms timeout...");

   select
      Slow_Worker.Do_Work (Result);
      Put_Line ("Main: work completed, result = " & Boolean'Image (Result));
   or
      delay until Ada.Real_Time.Clock + Deadline;
      Put_Line ("Main: timeout after 500ms!");
   end select;

   Put_Line ("Main: elapsed ="
             & Duration'Image (To_Duration (Ada.Real_Time.Clock - Start))
             & " seconds");
end Timed_Entry_Demo;