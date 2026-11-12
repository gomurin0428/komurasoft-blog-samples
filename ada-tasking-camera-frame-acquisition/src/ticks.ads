with Ada.Real_Time; use Ada.Real_Time;

--  時刻の足し算と引き算。時刻が型の上限に近づいたときにあふれないことを
--  GNATprove は示せないので、本体を SPARK の検査の外に置く。
package Ticks with SPARK_Mode is

   function Add (T : Time; D : Time_Span) return Time
   with Global => null;

   function Since (Later, Earlier : Time) return Time_Span
   with Global => null;

end Ticks;
