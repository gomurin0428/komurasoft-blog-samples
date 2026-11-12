with Interfaces;
with Ada.Real_Time;
with Slot_Queues;    use Slot_Queues;
with Capture_Config; use Capture_Config;

--  画像の領域。スロットの番号を持っているタスクだけが、そのスロットに触る。
--  この約束は SPARK では証明できないので、Synchronous と宣言して本体を検査の外に置く。
package Frame_Pool with
  SPARK_Mode,
  Abstract_State => (Pixels with Synchronous, External),
  Initializes    => Pixels
is

   procedure Fill
     (Slot    : Slot_Index;
      Frame   : Frame_Number;
      Arrival : Ada.Real_Time.Time)
   with Global => (In_Out => Pixels);

   procedure Inspect
     (Slot     : Slot_Index;
      Frame    : out Frame_Number;
      Arrival  : out Ada.Real_Time.Time;
      Checksum : out Interfaces.Unsigned_32)
   with Global => (In_Out => Pixels);

   function Expected_Checksum (Frame : Frame_Number) return Interfaces.Unsigned_32
   with Global => null;

end Frame_Pool;
