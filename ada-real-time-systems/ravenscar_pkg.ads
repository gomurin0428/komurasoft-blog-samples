with System;       use System;
with Ada.Real_Time; use Ada.Real_Time;

package Ravenscar_Pkg is
   protected Signal is
      pragma Priority (System.Default_Priority + 5);
      entry Wait_For_Release;
      procedure Release;
   private
      Released : Boolean := False;
   end Signal;

   task Periodic_Worker is
      pragma Priority (System.Default_Priority + 1);
      pragma Storage_Size (4 * 1024);
   end Periodic_Worker;

   task Monitor is
      pragma Priority (System.Default_Priority);
      pragma Storage_Size (4 * 1024);
   end Monitor;
end Ravenscar_Pkg;
