with Ada.Real_Time;  use Ada.Real_Time;
with Capture_Config; use Capture_Config;
with Capture_Priorities; use Capture_Priorities;

package Capture_Stats with SPARK_Mode is

   type Span_Array is array (Frame_Number) of Time_Span;
   type Flag_Array is array (Frame_Number) of Boolean;

   protected Stats with Priority => Camera_Priority is
      procedure Arrived (Frame : Frame_Number; Lateness : Time_Span; Dropped : Boolean);
      procedure Processed (Frame : Frame_Number; Latency : Time_Span; Intact : Boolean);
      procedure Finish;
      entry Wait_Done;
      function Lateness (Frame : Frame_Number) return Time_Span;
      function Latency (Frame : Frame_Number) return Time_Span;
      function Dropped (Frame : Frame_Number) return Boolean;
      function Done_Frame (Frame : Frame_Number) return Boolean;
      function Intact (Frame : Frame_Number) return Boolean;
   private
      Late      : Span_Array := [others => Time_Span_Zero];
      Lat       : Span_Array := [others => Time_Span_Zero];
      Drop      : Flag_Array := [others => False];
      Processed_Flag : Flag_Array := [others => False];
      Intact_Flag    : Flag_Array := [others => True];
      Done      : Boolean := False;
   end Stats;

end Capture_Stats;
