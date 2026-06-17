# Adaのジェネリックプログラミング ── サンプルコード

ブログ記事「[Adaのジェネリックプログラミング ── 型安全な再利用をゼロコストで実現する仕組み](https://comcomponent.com/blog/ada-generic-programming/)」のサンプルコードです。

Ada のジェネリックは、型やサブプログラム、値を仮パラメータとして受け取ることで、型安全なコード再利用をゼロ実行時コストで実現します。記事では、総称サブプログラムから型カテゴリ指定、述語注入、複数パラメータの合成までを体系的に解説しています。

## 構成

```
ada-generic-programming/
└── src/snippets/
    ├── 01_swap.ada         総称サブプログラム — 任意の型で動作するSwap（記事 3 章）
    ├── 02_stack.ada        総称パッケージ — 要素型と容量をパラメータとする汎用スタック（記事 4 章）
    ├── 03_sort.ada         仮サブプログラムパラメータ — 比較関数を注入する挿入ソート（記事 5 章）
    ├── 04_statistics.ada   型カテゴリ指定 — digits <> で任意の浮動小数点型に対応（記事 6 章）
    ├── 05_filter.ada       述語注入 — Predicate を受け取る Generic_Count_If（記事 7 章）
    └── 06_kv_store.ada     複数パラメータの合成 — 汎用キーバリューストア（記事 8 章）
```

## 試し方

### 前提

GNAT (GCC の Ada コンパイラ) が必要です。Linux では `apt install gnat`、Windows や macOS では [Alire](https://alire.ada.dev/) で導入できます。

### gnatchop + gnatmake で個別に試す

```bash
mkdir work && cd work
gnatchop ../src/snippets/01_swap.ada
gnatmake -gnata swap_demo
./swap_demo
```

`-gnata` はアサーションを有効にします。

### 全スニペットを一括ビルド

```bash
mkdir work && cd work
for f in ../src/snippets/*.ada; do
    gnatchop "$f"
done
gnatmake -gnata *.adb
for exe in swap_demo stack_demo sort_demo statistics_demo filter_demo kv_store_demo; do
    echo "=== $exe ==="
    ./$exe
done
```

### Alire で試す

```bash
alr init --bin demo
# 分割後の .ads/.adb ファイルを src/ に配置
alr build
```

## 検証状態

Windows 上の **GNAT 15.2.1**（Alire ツールチェーン）で**全 6 スニペットのコンパイル・実行を確認済み**です。

| # | スニペット | 主手続き | 出力 |
|---|-----------|---------|------|
| 01 | swap | swap_demo | 整数/文字のスワップ前後 |
| 02 | stack | stack_demo | Push/Pop とスタックサイズ表示 |
| 03 | sort | sort_demo | 昇順/降順ソート結果 |
| 04 | statistics | statistics_demo | Float/Long_Float の平均・分散 |
| 05 | filter | filter_demo | 偶数カウント・50超カウント |
| 06 | kv_store | kv_store_demo | キー指定による値取得・存在確認 |

## ポイント

- ジェネリックはマクロではなく、**コンパイル時に完全に型解決**される（ゼロ実行時コスト）
- 仮パラメータは `is private`（任意）、`is (<>)`（離散型）、`is digits <>`（浮動小数点型）など**型カテゴリ**で制約できる
- 仮サブプログラムパラメータ（`with function "<" ... is <>;`）で**振る舞いを注入**可能
- `is <>` は省略時のデフォルト、`:=` は値のデフォルトを指定する
- ジェネリック本体は**それ自体として型チェック**される（C++テンプレートと異なり、インスタンス化前に正当性が保証される）

詳しい解説は[記事本文](https://comcomponent.com/blog/ada-generic-programming/)をご覧ください。
