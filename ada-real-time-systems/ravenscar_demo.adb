-- 04_ravenscar_profile.ada
-- Ravenscar プロファイルの基本形
-- コンパイル時に -gnatec=ravenscar.adc で pragma Profile (Ravenscar); を指定する

with Ravenscar_Pkg;
with Ada.Text_IO;               use Ada.Text_IO;
with Ada.Real_Time;             use Ada.Real_Time;

procedure Ravenscar_Demo is
begin
   Put_Line ("=== Ravenscar Profile Demo ===");
   Put_Line ("(compile with: gnatmake -gnat2022 -gnatec=ravenscar.adc ravenscar_demo)");
   Put_Line ("Main: waiting for Ravenscar tasks...");
   delay until Clock + Milliseconds (800);
   Put_Line ("Main: done");
end Ravenscar_Demo;
