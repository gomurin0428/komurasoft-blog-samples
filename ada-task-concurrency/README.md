# Adaにおける安全な並行処理 ── サンプルコード

ブログ記事「[Adaにおける安全な並行処理 ── タスクと保護オブジェクトの実践ガイド](https://comcomponent.com/blog/2026/06/14/000-ada-task-concurrency/)」のサンプルコードです。

Ada は並行処理を言語仕様に組み込んだ稀有な言語です。タスク(task)、ランデブー(entry/accept)、保護オブジェクト(protected object)により、ミューテックスや条件変数を手動で操作することなく安全な並行処理を記述できます。記事では、基本から境界付きバッファ、タイムアウト、リアルタイム優先度までを実践的に解説しています。

このフォルダは、記事に登場する Ada のコード断片を**章ごとに整理した参照用コード集**です。各ファイルは独立したトピックを説明するためのスニペットで、一部のファイルは複数のコンパイル単位(タスク仕様・本体・メイン手続き)を含むため、ビルドする際は `gnatchop` で分割します(手順は後述)。

## 構成

```
ada-task-concurrency/
└── src/snippets/                             章ごとに整理した参照用スニペット
    ├── 01_hello_task.ada                     Hello task — タスクの基本形(記事 3 章)
    ├── 02_rendezvous_intro.ada               ランデブーによる双方向データ転送(記事 4 章)
    ├── 03_selective_accept.ada               選択的アクセプトとサーバータスク(記事 5 章)
    ├── 04_producer_consumer.ada              Producer-Consumer パターン(記事 6 章)
    ├── 05_protected_counter.ada              保護オブジェクトによる排他制御(記事 7 章)
    ├── 06_bounded_buffer.ada                 バリア付き保護エントリ — 境界付きバッファ(記事 8 章)
    ├── 07_timed_entry.ada                    タイムアウト付きselect呼び出し(記事 9 章)
    └── 08_task_priorities.ada                タスク優先度とリアルタイムスケジューリング(記事 10 章)
```

## 試し方

### 前提

GNAT (GCC の Ada コンパイラ) が必要です。Linux では `apt install gnat-13`、Windows では MSYS2 の `mingw-w64-x86_64-gcc-ada` や [Alire](https://alire.ada.dev/) で導入できます。

### gnatchop + gnatmake で個別に試す

スニペットを 1 つずつ試す手順です。

```sh
mkdir work && cd work
gnatchop ../src/snippets/01_hello_task.ada
gnatmake hello_task_demo
./hello_task_demo
```

全スニペットをまとめてコンパイル・実行する場合:

```sh
mkdir work && cd work
for f in ../src/snippets/*.ada; do
    echo "=== $(basename $f) ==="
    gnatchop "$f"
done
gnatmake -gnata *.adb
for exe in hello_task_demo rendezvous_demo selective_accept_demo \
           producer_consumer_demo protected_counter_demo \
           bounded_buffer_demo timed_entry_demo task_priorities_demo; do
    echo "=== Running $exe ==="
    ./$exe
done
```

- `gnatchop` は、1 ファイルに複数のコンパイル単位を含むスニペットを GNAT の命名規則(ユニット名 = ファイル名)に沿ったファイルへ分割します
- `-gnata` は Pre/Post などのアサーションを実行時チェックとして有効にします(本コード集ではアサーションは使用していませんが、安全のため指定しています)

### Alire で試す

[Alire](https://alire.ada.dev/) を使う場合は `alr init --bin demo` で雛形を作り、`src/` に分割後のファイルを置いて `alr build` してください。

## 検証状態について

このコード集は、Windows 上の GNAT 15.2.1（Alire ツールチェーン）および Linux 上の GNAT 13（GCC）で **全 8 スニペットのコンパイル・実行を確認済み** です。すべてのスニペットが期待通りの出力を生成することを確認しています。

`08_task_priorities.ada` は優先度と Annex D (Real-Time Systems) のサポートに依存するため、Annex D が完全にサポートされていない環境では通常のタスクとして動作します。

## ポイント

- タスクは宣言された時点で自動的に実行を開始。`join` の呼び忘れによるクラッシュがない
- ランデブー(`entry` / `accept`)は同期通信であり、双方向のデータ転送も可能
- `select` 文で複数エントリを待ち受け、ガード条件(`when`)で受付可否を宣言的に制御できる
- 保護オブジェクトの関数は同時読み取り可能、プロシージャは排他的。ロック操作は不要
- 保護エントリのバリア(`when Count < Buffer_Size`)は条件変数の while ループとシグナル通知を不要にする
- `select ... or delay until` でタイムアウト付き呼び出し。絶対時刻指定により累積ドリフトを防止
- `pragma Priority` と Ceiling_Locking により優先度逆転を防止しつつリアルタイム保証を得られる

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/14/000-ada-task-concurrency/)をご覧ください。