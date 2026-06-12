--  記事 16 章「SPARK ── 形式検証への道」のコード断片。
--
--  SPARKはAdaのサブセットで、プログラムの性質を数学的に証明できる。
--  GNATproveは、このコードに対してオーバーフローが起きないこと、
--  PreとPostの整合性などを「実行せずに」証明する。
--    テスト: 選んだ入力に対して正しく動くことを確認する
--    証明  : すべての入力に対して性質が成り立つことを示す
--
--  検証するには SPARK ツール(gnatprove)が必要:
--    alr with gnatprove などで導入し、gnatprove を実行する

package Spark_Demo
  with SPARK_Mode
is

   procedure Increment (X : in out Integer)
     with Pre  => X < Integer'Last,
          Post => X = X'Old + 1;

end Spark_Demo;

package body Spark_Demo
  with SPARK_Mode
is

   procedure Increment (X : in out Integer) is
   begin
      X := X + 1;
   end Increment;

end Spark_Demo;
