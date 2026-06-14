with Ada.Real_Time.Timing_Events; use Ada.Real_Time.Timing_Events;
with Ada.Real_Time;               use Ada.Real_Time;

package body Signal_Pkg is
   protected body Signal_Type is
      entry Wait_For_Event when Fired is
      begin
         Fired := False;
      end Wait_For_Event;

      procedure Fire (Event : in out Timing_Event) is
      begin
         Fired := True;
      end Fire;
   end Signal_Type;
end Signal_Pkg;
