with Ada.Text_IO;   use Ada.Text_IO;
with Ada.Real_Time; use Ada.Real_Time;
with Ada.Command_Line;
with Interfaces.C;  use Interfaces.C;
with Images;        use Images;
with Image_Ops;     use Image_Ops;
with Labeling;      use Labeling;
with Test_Support;  use Test_Support;
with C_Vision;

--  Ada と C の同じ処理の速度を比べる。引数を省略すると 1920x1200
procedure Bench is

   W : constant Positive :=
     (if Ada.Command_Line.Argument_Count >= 2
      then Positive'Value (Ada.Command_Line.Argument (1)) else 1920);
   H : constant Positive :=
     (if Ada.Command_Line.Argument_Count >= 2
      then Positive'Value (Ada.Command_Line.Argument (2)) else 1200);

   Rounds : constant := 7;

   type Times is array (1 .. Rounds) of Duration;

   function Median (T : Times) return Duration is
      S   : Times := T;
      Tmp : Duration;
   begin
      for I in S'Range loop
         for J in I + 1 .. S'Last loop
            if S (J) < S (I) then
               Tmp := S (I); S (I) := S (J); S (J) := Tmp;
            end if;
         end loop;
      end loop;
      return S ((Rounds + 1) / 2);
   end Median;

   Src      : constant Image_Access := New_Image (W, H);
   Ada_Out  : constant Image_Access := New_Image (W, H);
   C_Out    : constant Image_Access := New_Image (W, H);
   Bin      : constant Image_Access := New_Image (W, H);
   Labels   : constant Label_Image_Access := New_Labels (W, H);
   C_Labels : constant Label_Image_Access := New_Labels (W, H);
   Parent   : constant Parent_Access := new Parent_Table'(others => 1);
   C_Parent : constant C_Parent_Access := new C_Parent_Table'(others => 0);

   Count   : Natural := 0;
   C_Count : int := 0;
   Ok      : Boolean := False;

   procedure Report (Name : String; Iterations : Positive; A, C : Times) is
      Ms_A : constant Float := Float (Median (A)) * 1000.0 / Float (Iterations);
      Ms_C : constant Float := Float (Median (C)) * 1000.0 / Float (Iterations);
   begin
      Put_Line (Name & ": Ada" & Float'Image (Ms_A) & " ms, C" & Float'Image (Ms_C)
                & " ms, Ada/C" & Float'Image (Ms_A / Ms_C));
   end Report;

   T_A, T_C : Times;
   Start    : Time;

begin
   Put_Line ("image" & Positive'Image (W) & " x" & Positive'Image (H)
             & ", median of" & Integer'Image (Rounds) & " rounds");
   Fill_Random (Src.all, 2_026);

   --  2値化
   for R in 1 .. Rounds loop
      Start := Clock;
      for I in 1 .. 200 loop
         Binarize (Src.all, 128, Ada_Out.all);
      end loop;
      T_A (R) := To_Duration (Clock - Start);
      Start := Clock;
      for I in 1 .. 200 loop
         C_Vision.Binarize (Src.all'Address, C_Out.all'Address, int (W), int (H), 128);
      end loop;
      T_C (R) := To_Duration (Clock - Start);
   end loop;
   Report ("binarize", 200, T_A, T_C);
   Put_Line ("  same result: " & Boolean'Image (Ada_Out.all = C_Out.all));

   --  3x3 の平滑化
   for R in 1 .. Rounds loop
      Start := Clock;
      for I in 1 .. 100 loop
         Smooth_3x3 (Src.all, Ada_Out.all);
      end loop;
      T_A (R) := To_Duration (Clock - Start);
      Start := Clock;
      for I in 1 .. 100 loop
         C_Vision.Smooth_3x3 (Src.all'Address, C_Out.all'Address, int (W), int (H));
      end loop;
      T_C (R) := To_Duration (Clock - Start);
   end loop;
   Report ("smooth_3x3", 100, T_A, T_C);
   Put_Line ("  same result: " & Boolean'Image (Ada_Out.all = C_Out.all));

   --  ラベリング: 平滑化を2回かけてから2値化し、大小の塊がある画像にする
   Smooth_3x3 (Src.all, Ada_Out.all);
   Smooth_3x3 (Ada_Out.all, C_Out.all);
   Binarize (C_Out.all, 128, Bin.all);
   for R in 1 .. Rounds loop
      Start := Clock;
      for I in 1 .. 20 loop
         Label_Components (Bin.all, Labels.all, Parent.all, Count, Ok);
      end loop;
      T_A (R) := To_Duration (Clock - Start);
      Start := Clock;
      for I in 1 .. 20 loop
         C_Count := C_Vision.Label_Components
           (Bin.all'Address, C_Labels.all'Address, C_Parent.all'Address,
            int (W), int (H), int (Max_Labels));
      end loop;
      T_C (R) := To_Duration (Clock - Start);
   end loop;
   Report ("labeling", 20, T_A, T_C);
   Put_Line ("  components:" & Natural'Image (Count) & ", same result: "
             & Boolean'Image (Ok and then int (Count) = C_Count
                              and then Labels.all = C_Labels.all));
end Bench;
