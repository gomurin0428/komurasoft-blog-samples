--  記事 17 章「CやC++との相互運用」のコード断片。
--
--  Cとの相互運用は言語仕様(Annex B)で標準化されている。
--    Import        外部の実装を取り込む
--    Convention    呼び出し規約(C、Stdcallなど)を指定する
--    External_Name リンク時のシンボル名を指定する
--  Interfaces.C はCの型に対応する型を提供する。
--
--  ※ この例はWindows APIのSleepを呼ぶため、リンクできるのは
--    Windows環境のみ(コンパイル自体は他環境でも通る)。

with Interfaces.C;

procedure Sleep_Demo is

   procedure Sleep (Milliseconds : Interfaces.C.unsigned)
     with Import,
          Convention    => Stdcall,
          External_Name => "Sleep";

begin
   Sleep (1000);
end Sleep_Demo;
