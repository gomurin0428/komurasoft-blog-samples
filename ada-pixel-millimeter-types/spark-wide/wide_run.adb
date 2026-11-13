with Ada.Text_IO;      use Ada.Text_IO;
with Ada.Command_Line; use Ada.Command_Line;
with Scaled_Length;    use Scaled_Length;

procedure Wide_Run is
   --  値は実行時に引数から受け取る（例: 1000000.0 1.0）
   L : constant Px_Length := Px_Length'Value (Argument (1));
   S : constant Mm_Per_Px := Mm_Per_Px'Value (Argument (2));
begin
   Put_Line (Mm'Image (To_Mm (L, S)));
end Wide_Run;
