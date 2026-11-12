package body Capture_Stats with SPARK_Mode is

   protected body Stats is

      procedure Arrived (Frame : Frame_Number; Lateness : Time_Span; Dropped : Boolean) is
      begin
         Late (Frame) := Lateness;
         Drop (Frame) := Dropped;
      end Arrived;

      procedure Processed (Frame : Frame_Number; Latency : Time_Span; Intact : Boolean) is
      begin
         Lat (Frame) := Latency;
         Processed_Flag (Frame) := True;
         Intact_Flag (Frame) := Intact;
      end Processed;

      procedure Finish is
      begin
         Done := True;
      end Finish;

      entry Wait_Done when Done is
      begin
         null;
      end Wait_Done;

      function Lateness (Frame : Frame_Number) return Time_Span is (Late (Frame));
      function Latency (Frame : Frame_Number) return Time_Span is (Lat (Frame));
      function Dropped (Frame : Frame_Number) return Boolean is (Drop (Frame));
      function Done_Frame (Frame : Frame_Number) return Boolean is (Processed_Flag (Frame));
      function Intact (Frame : Frame_Number) return Boolean is (Intact_Flag (Frame));

   end Stats;

end Capture_Stats;
