# ピクセルとミリメートルを型で分ける ── サンプルコード

ブログ記事「[ピクセルとミリメートルを型で分ける ── Adaで寸法検査の単位の取り違えを防ぐ](https://comcomponent.com/blog/ada-pixel-millimeter-types/)」のサンプルコードです。

寸法検査で起きやすい単位の取り違え（ピクセルとミリメートル、x 方向と y 方向、座標と長さ）を、Ada の型と演算子の宣言でコンパイルエラーにする例です。公差の判定には10進固定小数点型を使い、GNAT 独自の次元の検査（`Dimension_System`）と、固定小数点型の範囲を GNATprove で証明する例も含みます。記事に載せた Ada のコードブロックは、すべてこのフォルダのファイルにそのまま含まれています。

## 構成

```
ada-pixel-millimeter-types/
├── src/                        動く例（units.gpr）
│   ├── vision_units.ad[sb]     ミリメートルの型と、意味のない演算の削除
│   ├── vision_units-axis.ad[sb] 軸ごとの座標・長さ・校正係数の型（ジェネリック）
│   ├── x_axis.ads / y_axis.ads x 方向と y 方向の実体
│   ├── tolerance.ads           0.001 mm 刻みの10進固定小数点型と公差の判定（Accept_Rounded）
│   ├── vision_dims.ads         GNAT の Dimension_System で mm と x・y のピクセルを次元にする
│   ├── naive_check.adb         Long_Float だけで書いた例（取り違えてもコンパイルが通る）
│   ├── measure_width.adb       ゲージで校正して幅を測る例
│   ├── tolerance_check.adb     上限ちょうどの値の判定と、型変換と 'Round の違い
│   ├── small_check.adb         delta だけを書いたときの small
│   ├── dim_check.adb           次元つきの値の表示
│   └── area_check.adb          長さ × 長さを面積の型で返す演算子
├── errors/                     コンパイルエラーになるはずの例（errors.gpr）
├── spark/scaled_length.ad[sb]  0.01 px 刻みの長さを mm に直す（prove.gpr、範囲を証明できる）
├── spark-wide/                 画像の幅の上限を広げた版（prove_wide.gpr、証明できず実行時に Constraint_Error）
├── limits/                     型で防げないものの例（limits.gpr、trunc.gpr）
│   ├── hole_literals.adb       型のないリテラルの係数が -gnatwa でも警告なしで通る
│   ├── hole_px.adb             ピクセルの長さを Accept_Rounded に渡すとエラーになる
│   └── trunc_run.adb           10進ではない固定小数点型への型変換の丸め方
├── units.gpr / errors.gpr / prove.gpr / prove_wide.gpr / limits.gpr / trunc.gpr
├── alire.toml
└── verify.sh                   ビルド・実行・エラーの例・証明をまとめて確かめるスクリプト
```

## 試し方

### 前提

- GNAT と gprbuild（Ubuntu なら `apt install gnat gprbuild`）
- 証明には GNATprove が必要です。[Alire](https://alire.ada.dev/) を使うと `alr` が GNAT・gprbuild・GNATprove をまとめて用意します。

### まとめて確かめる

```sh
./verify.sh
```

`alr` があれば Alire の環境でビルドし、GNATprove まで実行します。`alr` がなければ PATH 上の gprbuild でビルド・実行・エラーの例だけを確かめ、証明は飛ばします。

### 個別に動かす

```sh
gprbuild -P units.gpr
./bin/tolerance_check
gprbuild -c -u -P errors.gpr err_px_times_px.adb    # エラーになるはず
alr exec -- gnatprove -P prove.gpr --level=2 --report=all
```

## 検証状態について

- **ローカル（Ubuntu 24.04、GNAT 13.3.0、gprbuild）**: `verify.sh` のビルド、実行結果、8つのエラーの例のメッセージ、型で防げないものの例、範囲を広げた版の Constraint_Error を確認済みです。エラーの例のメッセージと実行結果は、記事に載せたものと一致します。
- **GitHub Actions（ubuntu-24.04、Alire で入れた GNAT 16.1.0 と GNATprove 16.1.0）**: [verify-ada-machine-vision](../.github/workflows/verify-ada-machine-vision.yml) のワークフローで `verify.sh` を実行し、`prove.gpr` の検査がすべて証明されること（記事を書いた時点では 3 件）と、`prove_wide.gpr` で「range check might fail」が報告されることを確かめます。
- エラーメッセージの文言と行・桁は GNAT のバージョンで変わることがあります。`verify.sh` の判定に使う文字列は GNAT 13.3.0 の出力から取ったもので、GNAT 16.1.0 でも同じになることは CI で確かめます。

## ポイント

- 座標・長さ・校正係数・ミリメートルを別の型にし、意味のない演算子は `abstract` で消す
- x 方向と y 方向はジェネリックの実体を分けて、別の型にする
- 公差の判定は10進固定小数点型で行い、浮動小数点数から直すときは `'Round` で丸める
- 型のないリテラルの係数、校正係数の値の誤り、レンズのゆがみは型では防げない

詳しい解説は[記事本文](https://comcomponent.com/blog/ada-pixel-millimeter-types/)をご覧ください。
