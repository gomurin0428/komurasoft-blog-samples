with Interfaces;
with Interfaces.C; use Interfaces.C;
with System;

package Inspect_API is

   --  struct inspect_params { int threshold; int max_defect_pixels; };
   type Inspect_Params is record
      Threshold         : int;  --  これより暗い画素を欠陥とみなす (0〜255)
      Max_Defect_Pixels : int;  --  欠陥の画素数がこれを超えたら不合格 (0以上)
   end record
     with Convention => C;

   --  struct inspect_result { int64_t defect_pixels;
   --    int min_x, min_y, max_x, max_y; int verdict; };
   type Inspect_Result is record
      Defect_Pixels : Interfaces.Integer_64;
      Min_X, Min_Y  : int;  --  欠陥の外接矩形。欠陥がなければ -1
      Max_X, Max_Y  : int;
      Verdict       : int;  --  0 = 合格、1 = 不合格
   end record
     with Convention => C;

   type Params_Access is access constant Inspect_Params
     with Convention => C;
   type Result_Access is access all Inspect_Result
     with Convention => C;

   Status_OK               : constant int := 0;
   Status_Null_Pointer     : constant int := 1;
   Status_Bad_Geometry     : constant int := 2;
   Status_Buffer_Too_Small : constant int := 3;
   Status_Bad_Parameter    : constant int := 4;
   Status_Internal_Error   : constant int := 99;

   --  int inspect_dark_defects (const uint8_t *pixels, size_t length,
   --                            int width, int height, int stride,
   --                            const inspect_params *params,
   --                            inspect_result *result);
   function Inspect_Dark_Defects
     (Pixels : System.Address;
      Length : size_t;
      Width  : int;
      Height : int;
      Stride : int;
      Params : Params_Access;
      Result : Result_Access) return int
     with Export, Convention => C, External_Name => "inspect_dark_defects";

   --  int inspect_result_size (void);  呼び出し側の構造体と大きさを比べる
   function Result_Size return int
     with Export, Convention => C, External_Name => "inspect_result_size";

   --  int inspect_elaborated (void);  初期化が済んでいれば 1
   function Elaborated return int
     with Export, Convention => C, External_Name => "inspect_elaborated";

end Inspect_API;
