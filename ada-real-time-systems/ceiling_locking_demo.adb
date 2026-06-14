-- 02_ceiling_locking.ada
-- Ceiling_Locking プロトコルによる優先度逆転の防止

pragma Locking_Policy (Ceiling_Locking);

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;

procedure Ceiling_Locking_Demo is

   Max_Prio : constant System.Any_Priority := System.Any_Priority'Last;

   protected Shared_Data is
      pragma Priority (Max_Prio);
      procedure Write (V : Integer);
      function Read return Integer;
   private
      Value : Integer := 0;
   end Shared_Data;

   protected body Shared_Data is
      procedure Write (V : Integer) is
      begin
         Value := V;
         Put_Line ("  [Protected] Written: " & Integer'Image (V));
      end Write;

      function Read return Integer is
      begin
         Put_Line ("  [Protected] Read: " & Integer'Image (Value));
         return Value;
      end Read;
   end Shared_Data;

   task Producer is
      pragma Priority (Max_Prio - 1);
      pragma Storage_Size (4 * 1024);
   end Producer;

   task Consumer is
      pragma Priority (Priority'First);
      pragma Storage_Size (4 * 1024);
   end Consumer;

   task body Producer is
   begin
      Put_Line ("[T=0.0s] Producer (high prio): about to write");
      Shared_Data.Write (42);
      Put_Line ("[T=0.0s] Producer (high prio): write done");
      delay until Clock + Milliseconds (100);
   end Producer;

   task body Consumer is
   begin
      delay until Clock + Milliseconds (10);
      Put_Line ("[T=0.01s] Consumer (low prio): about to read");
      declare
         V : Integer;
      begin
         V := Shared_Data.Read;
         Put_Line ("[T=0.01s] Consumer (low prio): read done, got" &
                     Integer'Image (V));
      end;
      delay until Clock + Milliseconds (100);
   end Consumer;

begin
   Put_Line ("=== Ceiling_Locking Demo ===");
   Put_Line ("Main: producer priority = Max-1, consumer priority = First");
   Put_Line ("Ceiling = Max, so neither can block the other inside PO");
   delay until Clock + Milliseconds (300);
   Put_Line ("Main: done");
end Ceiling_Locking_Demo;