package body Slot_Queues with SPARK_Mode is

   protected body Slot_Queue is

      procedure Put (Slot : Slot_Index; Accepted : out Boolean) is
      begin
         if Count = Capacity then
            Accepted := False;
         else
            Items (Head + Ring_Index'Mod (Count)) := Slot;
            Count := Count + 1;
            if Count > Peak then
               Peak := Count;
            end if;
            Accepted := True;
         end if;
      end Put;

      procedure Try_Take (Slot : out Slot_Index; Found : out Boolean) is
      begin
         if Count = 0 then
            Slot := Slot_Index'First;
            Found := False;
         else
            Slot := Items (Head);
            Head := Head + 1;
            Count := Count - 1;
            Found := True;
         end if;
      end Try_Take;

      entry Take (Slot : out Slot_Index) when Count > 0 is
      begin
         Slot := Items (Head);
         Head := Head + 1;
         Count := Count - 1;
      end Take;

      function Length return Count_Type is (Count);

      function Max_Length return Count_Type is (Peak);

   end Slot_Queue;

end Slot_Queues;
