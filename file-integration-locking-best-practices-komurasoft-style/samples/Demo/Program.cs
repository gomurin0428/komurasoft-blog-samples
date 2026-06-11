using System.Text;
using KomuraSoft.FileIntegration;

// ファイル連携の排他制御を、ローカルの一時ディレクトリ上で実演するデモです。
// 1. 送信側が temp -> close -> rename / replace で payload を公開し、最後に done file を置く
// 2. 2 つの受信ワーカーが同じ incoming を監視し、claim rename で所有権を取り合う
// 3. 同じ idempotency key のバンドルが二重に届いても、処理は 1 回だけになる
// 4. おまけ: lease 方式の lock file の取得・引き取りを実演する

string root = Path.Combine(
    Path.GetTempPath(),
    "komurasoft-file-integration-demo-" + Guid.NewGuid().ToString("N"));

string incomingDir = Path.Combine(root, "incoming");
string archiveDir = Path.Combine(root, "archive");
string errorDir = Path.Combine(root, "error");
var ledger = new ProcessedLedger(Path.Combine(root, "ledger"));

Console.WriteLine($"[demo] working directory: {root}");
Console.WriteLine();

try
{
    // ---- 1. 送信側: temp -> rename で公開し、done file で完了を明示する ----
    Console.WriteLine("== sender: temp -> close -> rename / replace + done file ==");

    (string BaseName, string Key, string Body)[] bundles =
    [
        ("orders-0001.csv", "integration-0001", "id,amount\n1,100\n2,250\n"),
        ("orders-0002.csv", "integration-0002", "id,amount\n3,300\n"),
        // integration-0001 の再送（リトライで同じ連携 ID がもう一度届いた状況）
        ("orders-0001-retry.csv", "integration-0001", "id,amount\n1,100\n2,250\n"),
    ];

    foreach ((string baseName, string key, string body) in bundles)
    {
        TransferManifest manifest = FileTransferSender.PublishBundle(
            incomingDir, baseName, key,
            stream => stream.Write(Encoding.UTF8.GetBytes(body)));
        Console.WriteLine(
            $"[sender] published {baseName} " +
            $"(size={manifest.Size}, key={manifest.IdempotencyKey}, hash={manifest.Hash[..12]}...)");
    }

    Console.WriteLine();

    // ---- 2. 受信側: 2 ワーカーが claim rename で取り合う ----
    Console.WriteLine("== receivers: claim by rename (incoming -> processing) ==");

    FileTransferReceiver CreateReceiver(string workerName) => new(
        incomingDir: incomingDir,
        processingDir: Path.Combine(root, "processing", workerName),
        archiveDir: archiveDir,
        errorDir: errorDir,
        ledger: ledger,
        process: path => Console.WriteLine(
            $"[{workerName}] processing {Path.GetFileName(path)}: " +
            $"{File.ReadLines(path).Count() - 1} record(s)"));

    FileTransferReceiver worker1 = CreateReceiver("worker-1");
    FileTransferReceiver worker2 = CreateReceiver("worker-2");

    foreach (string baseName in worker1.ListReadyBaseNames())
    {
        // 同じバンドルを 2 ワーカーが同時に拾おうとする（記事 2.2 章の競合）
        Task<ReceiveResult> attempt1 = Task.Run(() => worker1.ProcessBundle(baseName));
        Task<ReceiveResult> attempt2 = Task.Run(() => worker2.ProcessBundle(baseName));
        ReceiveResult[] results = await Task.WhenAll(attempt1, attempt2);

        Console.WriteLine(
            $"[demo] {baseName}: worker-1 -> {results[0]}, worker-2 -> {results[1]}");
    }

    Console.WriteLine();
    Console.WriteLine(
        $"[demo] archive: {string.Join(", ", Directory.GetFiles(archiveDir).Select(Path.GetFileName).Order())}");
    Console.WriteLine();

    // ---- 3. lease 方式の lock file ----
    Console.WriteLine("== lease lock file: atomic create + stale takeover ==");

    string lockPath = Path.Combine(root, "nightly-batch.lock.json");

    using (LeaseLockFile? lease = LeaseLockFile.TryAcquire(
        lockPath, "batch-A", TimeSpan.FromMinutes(5)))
    {
        Console.WriteLine($"[batch-A] acquired: {(lease is not null ? "yes" : "no")}");

        LeaseLockFile? blocked = LeaseLockFile.TryAcquire(
            lockPath, "batch-B", TimeSpan.FromMinutes(5));
        Console.WriteLine($"[batch-B] acquired while batch-A holds the lease: {(blocked is not null ? "yes" : "no")}");
    } // batch-A が Release（正常終了）

    // 異常終了して heartbeat が止まった lock の引き取り
    LeaseLockFile? crashed = LeaseLockFile.TryAcquire(
        lockPath, "batch-A", TimeSpan.FromMilliseconds(100));
    Console.WriteLine($"[batch-A] acquired again (then crashes without release): {(crashed is not null ? "yes" : "no")}");
    await Task.Delay(300); // lease の期限切れを待つ

    using (LeaseLockFile? successor = LeaseLockFile.TryAcquire(
        lockPath, "batch-B", TimeSpan.FromMinutes(5)))
    {
        Console.WriteLine($"[batch-B] took over the stale lease: {(successor is not null ? "yes" : "no")}");
        Console.WriteLine($"[demo] lock owner on disk: {LeaseLockFile.TryRead(lockPath)?.OwnerId}");
    }

    Console.WriteLine();
    Console.WriteLine("[demo] done");
}
finally
{
    Directory.Delete(root, recursive: true);
}
