with Slot_Queues;    use Slot_Queues;
with Capture_Priorities; use Capture_Priorities;

package Capture_Tasks with SPARK_Mode is

   --  空いているスロットの番号と、画像が入ったスロットの番号
   Free  : Slot_Queue (Initially_Full => True);
   Ready : Slot_Queue (Initially_Full => False);

   task Camera with Priority => Camera_Priority;
   task Worker with Priority => Worker_Priority;

end Capture_Tasks;
