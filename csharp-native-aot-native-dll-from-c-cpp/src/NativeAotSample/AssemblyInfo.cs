// テストから internal なロジック（AccumulatorStore など）を検証するための設定です。
// 記事本文のコード（NativeExports.cs）には手を入れず、ここで公開範囲だけを広げています。
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NativeAotSample.Tests")]
