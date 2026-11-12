with Ada.Text_IO; use Ada.Text_IO;
with Ada.Command_Line;
with Images;   use Images;
with Labeling; use Labeling;

--  (X + Y) mod 3 /= 2 の画素を前景にした斜めの縞の画像。
--  塊は少ないが仮ラベルは多いので、4096×4096 では仮ラベルが足りなくなる。
procedure Stair is
   type Image_Access is access Image;
   type Label_Access is access Label_Image;
   type Parent_Access is access Parent_Table;

   Failures : Natural := 0;

   procedure Run
     (W, H : Positive; Expect_Ok : Boolean; Expect_Count : Natural)
   is
      Src : constant Image_Access :=
        new Image'(0 .. H - 1 => (0 .. W - 1 => 0));
      Lab : constant Label_Access :=
        new Label_Image'(0 .. H - 1 => (0 .. W - 1 => 0));
      Par : constant Parent_Access := new Parent_Table'(others => 1);
      Count : Natural;
      Ok    : Boolean;
   begin
      for Y in Src'Range (1) loop
         for X in Src'Range (2) loop
            Src (Y, X) := (if (X + Y) mod 3 /= 2 then 255 else 0);
         end loop;
      end loop;
      Label_Components (Src.all, Lab.all, Par.all, Count, Ok);
      Put_Line (W'Image & " x" & H'Image & ": Success = " & Ok'Image
                & ", Count =" & Count'Image);
      if Ok /= Expect_Ok or else (Ok and then Count /= Expect_Count) then
         Put_Line ("  UNEXPECTED");
         Failures := Failures + 1;
      end if;
   end Run;
begin
   Run (3000, 3000, Expect_Ok => True, Expect_Count => 2000);
   Run (4096, 4096, Expect_Ok => False, Expect_Count => 0);
   if Failures /= 0 then
      Ada.Command_Line.Set_Exit_Status (Ada.Command_Line.Failure);
   end if;
end Stair;
