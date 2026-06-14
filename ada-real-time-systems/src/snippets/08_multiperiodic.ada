-- 08_multiperiodic.ada
-- マルチ周期リアルタイムシステムの統合デモ
-- 速い周期(100ms)のセンサー読取タスク
-- 遅い周期(400ms)の制御タスク
-- Ceiling_Locking によるデータ共有

with Ada.Text_IO;               use Ada.Text_IO;
with System;                    use System;
with Ada.Real_Time;             use Ada.Real_Time;

procedure Multiperiodic_Demo is

   package Int_IO is new Ada.Text_IO.Integer_IO (Integer);

   protected Shared_Sensor is
      pragma Priority (System.Any_Priority'Last);
      procedure Write (V : Integer);
      function Read return Integer;
   private
      Value : Integer := 0;
   end Shared_Sensor;

   protected body Shared_Sensor is
      procedure Write (V : Integer) is
      begin
         Value := V;
      end Write;

      function Read return Integer is
      begin
         return Value;
      end Read;
   end Shared_Sensor;

   task Fast_Sensor is
      pragma Priority (System.Default_Priority + 3);
      pragma Storage_Size (4 * 1024);
   end Fast_Sensor;

   task body Fast_Sensor is
      Next_Release : Time := Clock + Milliseconds (100);
      Period       : constant Time_Span := Milliseconds (100);
      Cycle        : Natural := 0;
   begin
      Put_Line ("[Fast] Sensor reader starts (100ms period)");

      for I in 1 .. 12 loop
         delay until Next_Release;
         Cycle := Cycle + 1;
         Shared_Sensor.Write (Cycle * 10);
         Next_Release := Next_Release + Period;
      end loop;
      Put_Line ("[Fast] Done");
   end Fast_Sensor;

   task Slow_Controller is
      pragma Priority (System.Default_Priority + 2);
      pragma Storage_Size (4 * 1024);
   end Slow_Controller;

   task body Slow_Controller is
      Next_Release : Time := Clock + Milliseconds (150);
      Period       : constant Time_Span := Milliseconds (400);
      Cycle        : Natural := 0;
      Raw          : Integer;
   begin
      Put_Line ("[Slow] Controller starts (400ms period)");

      for I in 1 .. 3 loop
         delay until Next_Release;
         Cycle := Cycle + 1;
         Raw := Shared_Sensor.Read;
         Put_Line ("[Slow] Cycle" & Natural'Image (Cycle) &
                   " reads sensor =" & Integer'Image (Raw));
         Next_Release := Next_Release + Period;
      end loop;
      Put_Line ("[Slow] Done");
   end Slow_Controller;

begin
   Put_Line ("=== Multiperiodic Real-Time System Demo ===");
   Put_Line ("Fast sensor (100ms) x 12 + Slow controller (400ms) x 3");
   Put_Line ("Ceiling_Locking prevents priority inversion on shared data");
   delay until Clock + Milliseconds (2000);
   Put_Line ("Main: done");
end Multiperiodic_Demo;