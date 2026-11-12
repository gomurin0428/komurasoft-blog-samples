with Ada.Text_IO;   use Ada.Text_IO;
with Scaled_Length; use Scaled_Length;

procedure Trunc_Run is
begin
   Put_Line (Mm'Image (To_Mm (0.19, 0.01)));
   Put_Line (Mm'Image (To_Mm (510.55, 0.01999)));
end Trunc_Run;
