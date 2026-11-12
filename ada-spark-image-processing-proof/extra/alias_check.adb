with Ada.Text_IO; use Ada.Text_IO;
with Ada.Command_Line;
with Images;    use Images;
with Image_Ops; use Image_Ops;

--  入力と出力に同じ画像を渡す（SPARK の外から呼ぶと起こりうる）。
--  -gnateA を付けたビルドでは Program_Error になるはず。
procedure Alias_Check is
   A : Image (0 .. 3, 0 .. 3) := (others => (others => 0));
begin
   A (1, 1) := 90;
   A (2, 2) := 180;
   Smooth_3x3 (A, A);
   Put_Line ("UNEXPECTED: the aliased call returned");
   Ada.Command_Line.Set_Exit_Status (Ada.Command_Line.Failure);
exception
   when Program_Error =>
      Put_Line ("EXPECTED: Program_Error for aliased parameters");
end Alias_Check;
