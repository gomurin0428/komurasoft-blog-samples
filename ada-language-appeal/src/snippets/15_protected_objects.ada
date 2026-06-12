--  記事 15 章「保護オブジェクト ── 排他制御を型として書く」のコード断片。
--
--  保護オブジェクトのデータには、定義した操作経由でしかアクセス
--  できず、排他制御は言語が保証する。
--    procedure  読み書き可、排他的に実行される
--    function   読み取り専用、複数タスクの同時実行を許す
--  「ロックを取り忘れたコード」はそもそも書けない。

with Ada.Text_IO;

procedure Protected_Objects_Demo is

   protected Shared_Counter is
      procedure Increment;
      function  Value return Natural;
   private
      Count : Natural := 0;
   end Shared_Counter;

   protected body Shared_Counter is

      procedure Increment is
      begin
         Count := Count + 1;
      end Increment;

      function Value return Natural is
      begin
         return Count;
      end Value;

   end Shared_Counter;

   --  複数のタスクから同時にIncrementしても安全
   task type Incrementer;

   task body Incrementer is
   begin
      for I in 1 .. 1000 loop
         Shared_Counter.Increment;
      end loop;
   end Incrementer;

begin
   declare
      Workers : array (1 .. 4) of Incrementer;
      pragma Unreferenced (Workers);
   begin
      null;  --  declareブロックの終わりで全タスクの完了を待つ
   end;

   Ada.Text_IO.Put_Line ("count:" & Natural'Image (Shared_Counter.Value));
end Protected_Objects_Demo;
