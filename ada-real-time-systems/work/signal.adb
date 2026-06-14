
protected body Signal is
   entry Wait_For_Event when Fired is
   begin
      Fired := False;
      Put_Line ("  [Signal] Event handler woke up");
   end Wait_For_Event;

   procedure Fire is
   begin
      Fired := True;
      Put_Line ("  [Signal] Timing event fired");
   end Fire;
end Signal;
