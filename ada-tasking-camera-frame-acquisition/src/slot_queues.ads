with Capture_Priorities;

package Slot_Queues with SPARK_Mode is

   Capacity : constant := 8;

   type Slot_Index is range 1 .. Capacity;
   type Count_Type is range 0 .. Capacity;
   type Ring_Index is mod Capacity;
   type Slot_Ring is array (Ring_Index) of Slot_Index;

   protected type Slot_Queue (Initially_Full : Boolean)
     with Priority => Capture_Priorities.Camera_Priority
   is
      procedure Put (Slot : Slot_Index; Accepted : out Boolean);
      procedure Try_Take (Slot : out Slot_Index; Found : out Boolean);
      entry Take (Slot : out Slot_Index);
      function Length return Count_Type;
      function Max_Length return Count_Type;
   private
      Items : Slot_Ring := [for I in Ring_Index => Slot_Index (Integer (I) + 1)];
      Head  : Ring_Index := 0;
      Count : Count_Type := (if Initially_Full then Capacity else 0);
      Peak  : Count_Type := (if Initially_Full then Capacity else 0);
   end Slot_Queue;

end Slot_Queues;
