-- 05_timing_events.ada
-- タイミングイベント (Ada.Real_Time.Timing_Events)
-- 高優先度タスクをポーリングなしで起床させる仕組み

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;
with Ada.Real_Time.Timing_Events; use Ada.Real_Time.Timing_Events;

-- ライブラリレベルの保護オブジェクト（'Access を許可するため）
protected Signal is
   pragma Priority (System.Default_Priority + 10);
   entry Wait_For_Event;
   procedure Fire;
private
   Fired : Boolean := False;
end Signal;
