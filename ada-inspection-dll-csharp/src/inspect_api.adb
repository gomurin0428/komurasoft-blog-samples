with Inspect_Core;
with System; use type System.Address;

package body Inspect_API is

   use type Interfaces.Unsigned_64;
   use type Interfaces.Integer_64;

   Elaboration_Done : int := 0;  --  パッケージ本体の初期化で 1 にする

   function Inspect_Dark_Defects
     (Pixels : System.Address;
      Length : size_t;
      Width  : int;
      Height : int;
      Stride : int;
      Params : Params_Access;
      Result : Result_Access) return int
   is
   begin
      if Result = null then
         return Status_Null_Pointer;
      end if;

      --  どの戻り方でも、呼び出し側が読む結果を決まった値にしておく
      Result.all :=
        (Defect_Pixels => 0, Min_X | Min_Y | Max_X | Max_Y => -1, Verdict => 1);

      if Pixels = System.Null_Address or else Params = null then
         return Status_Null_Pointer;
      end if;

      if Width <= 0 or else Height <= 0 or else Stride < Width then
         return Status_Bad_Geometry;
      end if;

      --  最後の行も stride バイトぶんあることを求める
      if Interfaces.Unsigned_64 (Stride) * Interfaces.Unsigned_64 (Height)
         > Interfaces.Unsigned_64 (Length)
      then
         return Status_Buffer_Too_Small;
      end if;

      --  パラメーターの範囲は、例外に頼らず if で確かめる
      if Params.Threshold not in 0 .. 255
        or else Params.Max_Defect_Pixels < 0
      then
         return Status_Bad_Parameter;
      end if;

      declare
         --  範囲は上で確かめたので、この変換で例外は起きない
         Threshold   : constant Inspect_Core.Byte :=
           Inspect_Core.Byte (Params.Threshold);
         Max_Defects : constant Natural := Natural (Params.Max_Defect_Pixels);

         --  呼び出し側のバッファを、コピーせずに (行, 列) の配列として見る
         Img : Inspect_Core.Image
                 (0 .. Natural (Height) - 1, 0 .. Natural (Stride) - 1)
           with Import, Address => Pixels;

         S : Inspect_Core.Summary;
      begin
         Inspect_Core.Count_Dark (Img, Positive (Width), Threshold, S);

         Result.all :=
           (Defect_Pixels => S.Defect_Pixels,
            Min_X         => int (S.Min_X),
            Min_Y         => int (S.Min_Y),
            Max_X         => int (S.Max_X),
            Max_Y         => int (S.Max_Y),
            Verdict       =>
              (if S.Defect_Pixels <= Interfaces.Integer_64 (Max_Defects)
               then 0 else 1));
         return Status_OK;
      end;
   exception
      when others =>
         --  想定していない失敗。呼び出し側は検査を止めて扱う
         return Status_Internal_Error;
   end Inspect_Dark_Defects;

   function Result_Size return int is
     (int (Inspect_Result'Max_Size_In_Storage_Elements));

   function Elaborated return int is (Elaboration_Done);

begin
   Elaboration_Done := 1;
end Inspect_API;
