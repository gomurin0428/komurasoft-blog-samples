with Ada.Real_Time; use Ada.Real_Time;

package Capture_Config with
  SPARK_Mode,
  Initializes => (Frames, Period, Work, Burst_Every, Burst_Work, Spin, Wait_For_Slot)
is
   pragma Elaborate_Body;

   Max_Frames : constant := 5_000;
   type Frame_Number is range 0 .. Max_Frames;

   --  コマンドラインの値で上書きする（本体の初期化部）
   Frames      : Frame_Number := 1_000 with Constant_After_Elaboration;
   Period      : Time_Span := Milliseconds (10) with Constant_After_Elaboration;
   Work        : Time_Span := Milliseconds (5) with Constant_After_Elaboration;
   Burst_Every : Frame_Number := 0 with Constant_After_Elaboration;
   Burst_Work  : Time_Span := Milliseconds (0) with Constant_After_Elaboration;
   Spin        : Boolean := True with Constant_After_Elaboration;
   Wait_For_Slot : Boolean := False with Constant_After_Elaboration;

end Capture_Config;
