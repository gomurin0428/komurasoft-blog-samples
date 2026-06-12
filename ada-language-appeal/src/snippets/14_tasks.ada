--  記事 14 章「タスク ── 並行処理が言語に組み込まれている」のコード断片。
--
--  taskを宣言すると、囲んでいるブロックの開始と同時に並行実行が
--  始まる。ブロックは内部のタスクがすべて終わるまで終了しないため、
--  「joinを忘れる」類いのバグが構造的に起きない。
--  タスク間の同期にはランデブー(entry / accept)が使える。
--  select ... or terminate により、他のタスクが終わったら
--  Loggerタスクも終了する。

with Ada.Text_IO;

procedure Tasks_Demo is

   task Logger is
      entry Write (Message : String);
   end Logger;

   task body Logger is
   begin
      loop
         select
            accept Write (Message : String) do
               Ada.Text_IO.Put_Line (Message);
            end Write;
         or
            terminate;
         end select;
      end loop;
   end Logger;

   task Worker;

   task body Worker is
   begin
      for I in 1 .. 3 loop
         Logger.Write ("worker:" & Integer'Image (I));
         delay 0.1;
      end loop;
   end Worker;

begin
   for I in 1 .. 3 loop
      Logger.Write ("main  :" & Integer'Image (I));
      delay 0.1;
   end loop;
   --  ここでWorkerの完了を待ち、その後Loggerがterminateする
end Tasks_Demo;
