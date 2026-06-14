with Ada.Real_Time.Timing_Events; use Ada.Real_Time.Timing_Events;
with System;                      use System;

package Signal_Pkg is
   protected type Signal_Type is
      pragma Priority (System.Interrupt_Priority'Last);
      entry Wait_For_Event;
      procedure Fire (Event : in out Timing_Event);
   private
      Fired : Boolean := False;
   end Signal_Type;

   S : Signal_Type;
end Signal_Pkg;
