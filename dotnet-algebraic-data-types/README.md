# 代数的データ型を.NET Framework / .NETで使う ── サンプルコード

ブログ記事「[代数的データ型を.NET Framework / .NETで使う ── 状態と結果を型で表す設計](https://comcomponent.com/blog/2026/06/09/003-dotnet-algebraic-data-types/)」のサンプルコードです。

代数的データ型（ADT）、特に直和型は、「この値は、取り得る形があらかじめ決まっている」ことをコメントや命名規則ではなく型で表す考え方です。`bool` や `enum + nullable プロパティ群` で表していた状態・結果を「どれか 1 つ」の閉じたケース集合として設計すると、不正な状態をそもそも作れなくなります。

このサンプルでは、記事で紹介している **sealed なクラス階層 + private コンストラクター + Match メソッド**（.NET Framework でも使える形）、**record 階層 + パターンマッチング**（現行 .NET 向け）、**OneOf ライブラリ**の 3 つの実装パターンと、その検証コードを提供します。

## 構成

```
dotnet-algebraic-data-types/
├── src/KomuraSoft.AlgebraicDataTypes/         ADT の実装パターン集（クラスライブラリ）
│   ├── Classic/CreateUserResult.cs            sealed なクラス階層 + Match（記事 4 章）
│   ├── Classic/PaymentResult.cs               private コンストラクターで閉じた集合にする（記事 5 章）
│   ├── Records/CreateUserResult.cs            record 階層 + Match（記事 6 章）
│   ├── Records/CreateUserResultMessages.cs    switch 式によるパターンマッチング（記事 6 章）
│   ├── OneOfSamples/UserRegistrationService.cs OneOf による直和型（記事 8 章）
│   ├── Option.cs                              Option型: null の代わりに「ない」を表す（記事 11 章）
│   ├── Auth/LoginResult.cs                    Result型: 想定内の失敗を型で返す（記事 12 章）
│   ├── Orders/OrderState.cs, Order.cs         状態遷移を型で表す（記事 13 章）
│   ├── Orders/SubmitOrderResult.cs            ケースそのものがテスト観点になる結果型（記事 17・19 章）
│   ├── Records/PaymentResult*.cs              API 境界での DTO 変換（記事 14 章）
│   ├── Reservations/ReservationState.cs       不正な状態を作りにくくする（記事 15 章）
│   ├── Documents/GetDocumentResult.cs         呼び出し側に処理漏れを意識させる（記事 16 章）
│   ├── Members/RegisterMember*.cs             既存 API 境界では旧形式へ変換する（記事 20 章）
│   └── Stock/ReserveStockResult.cs, StockService.cs  bool + out errorCode のリファクタリング（記事 30 章）
├── samples/Demo/                              各パターンを順に実演するコンソールアプリ
└── tests/KomuraSoft.AlgebraicDataTypes.Tests/ Match の網羅・状態遷移・DTO 変換を検証するユニットテスト
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（クラス階層・record・OneOf・Option・状態遷移・DTO 変換を順に実演します）:

```console
dotnet run --project samples/Demo
```

テスト（各ケースへの Match の振り分け、状態遷移の制約、在庫引当のリファクタリング結果、旧形式レスポンスへの変換などを検証します）:

```console
dotnet test
```

## ポイント

- 「不正な状態をそもそも作れないようにする」ことが目的であり、特定の構文を使うことが目的ではない
- 基底クラスのコンストラクターを `private` にし、ケース型をネストした sealed クラスにすると、ケース集合を閉じられる
- 利用側を `Match` メソッドに寄せると、ケース追加時に呼び出し側の処理漏れをコンパイルエラーで見つけられる
- 想定内の業務分岐は Result / ADT で返し、通常の処理で回復できない異常は例外にする
- ドメイン内部では強い型を使い、JSON / DB / 既存 API との境界では DTO に変換して外部表現を安定させる
- ケースが外部から増える設計（プラグインなど）には、閉じた ADT ではなくインターフェースが向いている

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/09/003-dotnet-algebraic-data-types/)をご覧ください。
