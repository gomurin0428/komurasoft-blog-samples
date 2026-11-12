# 取りこぼさない取り込み処理を Ada のタスクで作る ── サンプルコード

ブログ記事「[取りこぼさない取り込み処理をAdaのタスクで作る ── 保護オブジェクトのリングバッファと優先度](https://comcomponent.com/blog/ada-tasking-camera-frame-acquisition/)」のサンプルコードです。

カメラのタスク（優先度20、周期的に `delay until`）と処理のタスク（優先度10）を、保護オブジェクトで作ったスロットの番号のキューでつなぎ、画像はスロットの番号を持つタスクだけが触る、という形の取り込み処理の骨組みです。Jorvik プロファイルで書き、データ競合がないことと優先度上限の検査を GNATprove で確かめます。実際のカメラの代わりに、決まった周期で画像ができたことにして動かします。記事に載せた Ada のコードブロックは、すべてこのフォルダのファイルにそのまま含まれています。

## 構成

```
ada-tasking-camera-frame-acquisition/
├── src/
│   ├── capture_main.adb          引数を読んで測定し、結果を表示するメイン（SPARK の外）
│   ├── capture_tasks.ad[sb]      カメラのタスクと処理のタスク
│   ├── slot_queues.ad[sb]        空きスロットと処理待ちスロットのキュー（保護オブジェクト）
│   ├── frame_pool.ad[sb]         画像の領域（抽象状態 Pixels、Synchronous）
│   ├── capture_stats.ad[sb]      記録用の保護オブジェクト
│   ├── capture_config.ad[sb]     測定の設定（フレーム数・周期・処理時間・方針。コマンドラインの値で上書き）
│   ├── capture_priorities.ads    優先度の定数
│   └── ticks.ad[sb]              時刻の足し算と引き算（本体は SPARK の検査の外）
├── gnat.adc                      Jorvik プロファイルと Partition_Elaboration_Policy (Sequential)
├── frame_capture.gpr
├── alire.toml
└── verify.sh                     ビルド・実行・証明・反例の確認をまとめて行うスクリプト
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
./bin/capture_main frames=300 period_ms=10 work_ms=5
./bin/capture_main frames=300 period_ms=10 work_ms=12
./bin/capture_main frames=300 period_ms=10 work_ms=12 policy=wait
alr exec -- gnatprove -P frame_capture.gpr --level=2 --report=all
```

処理が周期（10 ms）に収まらない `work_ms=12` では、既定の方針は処理待ちのキューが満杯のときに画像を捨て（`dropped` が増える）、`policy=wait` は捨てない代わりにカメラの遅れがたまります。測定値は実行する環境で変わります。

## 検証状態について

- **ローカル（Ubuntu 24.04、GNAT 13.3.0、gprbuild）**: `verify.sh` のビルドと3通りの実行が成功することを確認済みです。
- **GitHub Actions（ubuntu-24.04、Alire で入れた GNAT 16.1.0 と GNATprove 16.1.0）**: [verify-ada-machine-vision](../.github/workflows/verify-ada-machine-vision.yml) のワークフローで `verify.sh` を実行し、すべての検査が証明されること（記事を書いた時点では 43 件）を確かめます。あわせて、`Synchronous` を外した版でデータ競合が、`Slot_Queue` の優先度上限を10に下げた版で「ceiling priority protocol might not be respected」が報告されることを確かめます。
- 優先度（SCHED_FIFO）の効き方は、root か、CAP_SYS_NICE を持つかで変わります。記事の「権限」の節をご覧ください。

## ポイント

- 保護オブジェクトのエントリのバリアで、有限のキューを条件変数なしに書ける
- 保護オブジェクトの優先度上限は、呼び出すタスクのいちばん高い優先度に合わせる
- 画像の領域はスロットの番号を持つタスクだけが触るという約束にし、`Synchronous` で宣言する（これは証明ではなく前提）
- 満杯のときに捨てるか待つかは、取りこぼしとカメラの遅れのどちらを許すかで決める

詳しい解説は[記事本文](https://comcomponent.com/blog/ada-tasking-camera-frame-acquisition/)をご覧ください。
