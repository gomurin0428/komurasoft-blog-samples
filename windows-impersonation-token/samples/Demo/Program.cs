using System.Security.Principal;
using KomuraSoft.Impersonation;

// Windows の偽装トークンを扱うデモです。
// 偽装トークンは Windows のアクセストークンを前提とした仕組みのため、
// 非 Windows の OS では説明を出して終了します（ビルドは Linux でも可能です）。

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("このデモは Windows 専用です。");
    Console.WriteLine("偽装トークン（ImpersonateLoggedOnUser / WindowsIdentity.RunImpersonated）は");
    Console.WriteLine("Windows のアクセストークンを前提とした仕組みのため、Windows 上で実行してください。");
    Console.WriteLine();
    Console.WriteLine("Windows での確認手順は README.md の「Windows での確認手順」を参照してください。");
    return;
}

// 1. 現在のセキュリティコンテキストを確認する（記事 7 章・16 章）
//    名前だけ見て安心せず、偽装レベルや認証状態も確認します。
WindowsIdentity current = WindowsIdentity.GetCurrent();
Console.WriteLine($"[demo]  Name               : {current.Name}");
Console.WriteLine($"[demo]  ImpersonationLevel : {current.ImpersonationLevel}");
Console.WriteLine($"[demo]  IsAuthenticated    : {current.IsAuthenticated}");
Console.WriteLine();

// 2. RunImpersonated のスコープを確認する（記事 9 章・13 章）
//    ここでは自分自身のトークンを使うため、追加の資格情報なしで
//    「偽装している範囲がラムダ式の中に閉じている」ことを確認できます。
WindowsIdentity.RunImpersonated(current.AccessToken, () =>
{
    WindowsIdentity inner = WindowsIdentity.GetCurrent();
    Console.WriteLine($"[scope] Name               : {inner.Name}");
    Console.WriteLine($"[scope] ImpersonationLevel : {inner.ImpersonationLevel}");
});

// ここから外は元のコンテキスト
Console.WriteLine($"[demo]  scope の外の Name    : {WindowsIdentity.GetCurrent().Name}");
Console.WriteLine();

// 3. 引数が指定されていれば、LogonUser で別ユーザーのトークンを取得し、
//    そのユーザーとしてファイルを読み取る（記事 10 章）
if (args.Length >= 3)
{
    string userName = args[0];
    string password = args[1];
    string path = args[2];
    string? domain = args.Length >= 4 ? args[3] : null;

    Console.WriteLine($"[demo]  {userName} として読み取ります: {path}");

    string content = ImpersonatedFileAccess.ReadFileWithExplicitCredential(
        userName,
        domain,
        password,
        path);

    Console.WriteLine($"[demo]  読み取り成功 ({content.Length} 文字)");
}
else
{
    Console.WriteLine("別ユーザーの資格情報でファイルを読むには、次の形式で引数を指定してください。");
    Console.WriteLine("  dotnet run --project samples/Demo -- <userName> <password> <path> [domain]");
    Console.WriteLine();
    Console.WriteLine("注意: これは API の形を示すためのデモです。実務ではパスワードをコマンドラインや");
    Console.WriteLine("      設定ファイルに平文で置かず、Secret Store 等で管理してください（記事 10 章・21 章）。");
}

Console.WriteLine();
Console.WriteLine("[demo]  done");
