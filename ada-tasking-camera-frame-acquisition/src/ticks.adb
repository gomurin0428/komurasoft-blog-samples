package body Ticks with SPARK_Mode => Off is

   function Add (T : Time; D : Time_Span) return Time is (T + D);

   function Since (Later, Earlier : Time) return Time_Span is (Later - Earlier);

end Ticks;
