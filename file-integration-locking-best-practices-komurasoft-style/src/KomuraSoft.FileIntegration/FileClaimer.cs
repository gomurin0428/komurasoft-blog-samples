namespace KomuraSoft.FileIntegration;

/// <summary>
/// incoming -> processing への rename で claim（処理権）を原子的に取ります（記事 4.3 / 5.2 章）。
/// rename が成功したワーカーだけが所有権を持つので、「一覧を見て、未処理なら開く」の
/// 二重取得（記事 2.2 章）が起きません。
/// </summary>
public static class FileClaimer
{
    /// <summary>
    /// payload と done file の組（バンドル）の claim を rename で取ります。
    /// 先に rename に成功したワーカーだけが true を受け取ります。
    /// incoming と processing は同一ファイルシステム上に置くのが前提です。
    /// </summary>
    public static bool TryClaimBundleByRename(string baseName, string incomingDir, string processingDir)
    {
        Directory.CreateDirectory(processingDir);

        string payloadSource = Path.Combine(incomingDir, baseName);
        string payloadTarget = Path.Combine(processingDir, baseName);

        try
        {
            // 「確認」と「確保」を分けず、rename の成否だけで所有権を決める
            File.Move(payloadSource, payloadTarget);
        }
        catch (FileNotFoundException)
        {
            return false; // 他ワーカーが先に取得
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            return false; // Windows では移動先競合なども IOException になる
        }

        // 所有権を取ったワーカーが、続けて done file も引き取る
        string doneSource = Path.Combine(incomingDir, baseName + ".done");
        string doneTarget = Path.Combine(processingDir, baseName + ".done");
        if (File.Exists(doneSource))
        {
            File.Move(doneSource, doneTarget, overwrite: true);
        }

        return true;
    }

    /// <summary>
    /// バンドル（payload と done file）を別ディレクトリへ移します（記事 5.2 章の MoveBundle）。
    /// processing -> archive / error の遷移に使います。
    /// </summary>
    public static void MoveBundle(string sourceDir, string targetDir, string baseName)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string fileName in new[] { baseName, baseName + ".done" })
        {
            string source = Path.Combine(sourceDir, fileName);
            if (File.Exists(source))
            {
                File.Move(source, Path.Combine(targetDir, fileName), overwrite: true);
            }
        }
    }

    /// <summary>
    /// incoming で「読んでよい」状態になったバンドルの baseName を列挙します。
    /// 完了の合図は done file の存在で判定します（サイズ安定待ちはしません。記事 3.3 章）。
    /// </summary>
    public static IReadOnlyList<string> ListReadyBaseNames(string incomingDir)
    {
        if (!Directory.Exists(incomingDir))
        {
            return Array.Empty<string>();
        }

        var ready = new List<string>();
        foreach (string donePath in Directory.EnumerateFiles(incomingDir, "*.done"))
        {
            string baseName = Path.GetFileNameWithoutExtension(donePath);
            if (File.Exists(Path.Combine(incomingDir, baseName)))
            {
                ready.Add(baseName);
            }
        }

        ready.Sort(StringComparer.Ordinal);
        return ready;
    }
}
