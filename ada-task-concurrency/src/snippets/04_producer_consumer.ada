-- 04_producer_consumer.ada
-- Producer-consumer pattern using rendezvous.
-- The consumer task provides an entry for the producer to deliver items.

with Ada.Text_IO; use Ada.Text_IO;

procedure Producer_Consumer_Demo is

   task Consumer is
      entry Deliver (Item : Integer);
   end Consumer;

   task body Consumer is
      Sum : Integer := 0;
   begin
      for I in 1 .. 5 loop
         accept Deliver (Item : Integer) do
            Sum := Sum + Item;
            Put_Line ("  [Consumer] Received" & Integer'Image (Item)
                      & ", running total =" & Integer'Image (Sum));
         end Deliver;
      end loop;
      Put_Line ("  [Consumer] Final sum =" & Integer'Image (Sum));
   end Consumer;

   task Producer;

   task body Producer is
   begin
      for I in 1 .. 5 loop
         Put_Line ("[Producer] Sending" & Integer'Image (I));
         Consumer.Deliver (I);
      end loop;
      Put_Line ("[Producer] Done.");
   end Producer;

begin
   null;
end Producer_Consumer_Demo;