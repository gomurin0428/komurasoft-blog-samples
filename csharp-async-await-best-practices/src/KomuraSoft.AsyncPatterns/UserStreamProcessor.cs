namespace KomuraSoft.AsyncPatterns;

public sealed record User(int Id, string Name);

/// <summary>
/// 記事のコード片に登場する _userRepository の最小インターフェイスです。
/// </summary>
public interface IUserRepository
{
    IAsyncEnumerable<User> StreamUsersAsync(CancellationToken cancellationToken);

    Task<User> GetAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// 逐次的に届くデータを IAsyncEnumerable&lt;T&gt; / await foreach で処理する例（記事 3.9）と、
/// LINQ でタスクを作るときに ToArray で確定する例（記事 4.5）です。
/// </summary>
public sealed class UserStreamProcessor
{
    private readonly IUserRepository _userRepository;
    private readonly Func<User, CancellationToken, Task> _processUser;

    public UserStreamProcessor(IUserRepository userRepository, Func<User, CancellationToken, Task> processUser)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(processUser);
        _userRepository = userRepository;
        _processUser = processUser;
    }

    /// <summary>
    /// 全件そろうまで待たずに、届いたものから順に処理します（記事 3.9）。
    /// </summary>
    public async Task ProcessUsersAsync(CancellationToken cancellationToken)
    {
        await foreach (User user in _userRepository.StreamUsersAsync(cancellationToken))
        {
            await ProcessUserAsync(user, cancellationToken);
        }
    }

    /// <summary>
    /// LINQ は遅延実行なので、ToArray() でいったん確定して全タスクを開始します（記事 4.5）。
    /// </summary>
    public async Task<User[]> GetUsersAsync(IEnumerable<int> userIds, CancellationToken cancellationToken)
    {
        Task<User>[] tasks = userIds
            .Select(id => _userRepository.GetAsync(id, cancellationToken))
            .ToArray();

        User[] users = await Task.WhenAll(tasks);
        return users;
    }

    private Task ProcessUserAsync(User user, CancellationToken cancellationToken)
        => _processUser(user, cancellationToken);
}
