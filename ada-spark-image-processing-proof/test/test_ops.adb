with Ada.Text_IO;      use Ada.Text_IO;
with Ada.Command_Line;
with Ada.Assertions;
with Interfaces.C;
with Images;           use Images;
with Image_Ops;        use Image_Ops;
with Labeling;         use Labeling;
with Test_Support;     use Test_Support;
with C_Vision;

procedure Test_Ops is

   Failures : Natural := 0;

   procedure Check (Condition : Boolean; Name : String) is
   begin
      if Condition then
         Put_Line ("PASS " & Name);
      else
         Put_Line ("FAIL " & Name);
         Failures := Failures + 1;
      end if;
   end Check;

   procedure Run_Labeling
     (Source : Image; Labels : out Label_Image_Access;
      Count : out Natural; Success : out Boolean)
   is
      Parent : constant Parent_Access := new Parent_Table'(others => 1);
   begin
      Labels := New_Labels (Source'Length (2), Source'Length (1));
      Label_Components (Source, Labels.all, Parent.all, Count, Success);
   end Run_Labeling;

   function From_Rows (Rows : Image) return Image is (Rows);

begin
   --  2値化
   declare
      Src : constant Image := (0 => (10, 128, 200), 1 => (127, 0, 255));
      Dst : Image (0 .. 1, 0 .. 2) := (others => (others => 7));
   begin
      Binarize (Src, 128, Dst);
      Check (Dst = Image'(0 => (0, 255, 255), 1 => (0, 0, 255)), "binarize 2x3");
   end;

   --  平滑化: 縁はそのまま、内側は周りの9画素の平均
   declare
      Src : constant Image :=
        (0 => (9, 9, 9, 9), 1 => (9, 0, 18, 9),
         2 => (9, 9, 9, 9), 3 => (9, 9, 9, 9));
      Dst : Image (0 .. 3, 0 .. 3) := (others => (others => 0));
   begin
      Smooth_3x3 (Src, Dst);
      Check (Dst (0, 0) = 9 and then Dst (1, 1) = 9 and then Dst (1, 2) = 9
               and then Dst (2, 1) = 9 and then Dst (3, 3) = 9,
             "smooth 4x4");
   end;

   --  空の画像は事前条件で弾く（範囲が Integer'Last から始まる空の配列）
   declare
      Src : Image (Integer'Last .. Integer'Last - 1, 0 .. 0);
      Dst : Image (Integer'Last .. Integer'Last - 1, 0 .. 0);
   begin
      Smooth_3x3 (Src, Dst);
      Check (False, "smooth rejects empty image");
   exception
      when Ada.Assertions.Assertion_Error =>
         Check (True, "smooth rejects empty image");
   end;

   --  ラベリング: U の字は1つ、離れた2つの塊は2つ、市松模様は4近傍で画素ごとに別
   declare
      U_Shape : constant Image :=
        From_Rows ((0 => (1, 0, 1), 1 => (1, 0, 1), 2 => (1, 1, 1)));
      Two     : constant Image :=
        From_Rows ((0 => (1, 1, 0, 0), 1 => (0, 0, 0, 1)));
      Checker : constant Image :=
        From_Rows ((0 => (1, 0, 1, 0), 1 => (0, 1, 0, 1),
                    2 => (1, 0, 1, 0), 3 => (0, 1, 0, 1)));
      Labels  : Label_Image_Access;
      Count   : Natural;
      Ok      : Boolean;
   begin
      Run_Labeling (U_Shape, Labels, Count, Ok);
      Check (Ok and then Count = 1
               and then Labels (0, 0) = Labels (0, 2)
               and then Labels (0, 2) = Labels (2, 1),
             "label U shape");
      Run_Labeling (Two, Labels, Count, Ok);
      Check (Ok and then Count = 2, "label two blobs");
      Run_Labeling (Checker, Labels, Count, Ok);
      Check (Ok and then Count = 8, "label checkerboard 4x4");
   end;

   --  ラベルが足りなくなる画像: 4096x4096 の市松模様は 8,388,608 個の成分を持つ
   declare
      Big    : constant Image_Access := New_Image (Max_Width, Max_Height);
      Labels : Label_Image_Access;
      Count  : Natural;
      Ok     : Boolean;
   begin
      for Y in Big'Range (1) loop
         for X in Big'Range (2) loop
            Big (Y, X) := (if (X + Y) mod 2 = 0 then 1 else 0);
         end loop;
      end loop;
      Run_Labeling (Big.all, Labels, Count, Ok);
      Check (not Ok, "label overflow reports failure");
   end;

   --  C で書いた同じ処理と結果を比べる
   declare
      use Interfaces.C;
      W : constant := 64;
      H : constant := 48;
      Src      : constant Image_Access := New_Image (W, H);
      Ada_Out  : constant Image_Access := New_Image (W, H);
      C_Out    : constant Image_Access := New_Image (W, H);
      Smoothed : constant Image_Access := New_Image (W, H);
      Bin      : constant Image_Access := New_Image (W, H);
      Labels   : Label_Image_Access;
      C_Labels : constant Label_Image_Access := New_Labels (W, H);
      C_Parent : constant C_Parent_Access := new C_Parent_Table;
      Count    : Natural;
      Ok       : Boolean;
      C_Count  : int;
   begin
      Fill_Random (Src.all, 12_345);

      Binarize (Src.all, 128, Ada_Out.all);
      C_Vision.Binarize (Src.all'Address, C_Out.all'Address, W, H, 128);
      Check (Ada_Out.all = C_Out.all, "binarize matches C");

      Smooth_3x3 (Src.all, Ada_Out.all);
      C_Vision.Smooth_3x3 (Src.all'Address, C_Out.all'Address, W, H);
      Check (Ada_Out.all = C_Out.all, "smooth matches C");

      Smooth_3x3 (Ada_Out.all, Smoothed.all);
      Binarize (Smoothed.all, 128, Bin.all);
      Run_Labeling (Bin.all, Labels, Count, Ok);
      C_Count := C_Vision.Label_Components
        (Bin.all'Address, C_Labels.all'Address, C_Parent.all'Address,
         W, H, int (Max_Labels));
      Check (Ok and then int (Count) = C_Count and then Labels.all = C_Labels.all,
             "labeling matches C (" & Natural'Image (Count) & " components)");
   end;

   if Failures > 0 then
      Put_Line (Natural'Image (Failures) & " test(s) failed");
      Ada.Command_Line.Set_Exit_Status (Ada.Command_Line.Failure);
   else
      Put_Line ("all tests passed");
   end if;
end Test_Ops;
