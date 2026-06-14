-- 06_protected_queue.ada
-- 保護オブジェクトによるリアルタイムデータ共有
-- パイプライン: Producer -> Bounded_Buffer -> Consumer

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;

procedure Protected_Queue_Demo is

   Buffer_Size : constant := 4;

   type Buf_Array is array (1 .. Buffer_Size) of Integer;

   protected Bounded_Buffer is
      pragma Priority (System.Any_Priority'Last);
      entry Put (Item : Integer);
      entry Get (Item : out Integer);
   private
      Buf    : Buf_Array;
      Count  : Natural := 0;
      Head   : Positive := 1;
      Tail   : Positive := 1;
   end Bounded_Buffer;

   protected body Bounded_Buffer is
      entry Put (Item : Integer) when Count < Buffer_Size is
      begin
         Buf (Tail) := Item;
         Tail := (Tail mod Buffer_Size) + 1;
         Count := Count + 1;
         Put_Line ("[Buffer] Put" & Integer'Image (Item) &
                   ", count=" & Natural'Image (Count));
      end Put;

      entry Get (Item : out Integer) when Count > 0 is
      begin
         Item := Buf (Head);
         Head := (Head mod Buffer_Size) + 1;
         Count := Count - 1;
         Put_Line ("[Buffer] Get" & Integer'Image (Item) &
                   ", count=" & Natural'Image (Count));
      end Get;
   end Bounded_Buffer;

   task Producer is
      pragma Priority (System.Default_Priority + 2);
      pragma Storage_Size (4 * 1024);
   end Producer;

   task Consumer is
      pragma Priority (System.Default_Priority + 1);
      pragma Storage_Size (4 * 1024);
   end Consumer;

   task body Producer is
   begin
      for I in 1 .. 6 loop
         Bounded_Buffer.Put (I);
         delay until Clock + Milliseconds (50);
      end loop;
      Put_Line ("[Producer] Done");
   end Producer;

   task body Consumer is
      Item : Integer;
   begin
      delay until Clock + Milliseconds (30);
      for I in 1 .. 6 loop
         Bounded_Buffer.Get (Item);
         Put_Line ("[Consumer] Processing" & Integer'Image (Item));
         delay until Clock + Milliseconds (80);
      end loop;
      Put_Line ("[Consumer] Done");
   end Consumer;

begin
   Put_Line ("=== Protected Queue Demo (Ceiling_Locking) ===");
   Put_Line ("Buffer size = 4; Producer every 50ms, Consumer every 80ms");
   delay until Clock + Milliseconds (800);
   Put_Line ("Main: done");
end Protected_Queue_Demo;