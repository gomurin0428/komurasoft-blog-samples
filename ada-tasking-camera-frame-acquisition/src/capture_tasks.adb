with Interfaces;     use Interfaces;
with Ada.Real_Time;  use Ada.Real_Time;
with Frame_Pool;
with Ticks;
with Capture_Config;  use Capture_Config;
with Capture_Stats;  use Capture_Stats;

package body Capture_Tasks with SPARK_Mode is

   task body Camera is
      Next     : Time := Clock;
      Now      : Time;
      Slot     : Slot_Index;
      Found    : Boolean;
      Accepted : Boolean;
      Returned : Boolean;
      Left     : Count_Type;
   begin
      for Frame in 1 .. Frames loop
         Next := Ticks.Add (Next, Period);
         delay until Next;
         if Wait_For_Slot then
            Free.Take (Slot);   --  空きが出るまで待つ方式（比較用）
            Found := True;
         else
            Free.Try_Take (Slot, Found);
         end if;
         if Found then
            Frame_Pool.Fill (Slot, Frame, Next);
            Ready.Put (Slot, Accepted);
            if not Accepted then
               Free.Put (Slot, Returned);
            end if;
         else
            Accepted := False;
         end if;
         Now := Clock;
         Stats.Arrived (Frame, Ticks.Since (Now, Next), Dropped => not Accepted);
      end loop;

      --  すべてのスロットが空きに戻るまで待ってから終わりを知らせる
      loop
         Left := Free.Length;
         exit when Left = Capacity;
         Next := Ticks.Add (Next, Period);
         delay until Next;
      end loop;
      Stats.Finish;

      loop
         delay until Time_Last;
      end loop;
   end Camera;

   procedure Simulate_Work (Frame : Frame_Number; Started : Time) is
      Cost : constant Time_Span :=
        (if Burst_Every > 0 and then Frame mod Burst_Every = 0
         then Burst_Work else Work);
      Deadline : constant Time := Ticks.Add (Started, Cost);
      Now : Time;
   begin
      if Spin then
         loop
            Now := Clock;
            exit when Now >= Deadline;
         end loop;
      else
         delay until Deadline;
      end if;
   end Simulate_Work;

   task body Worker is
      Slot     : Slot_Index;
      Frame    : Frame_Number;
      Arrival  : Time;
      Started  : Time;
      Sum      : Unsigned_32;
      Accepted : Boolean;
   begin
      loop
         Ready.Take (Slot);
         Started := Clock;
         Frame_Pool.Inspect (Slot, Frame, Arrival, Sum);
         Simulate_Work (Frame, Started);
         --  記録してからスロットを返す。先に返すと、カメラのタスクがすべての
         --  スロットが空いたのを見て、この記録より前に終わりを知らせうる。
         Stats.Processed
           (Frame, Ticks.Since (Started, Arrival),
            Intact => Sum = Frame_Pool.Expected_Checksum (Frame));
         Free.Put (Slot, Accepted);
      end loop;
   end Worker;

end Capture_Tasks;
