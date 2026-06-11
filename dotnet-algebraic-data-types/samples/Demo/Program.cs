using KomuraSoft.AlgebraicDataTypes;
using KomuraSoft.AlgebraicDataTypes.Auth;
using KomuraSoft.AlgebraicDataTypes.Members;
using KomuraSoft.AlgebraicDataTypes.OneOfSamples;
using KomuraSoft.AlgebraicDataTypes.Orders;
using KomuraSoft.AlgebraicDataTypes.Records;
using KomuraSoft.AlgebraicDataTypes.Stock;
using ClassicCreateUserResult = KomuraSoft.AlgebraicDataTypes.Classic.CreateUserResult;
using RecordCreateUserResult = KomuraSoft.AlgebraicDataTypes.Records.CreateUserResult;

// 記事に登場する代数的データ型（直和型）の各実装パターンを順に実演するデモです。

Console.WriteLine("=== 1. sealed なクラス階層 + Match（記事 4 章）===");

ClassicCreateUserResult[] classicResults =
[
    ClassicCreateUserResult.Ok(new User("U-0001", "komura")),
    ClassicCreateUserResult.EmailAlreadyUsed("komura@example.com"),
    ClassicCreateUserResult.PasswordIsWeak("12文字以上にしてください。"),
    ClassicCreateUserResult.Failed("DB接続に失敗しました。"),
];

foreach (ClassicCreateUserResult result in classicResults)
{
    string message = result.Match(
        created => "ユーザーを作成しました: " + created.User.Id,
        duplicate => "このメールアドレスは既に使われています: " + duplicate.Email,
        weak => "パスワードが弱すぎます: " + weak.Reason,
        failure => "ユーザー作成に失敗しました: " + failure.Message);

    Console.WriteLine($"  {result.GetType().Name,-14} => {message}");
}

Console.WriteLine();
Console.WriteLine("=== 2. record 階層 + switch 式（記事 6 章）===");

RecordCreateUserResult[] recordResults =
[
    new RecordCreateUserResult.Created(new User("U-0002", "tanaka")),
    new RecordCreateUserResult.DuplicateEmail("tanaka@example.com"),
    new RecordCreateUserResult.WeakPassword("数字だけのパスワードは使えません。"),
    new RecordCreateUserResult.SystemFailure("タイムアウトしました。"),
];

foreach (RecordCreateUserResult result in recordResults)
{
    Console.WriteLine($"  {result.GetType().Name,-14} => {CreateUserResultMessages.ToMessage(result)}");
}

Console.WriteLine();
Console.WriteLine("=== 3. OneOf による直和型（記事 8 章）===");

var registration = new UserRegistrationService(existingEmails: ["taken@example.com"]);

CreateUserCommand[] commands =
[
    new CreateUserCommand("new-user@example.com", "long-enough-password"),
    new CreateUserCommand("taken@example.com", "long-enough-password"),
    new CreateUserCommand("short@example.com", "short"),
];

foreach (CreateUserCommand command in commands)
{
    var result = registration.CreateUser(command);

    var message = result.Match(
        user => $"作成しました: {user.Id}",
        duplicate => $"重複しています: {duplicate.Email}",
        weak => $"パスワードが弱いです: {weak.Reason}");

    Console.WriteLine($"  {command.Email,-22} => {message}");
}

Console.WriteLine();
Console.WriteLine("=== 4. Option<T>: null の代わりに「ない」を表す（記事 11 章）===");

Option<User> foundUser = Option<User>.Of(new User("U-0003", "suzuki"));
Option<User> missingUser = Option<User>.Empty();

foreach (Option<User> user in new[] { foundUser, missingUser })
{
    string displayName = user.Match(
        some: u => u.Name,
        none: () => "ゲスト");

    Console.WriteLine($"  {user.GetType().Name,-4} => 表示名: {displayName}");
}

Console.WriteLine();
Console.WriteLine("=== 5. LoginResult: 想定内の失敗を型で返す（記事 12 章）===");

LoginResult[] loginResults =
[
    LoginResult.Success(new Session("SESSION-001")),
    LoginResult.WrongPassword(),
    LoginResult.Locked(DateTimeOffset.Now.AddMinutes(30)),
    LoginResult.RequireMfa("CHALLENGE-001"),
];

foreach (LoginResult result in loginResults)
{
    string message = result.Match(
        succeeded => $"200 OK (session: {succeeded.Session.Token})",
        invalidPassword => "401 Unauthorized",
        accountLocked => $"423 Locked (until: {accountLocked.Until:HH:mm})",
        mfaRequired => $"202 Accepted (challengeId: {mfaRequired.ChallengeId})");

    Console.WriteLine($"  {result.GetType().Name,-15} => {message}");
}

Console.WriteLine();
Console.WriteLine("=== 6. 状態遷移を型で表す（記事 13 章）===");

var order = new Order(new OrderId("ORD-001"), new UserId("U-0001"));
Console.WriteLine($"  作成直後: {order.State.GetType().Name}");

order.Submit(new SystemClock());
Console.WriteLine($"  Submit 後: {order.State.GetType().Name}");

order.MarkAsPaid("PAY-001");
Console.WriteLine($"  MarkAsPaid 後: {order.State.GetType().Name}");

try
{
    order.Submit(new SystemClock());
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  決済済みの注文を再提出 => InvalidOperationException: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("=== 7. API 境界では DTO に変換する（記事 14 章）===");

PaymentResult[] paymentResults =
[
    new PaymentResult.Succeeded("R-001"),
    new PaymentResult.Rejected("card_expired"),
    new PaymentResult.NetworkFailure("connection timed out"),
];

foreach (PaymentResult result in paymentResults)
{
    PaymentResultDto dto = PaymentResultMapper.ToDto(result);
    string body = dto.Type switch
    {
        "succeeded" => $"{{ \"type\": \"{dto.Type}\", \"receiptNo\": \"{dto.ReceiptNo}\" }}",
        "rejected" => $"{{ \"type\": \"{dto.Type}\", \"reason\": \"{dto.Reason}\" }}",
        _ => $"{{ \"type\": \"{dto.Type}\", \"message\": \"{dto.Message}\" }}",
    };

    Console.WriteLine($"  {result.GetType().Name,-14} => {body}");
}

Console.WriteLine();
Console.WriteLine("=== 8. 在庫引当のリファクタリング例（記事 30 章）===");

var repository = new InMemoryStockRepository();
repository.Add(new StockItem(new Sku("SKU-001"), available: 10));

var stockService = new StockService(repository);

(Sku Sku, int Quantity)[] requests =
[
    (new Sku("SKU-001"), 3),
    (new Sku("SKU-001"), 100),
    (new Sku("SKU-999"), 1),
];

foreach ((Sku sku, int quantity) in requests)
{
    var result = stockService.ReserveStock(sku, quantity);

    string message = result.Match(
        reserved => $"引当成功: {reserved.ReservationId}",
        notFound => $"SKU が見つかりません: {notFound.Sku.Value}",
        outOfStock => $"在庫不足: 要求 {outOfStock.Requested} / 在庫 {outOfStock.Available}");

    Console.WriteLine($"  {sku.Value} x {quantity,3} => {message}");
}

Console.WriteLine();
Console.WriteLine("=== 9. 既存 API 境界では旧形式へ変換する（記事 20 章）===");

RegisterMemberResult[] registerResults =
[
    RegisterMemberResult.Success(new MemberId("M-0001")),
    RegisterMemberResult.EmailAlreadyUsed("komura@example.com"),
    RegisterMemberResult.InvitationCodeIsInvalid("INVALID-CODE"),
];

foreach (RegisterMemberResult result in registerResults)
{
    RegisterMemberResponse response = RegisterMemberResponseMapper.ToResponse(result);
    string summary = response.Success
        ? $"Success=true, MemberId={response.MemberId}"
        : $"Success=false, ErrorCode={response.ErrorCode}, ErrorMessage={response.ErrorMessage}";

    Console.WriteLine($"  {result.GetType().Name,-21} => {summary}");
}

Console.WriteLine();
Console.WriteLine("[demo] done");
