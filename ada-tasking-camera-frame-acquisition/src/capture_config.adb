with Ada.Command_Line; use Ada.Command_Line;
with Ada.Strings.Fixed; use Ada.Strings.Fixed;

package body Capture_Config with SPARK_Mode => Off is
begin
   --  引数は key=value の形: frames=1000 period_ms=10 work_ms=5
   --  burst_every=50 burst_ms=60 mode=spin|sleep policy=drop|wait
   for I in 1 .. Argument_Count loop
      declare
         Arg   : constant String := Argument (I);
         Eq    : constant Natural := Index (Arg, "=");
         Key   : constant String := Arg (Arg'First .. Eq - 1);
         Value : constant String := Arg (Eq + 1 .. Arg'Last);
      begin
         if Key = "frames" then
            Frames := Frame_Number'Value (Value);
         elsif Key = "period_ms" then
            Period := Milliseconds (Integer'Value (Value));
         elsif Key = "work_ms" then
            Work := Milliseconds (Integer'Value (Value));
         elsif Key = "burst_every" then
            Burst_Every := Frame_Number'Value (Value);
         elsif Key = "burst_ms" then
            Burst_Work := Milliseconds (Integer'Value (Value));
         elsif Key = "mode" then
            Spin := Value = "spin";
         elsif Key = "policy" then
            Wait_For_Slot := Value = "wait";
         else
            raise Constraint_Error with "unknown argument: " & Arg;
         end if;
      end;
   end loop;
end Capture_Config;
