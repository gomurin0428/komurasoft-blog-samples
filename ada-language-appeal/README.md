# Ada言語の魅力 ── サンプルコード

ブログ記事「[Ada言語の魅力 ── 型で設計を語り、数十年動き続けるソフトウェアを支える言語](https://comcomponent.com/blog/2026/06/12/000-ada-language-appeal/)」のサンプルコードです。

Ada は、航空機のフライトコントロールや鉄道の信号システムなど、高信頼性が求められる分野で 40 年以上使われ続けている言語です。記事では、強い型付け、範囲制約、パッケージによる仕様と実装の分離、契約による設計(Pre/Post)、言語組み込みのタスクと保護オブジェクト、SPARK による形式検証、GNAT と Alire での開発環境を整理しています。

このフォルダは、記事に登場する Ada のコード断片を**章ごとに整理した参照用コード集**です。各ファイルは独立したトピックを説明するためのスニペットで、冒頭コメントで文脈を補足しています。1 ファイルに複数のコンパイル単位(パッケージ仕様・本体・メイン手続き)を含むものがあるため、ビルドする際は `gnatchop` で分割します(手順は後述)。

## 構成

```
ada-language-appeal/
└── src/snippets/                       章ごとに整理した参照用スニペット
    ├── 04_hello_world.ada              Hello, WorldとAdaの基本形(記事 4 章)
    ├── 05_readable_syntax.ada          ループ、case文の網羅性、名前付き引数(記事 5 章)
    ├── 06_strong_typing.ada            別名の型は別の型。単位の混同を防ぐ(記事 6 章)
    ├── 07_range_constraints.ada        範囲制約、述語、固定小数点型(記事 7 章)
    ├── 08_arrays_and_indexing.ada      列挙型添字と境界チェック(記事 8 章)
    ├── 09_packages_counters.ada        パッケージの仕様と本体、private型(記事 9 章)
    ├── 10_records_and_discriminants.ada レコードと判別子(記事 10 章)
    ├── 11_generics.ada                 ジェネリクスと要求操作の明示(記事 11 章)
    ├── 12_exception_handling.ada       例外処理とConstraint_Error(記事 12 章)
    ├── 13_design_by_contract.ada       Pre/Post契約付きスタック(記事 13 章)
    ├── 14_tasks.ada                    タスクとランデブー(entry/accept)(記事 14 章)
    ├── 15_protected_objects.ada        保護オブジェクトによる排他制御(記事 15 章)
    ├── 16_spark_increment.ada          SPARK_Mode付きパッケージ(記事 16 章)
    └── 17_c_interop.ada                Windows APIのSleepをImportで呼ぶ(記事 17 章)
```

## 試し方

### gnatchop + gnatmake で個別に試す

GNAT(GCC の Ada コンパイラ)があれば、スニペットを 1 つずつ試せます。

```sh
mkdir work && cd work
gnatchop ../src/snippets/13_design_by_contract.ada
gnatmake -gnata contracts_demo
./contracts_demo
```

- `gnatchop` は、1 ファイルに複数のコンパイル単位を含むスニペットを、GNAT の命名規則(ユニット名 = ファイル名)に沿ったファイルへ分割します
- `-gnata` は Pre/Post などのアサーションを実行時チェックとして有効にするオプションです

### Alire で試す

記事 18 章の通り、[Alire](https://alire.ada.dev/) を使う場合は `alr init --bin demo` で雛形を作り、`src/` に分割後のファイルを置いて `alr build` してください。

## 検証状態について

このコード集は、Linux 上の GNAT 13(GCC)で **全 14 スニペットのコンパイルが通ることを確認済み**です。実行可能な 12 本(04〜15)は実行して期待通りの出力になることも確認しています。残る 2 本のうち、`16_spark_increment.ada` の証明には別途 SPARK ツール(gnatprove)が、`17_c_interop.ada` のリンクと実行には Windows 環境が必要なため、これらはコンパイル確認のみです。

## ポイント

- 構造が同じでも別名で宣言した型は別の型 ── 単位の取り違えがコンパイルエラーになる
- 範囲制約(`range 0 .. 100`)により、不正な値を型のレベルで防げる
- 配列アクセスは常に境界チェックされ、範囲外は未定義動作ではなく `Constraint_Error`
- パッケージの仕様(.ads)だけ読めば使い方が分かる。引数モード(`in` / `out` / `in out`)でデータの流れが明示される
- `case` 文は網羅必須 ── 列挙型に値を追加すると影響箇所をコンパイラが列挙してくれる
- Ada 2012 の契約(`Pre` / `Post`)は実行時チェックにも、SPARK による静的証明の入力にもなる
- タスクと保護オブジェクトにより、並行処理と排他制御を言語機能として安全に書ける

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/12/000-ada-language-appeal/)をご覧ください。
