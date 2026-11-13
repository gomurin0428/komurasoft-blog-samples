# 画像処理の境界外アクセスを SPARK で証明する ── サンプルコード

ブログ記事「[画像処理の境界外アクセスをSPARKで証明する ── 2値化・平滑化・ラベリングとCとの速度比較](https://comcomponent.com/blog/ada-spark-image-processing-proof/)」のサンプルコードです。

2値化、3×3 の平滑化、連結成分のラベリングを SPARK で書き、添字や範囲の検査が事前条件を満たすどの入力でも失敗しないことを GNATprove で証明します。同じ処理を C でも書き、結果の一致と速度を比べます。記事に載せた Ada のコードブロックは、すべてこのフォルダのファイルにそのまま含まれています。

## 構成

```
ada-spark-image-processing-proof/
├── types/images.ads          画像の型（行と列の範囲を持つ2次元配列）
├── src/
│   ├── image_ops.ad[sb]      2値化と 3×3 の平滑化（SPARK）
│   └── labeling.ad[sb]       連結成分のラベリング（SPARK、4近傍、2回走査）
├── c/vision.c                同じ処理の C 版（比較用）
├── test/
│   ├── test_ops.adb          小さな画像の期待値、仮ラベルが足りない画像、C との一致
│   ├── bench.adb             Ada と C の速度の比較（引数を省略すると 1920x1200）
│   ├── c_vision.ads          C 版の関数の宣言
│   └── test_support.ad[sb]   画像の確保と擬似乱数の画像
├── extra/
│   ├── oracle.adb            乱数の小さな画像2万個で、塗りつぶしと 3×3 の平均の直接計算と比べる
│   ├── stair.adb             斜めの縞の画像（3000×3000 は成功、4096×4096 は仮ラベルが足りず失敗）
│   └── alias_check.adb       入力と出力に同じ画像を渡すと -gnateA で Program_Error になる
├── bug/smooth_bug.ad[sb]     わざと誤りを入れた平滑化（GNATprove が添字の検査を証明できない）
├── img_proof.gpr             本体のプロジェクト（BUILD=test/checks/nochecks、OPT=-O2/-O3）
├── extra.gpr                 extra/ の試験のプロジェクト
├── bug.gpr                   bug/ を証明にかけるプロジェクト
├── clang.gpr                 速度の比較用（C だけを clang でコンパイルする）
├── alire.toml
└── verify.sh                 ビルド・試験・証明をまとめて確かめるスクリプト
```

## 試し方

### 前提

- GNAT と gprbuild（Ubuntu なら `apt install gnat gprbuild`）
- 証明には GNATprove が必要です。[Alire](https://alire.ada.dev/) を使うと `alr` が GNAT・gprbuild・GNATprove をまとめて用意します。

### まとめて確かめる

```sh
./verify.sh
```

`alr` があれば Alire の環境でビルドし、GNATprove まで実行します。`alr` がなければ PATH 上の gprbuild でビルドと試験だけを行い、証明は飛ばします。

### 個別に動かす

```sh
alr build
./bin/test-O2/test_ops
alr exec -- gnatprove -P img_proof.gpr -j0 --level=2 --report=all
```

証明できたユニットだけ検査を外したビルドと、速度の比較:

```sh
alr exec -- gprbuild -P img_proof.gpr -XBUILD=nochecks -XOPT=-O2
./bin/nochecks-O2/bench
```

## 検証状態について

- **ローカル（Ubuntu 24.04、GNAT 13.3.0、gprbuild）**: `verify.sh` のビルドと試験（test_ops、oracle、stair、alias_check、検査を外したビルドの test_ops）がすべて成功することを確認済みです。
- **GitHub Actions（ubuntu-24.04、Alire で入れた GNAT 16.1.0 と GNATprove 16.1.0）**: [verify-ada-machine-vision](../.github/workflows/verify-ada-machine-vision.yml) のワークフローで `verify.sh` を実行し、`img_proof.gpr` で「Success: all checks proved」が出ること、`bug.gpr` で添字の検査が証明されないことを確かめます。記事を書いた時点では 107 件の検査がすべて証明されました。
- 速度の数値は環境で変わります。記事の数値は GNAT 13.3.0（GCC 13.3.0）で測ったものです。

## ポイント

- 画像の型に行と列の範囲を持たせ、入力と出力の範囲が等しいことと、画像が空でないことを事前条件に書く
- ラベルの対応表の添字を型で制限すれば、ラベリングでも実行時エラーがないことを証明できる
- 上限がかかるのは塊の数ではなく仮ラベルの数。上限を超える画像では `Success = False` を返す
- 証明したユニットの仕様に `pragma Assertion_Policy (Pre => Check)` を書き、SPARK の外から呼ばれる場合に備えて事前条件の検査を残す
- 速さを比べる前に、Ada と C を同じコンパイラーと同じスイッチでコンパイルしているかを確かめる

詳しい解説は[記事本文](https://comcomponent.com/blog/ada-spark-image-processing-proof/)をご覧ください。
