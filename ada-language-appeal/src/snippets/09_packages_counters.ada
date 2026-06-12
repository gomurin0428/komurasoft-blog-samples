--  記事 9 章「パッケージ ── 仕様と実装の分離」のコード断片。
--
--  パッケージは仕様(spec / .ads)と本体(body / .adb)に分かれる。
--  type を private と宣言すると、利用側は内部構造に触れない。
--  引数のモード(in / out / in out)により、データの流れる方向が
--  シグネチャを見るだけで分かる。
--
--  ※ 実際のGNATプロジェクトでは counters.ads / counters.adb の
--    2ファイルに分ける(gnatchopで分割できる形で収録している)。

package Counters is

   type Counter is private;

   procedure Increment (C : in out Counter);
   function  Value     (C : Counter) return Natural;

private
   type Counter is record
      Count : Natural := 0;
   end record;

end Counters;

package body Counters is

   procedure Increment (C : in out Counter) is
   begin
      C.Count := C.Count + 1;
   end Increment;

   function Value (C : Counter) return Natural is
   begin
      return C.Count;
   end Value;

end Counters;

--  利用側。仕様(.ads)だけ読めば使い方がすべて分かる

with Ada.Text_IO;
with Counters;

procedure Packages_Demo is
   C : Counters.Counter;
begin
   Counters.Increment (C);
   Counters.Increment (C);
   Ada.Text_IO.Put_Line ("count:" & Natural'Image (Counters.Value (C)));
end Packages_Demo;
