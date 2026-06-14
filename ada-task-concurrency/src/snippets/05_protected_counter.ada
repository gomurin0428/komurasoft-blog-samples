-- 05_protected_counter.ada
-- Protected objects provide mutual exclusion without explicit locks.
-- Functions (read-only) can run concurrently; procedures (read-write) are exclusive.

with Ada.Text_IO; use Ada.Text_IO;

procedure Protected_Counter_Demo is

   Num_Workers : constant := 3;

   protected Counter is
      procedure Increment;
      procedure Mark_Done;
      entry All_Done;
      function Value return Integer;
   private
      Count      : Integer := 0;
      Done_Count : Integer := 0;
   end Counter;

   protected body Counter is
      procedure Increment is
      begin
         Count := Count + 1;
      end Increment;

      procedure Mark_Done is
      begin
         Done_Count := Done_Count + 1;
      end Mark_Done;

      entry All_Done when Done_Count = Num_Workers is
      begin
         null;
      end All_Done;

      function Value return Integer is
      begin
         return Count;
      end Value;
   end Counter;

   task type Worker (Id : Integer; Rounds : Integer);

   task body Worker is
   begin
      for I in 1 .. Rounds loop
         Counter.Increment;
      end loop;
      Counter.Mark_Done;
   end Worker;

   W1 : Worker (1, 1_000);
   W2 : Worker (2, 1_000);
   W3 : Worker (3, 1_000);
begin
   Counter.All_Done;
   Put_Line ("Final counter value =" & Integer'Image (Counter.Value));
end Protected_Counter_Demo;