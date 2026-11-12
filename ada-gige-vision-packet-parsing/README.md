# GigE Vision のパケットを Ada の型で受ける ── サンプルコード

ブログ記事「[GigE VisionのパケットをAdaの型で受ける ── 表現句の落とし穴とSPARKで証明するパーサー](https://comcomponent.com/blog/ada-gige-vision-packet-parsing/)」のサンプルコードです。

GVSP（GigE Vision Streaming Protocol）のヘッダーを、表現句と `Scalar_Storage_Order` で重ねて読む方法の落とし穴を示し、バイト列から長さを確かめながら読むパーサーを SPARK で書いて、範囲外を読まないことを GNATprove で証明します。記事に載せた Ada のコードブロックは、すべてこのフォルダのファイルにそのまま含まれています。

## 構成

```
ada-gige-vision-packet-parsing/
├── src/
│   ├── gvsp.ad[sb]           長さを確かめながら読むパーサー（SPARK）
│   ├── gvsp_layout.ads       表現句と Scalar_Storage_Order で書いたヘッダーのレコード
│   ├── byte_order_demo.adb   Scalar_Storage_Order の有無で Block_Id の値が変わる例
│   ├── overlay_demo.adb      Unchecked_Conversion と 'Valid、短いバッファでの Constraint_Error
│   └── gvsp_test.adb         パーサーの試験（典型的なパケット、切れたパケット、乱数のバイト列）
├── warn/overlay_warning.adb  番地で重ねると GNAT が警告を出す例
├── bad/
│   ├── src/gvsp.ad[sb]       拡張 ID の長さの確認を消した版
│   ├── gvsp_unchecked.gpr    消した版を証明にかけるプロジェクト（証明できないはず）
│   └── bad_run.gpr           消した版を gvsp_test につないで動かすプロジェクト（ASSERT=on/off）
├── gvsp.gpr
├── alire.toml
└── verify.sh                 ビルド・実行・証明をまとめて確かめるスクリプト
```

## 試し方

### 前提

- GNAT と gprbuild（Ubuntu なら `apt install gnat gprbuild`）
- 証明には GNATprove が必要です。[Alire](https://alire.ada.dev/) を使うと `alr` が GNAT・gprbuild・GNATprove をまとめて用意します。

### まとめて確かめる

```sh
./verify.sh
```

`alr` があれば Alire の環境でビルドし、GNATprove まで実行します。`alr` がなければ PATH 上の gprbuild でビルドと実行だけを行い、証明は飛ばします。

### 個別に動かす

```sh
alr build
./bin/byte_order_demo
./bin/overlay_demo
./bin/gvsp_test
alr exec -- gnatprove -P gvsp.gpr -u gvsp.adb --level=2 --report=all
```

## 検証状態について

- **ローカル（Ubuntu 24.04、GNAT 13.3.0、gprbuild）**: `verify.sh` のビルドと実行がすべて成功し、出力が記事に載せたものと一致することを確認済みです。長さの確認を消した版は、`-gnata` ありで `Read_U64` の事前条件、なしで添字の検査の例外になります。
- **GitHub Actions（ubuntu-24.04、Alire で入れた GNAT 16.1.0 と GNATprove 16.1.0）**: [verify-ada-machine-vision](../.github/workflows/verify-ada-machine-vision.yml) のワークフローで `verify.sh` を実行し、`gvsp.adb` のすべての検査が証明されること（記事を書いた時点では 66 件）と、長さの確認を消した版で「precondition might fail」が報告されることを確かめます。
- 結果はリトルエンディアンのホスト（x86-64）でのものです。

## ポイント

- `Scalar_Storage_Order` はバイト順を変えるが、番地で重ねると GNAT は警告を出す
- スカラーでない `Unchecked_Conversion` の結果を GNAT はビットの写しとして定義している（処理系定義で移植性はない）。不正な値は `'Valid` で確かめる
- バイト列の長さを先に確かめ、添字の計算があふれない型で読むと、範囲外を読まないことを証明しやすい
- 長さの確認を1つ消すと、GNATprove は証明できないと報告する

詳しい解説は[記事本文](https://comcomponent.com/blog/ada-gige-vision-packet-parsing/)をご覧ください。
