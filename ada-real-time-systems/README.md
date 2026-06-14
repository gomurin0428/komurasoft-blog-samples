# Adaによるリアルタイムシステムプログラミング ── サンプルコード

ブログ記事「[Adaによるリアルタイムシステムプログラミング ── 優先度・周期・実行時間制御の実践](https://comcomponent.com/blog/2026/06/14/001-ada-real-time-systems/)」のサンプルコードです。

Ada の Annex D（Real-Time Systems）は、リアルタイム機能を言語仕様そのものに組み込んでいます。タスク優先度とプリエンプション、Ceiling_Locking による優先度逆転防止、`delay until` によるドリフトフリー周期実行、Ravenscar プロファイル、タイミングイベント、保護キュー、タスク別 CPU 時間計測──これらを 8 つの実践的なスニペットで段階的に学べます。

このフォルダは、記事に登場する Ada のコード断片を**章ごとに整理した参照用コード集**です。各ファイルは独立したトピックを説明するためのスニペットで、一部のファイルは複数のコンパイル単位(パッケージ仕様・本体・メイン手続き)を含むため、ビルドする際は `gnatchop` で分割します(手順は後述)。

## 構成

```
ada-real-time-systems/
└── src/snippets/                             章ごとに整理した参照用スニペット
    ├── 01_task_priority.ada                   タスク優先度と FIFO_Within_Priorities の基本形(記事 3 章)
    ├── 02_ceiling_locking.ada                 Ceiling_Locking プロトコルによる優先度逆転の防止(記事 4 章)
    ├── 03_periodic_task.ada                   delay until による周期タスク ── 累積ドリフトを防ぐ(記事 5 章)
    ├── 04_ravenscar_profile.ada               Ravenscar プロファイルの基本形(記事 6 章)
    ├── 05_timing_events.ada                   タイミングイベント ── ポーリングなしの時刻起床(記事 7 章)
    ├── 06_protected_queue.ada                 保護オブジェクトによるリアルタイムデータ共有(記事 8 章)
    ├── 07_execution_time.ada                  実行時間制御 ── タスクごとの CPU 消費時間を計測(記事 9 章)
    └── 08_multiperiodic.ada                   マルチ周期リアルタイムシステムの統合デモ(記事 10 章)
```

## 試し方

### 前提

GNAT (GCC の Ada コンパイラ) が必要です。Linux では `apt install gnat-13`、Windows では MSYS2 の `mingw-w64-x86_64-gcc-ada` や [Alire](https://alire.ada.dev/) で導入できます。

### gnatchop + gnatmake で個別に試す

スニペットを 1 つずつ試す手順です。

```sh
mkdir work && cd work
gnatchop ../src/snippets/01_task_priority.ada
gnatmake task_priority_demo
./task_priority_demo
```

全スニペットをまとめてコンパイル・実行する場合:

```sh
mkdir work && cd work
for f in ../src/snippets/*.ada; do
    echo "=== $(basename $f) ==="
    gnatchop "$f"
done
gnatmake -gnata *.adb
for exe in task_priority_demo ceiling_locking_demo periodic_task_demo \
           ravenscar_demo timing_events_demo protected_queue_demo \
           execution_time_demo multiperiodic_demo; do
    echo "=== Running $exe ==="
    ./$exe
done
```

- `gnatchop` は、1 ファイルに複数のコンパイル単位を含むスニペットを GNAT の命名規則(ユニット名 = ファイル名)に沿ったファイルへ分割します
- `-gnata` はアサーションを実行時チェックとして有効にします

### Ravenscar プロファイルを有効にして試す

`04_ravenscar_profile.ada` を Ravenscar プロファイル下で動作させるには、`gnat.adc` ファイルを作成します:

```sh
echo 'pragma Profile (Ravenscar);' > gnat.adc
```

その上で `gnatmake` に `-gnatec=gnat.adc` を渡します:

```sh
gnatmake -gnatec=gnat.adc ravenscar_demo
./ravenscar_demo
```

Ravenscar プロファイルが適用されると、動的タスク生成、複数 `select` 代替、`abort` 文などがコンパイル時に禁止されます。本スニペットは Ravenscar 準拠で記述されているため、プロファイル有効時もそのままコンパイルが通ります。

### Alire で試す

[Alire](https://alire.ada.dev/) を使う場合は `alr init --bin demo` で雛形を作り、`src/` に分割後のファイルを置いて `alr build` してください。

## 検証状態について

このコード集は、Windows 上の GNAT 15.2.1（Alire ツールチェーン）で **全 8 スニペットのコンパイル・実行を確認済み** です。すべてのスニペットが期待通りの出力を生成することを確認しています。

各スニペットは Annex D (Real-Time Systems) の完全なサポートを前提とします。Annex D が部分的にしかサポートされていない環境では、優先度やタイミングイベントの動作が異なる場合があります。

## ポイント

- `pragma Priority` でタスクに静的優先度を付与。`FIFO_Within_Priorities` ポリシーにより高優先度タスクが低優先度タスクをプリエンプトする
- 保護オブジェクトに `pragma Priority` を設定すると **Ceiling_Locking** が有効になり、進入中のタスクがシーリング優先度に昇格──優先度逆転が発生しない
- `delay until Next_Release; Next_Release := Next_Release + Period;` のイディオムで絶対時刻基準の周期実行を実現。累積ドリフトが起きない
- Ravenscar プロファイルはタスク機能を静的に解析可能なサブセットに制限。DO-178C や ISO 26262 の認証基盤として設計されている
- `Ada.Real_Time.Timing_Events.Set_Handler` で絶対時刻に保護プロシージャの呼び出しを登録。ポーリング不要で、ハンドラはシーリング優先度で実行される
- 保護エントリのバリア (`when Count < Buffer_Size`) により、バッファ満杯/空のブロッキングを言語が自動処理。ミューテックスも条件変数も不要
- `Ada.Execution_Time.Clock` はブロック中やプリエンプト中を除外した **実際の CPU 消費時間** を返す。WCET 分析の基礎となる

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/14/001-ada-real-time-systems/)をご覧ください。
