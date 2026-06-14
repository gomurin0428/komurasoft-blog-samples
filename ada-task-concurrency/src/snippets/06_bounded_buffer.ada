-- 06_bounded_buffer.ada
-- Protected entry with barrier: tasks block until the condition is true.
-- This is the classic bounded buffer (synchronized queue).

with Ada.Text_IO; use Ada.Text_IO;

procedure Bounded_Buffer_Demo is

   Buffer_Size : constant := 3;
   subtype Buffer_Index is Integer range 0 .. Buffer_Size - 1;
   type Buffer_Array is array (Buffer_Index) of Integer;

   protected Buf is
      entry Put (Item : Integer);
      entry Get (Item : out Integer);
   private
      Data    : Buffer_Array;
      Head    : Integer := 0;
      Tail    : Integer := 0;
      Count   : Integer := 0;
   end Buf;

   protected body Buf is
      entry Put (Item : Integer) when Count < Buffer_Size is
      begin
         Data (Tail) := Item;
         Tail := (Tail + 1) mod Buffer_Size;
         Count := Count + 1;
         Put_Line ("  [Buf] Put" & Integer'Image (Item) & ", count =" & Integer'Image (Count));
      end Put;

      entry Get (Item : out Integer) when Count > 0 is
      begin
         Item := Data (Head);
         Head := (Head + 1) mod Buffer_Size;
         Count := Count - 1;
         Put_Line ("  [Buf] Get" & Integer'Image (Item) & ", count =" & Integer'Image (Count));
      end Get;
   end Buf;

   task Producer;
   task Consumer;

   task body Producer is
   begin
      for I in 1 .. 5 loop
         delay 0.1;
         Buf.Put (I);
      end loop;
   end Producer;

   task body Consumer is
      Item : Integer;
   begin
      for I in 1 .. 5 loop
         Buf.Get (Item);
         Put_Line ("  [Consumer] Processing" & Integer'Image (Item));
      end loop;
   end Consumer;

begin
   null;
end Bounded_Buffer_Demo;