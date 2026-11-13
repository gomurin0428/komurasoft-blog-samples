# 圏論は業務システムで何に使えるか ── サンプルコード

ブログ記事「[圏論は業務システムで何に使えるか ── Topos Instituteの公開物とCatlab.jlで試すスキーマ・データ移行・モデルの合成](https://comcomponent.com/blog/topos-institute-category-theory-business-examples/)」のサンプルコードです。

[Catlab.jl](https://github.com/AlgebraicJulia/Catlab.jl) で、受注・顧客・商品のスキーマを圏として書き、スキーマの変更に沿ったデータの移し方（Δ と Σ）と、部門ごとの業務の流れを配線図で合成する例を動かします。記事に載せた Julia のコードブロックは、すべてこのフォルダのファイルにそのまま含まれています。

## 構成

```
topos-institute-category-theory-business-examples/
├── 01_schema.jl            受注のスキーマ（表が対象、外部キーと列が射）とデータ、外部キーを辿る
├── 02_delta.jl             地域を足した新スキーマ、旧スキーマを埋め込む関手 F（is_functorial で確かめる）、Δ_F
├── 03_sigma.jl             Σ_F（VarACSet の圏を渡して、値の決まらない地域名を AttrVar で表す）
├── run_orders.jl           01〜03 を1つのセッションで順に実行する
├── 04_compose_graph.jl     開いたグラフ（構造化コスパン）を @relation の配線図で合成する
├── expected/
│   ├── run_orders.out      run_orders.jl の期待する出力（記事に載せた出力）
│   └── 04_compose_graph.out
├── Project.toml            Catlab = "=0.17.6"、julia = "1.11"
├── Manifest.toml           検証に使った依存パッケージの版（Julia 1.11.7 で解決）
└── verify.sh               依存パッケージを入れ、2つの例を実行して expected/ と比べる
```

## 試し方

### 前提

- [Julia](https://julialang.org/downloads/) 1.11

### まとめて確かめる

```sh
./verify.sh
```

`Pkg.instantiate()` で Manifest.toml の版の依存パッケージを入れ、`run_orders.jl` と `04_compose_graph.jl` の出力を `expected/` と `diff` で比べます。Julia の実行ファイルの場所は環境変数 `JULIA` で指定できます（既定は PATH 上の `julia`）。初回は Catlab.jl のプリコンパイルに数分かかります。

### 個別に動かす

```sh
julia --project=. -e 'using Pkg; Pkg.instantiate()'
julia --project=. run_orders.jl
julia --project=. 04_compose_graph.jl
```

## 検証状態について

- **ローカル（Linux、Julia 1.11.7、Catlab.jl 0.17.6）**: `verify.sh` で2つの例の出力が `expected/` と一致することを確認済みです。
- **GitHub Actions（ubuntu-24.04、Julia 1.11 の最新）**: [verify-topos-julia](../.github/workflows/verify-topos-julia.yml) のワークフローで `verify.sh` を実行します。
- Catlab.jl は v1.0.0 の前で、版が変わると API が変わることがあります。Project.toml で 0.17.6 に固定しています。

## ポイント

- スキーマは `@present` で生成元（表・外部キー・列）を書き、データは `@acset` で書く
- 関手は構築しただけでは確かめられない。`is_functorial` で確かめる（わざと誤った G は `false`）
- Δ は新スキーマのデータを旧スキーマで読む（地域は落ちる）。Σ は旧スキーマのデータを新スキーマへ移す
- 属性を持つ表の Σ は、既定の呼び方ではなく `cat = ACSetCategory(VarACSetCat(...))` を渡す
- 部品をつなぐ点が合わなければ、合成は `Feet of cospans are not equal` で失敗する

詳しい解説は[記事本文](https://comcomponent.com/blog/topos-institute-category-theory-business-examples/)をご覧ください。
