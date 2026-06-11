using System.Runtime.CompilerServices;
using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class UserStreamProcessorTests
{
    private sealed class TrackingUserRepository : IUserRepository
    {
        private readonly User[] _users;
        private int _startedGetCount;
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TrackingUserRepository(params User[] users)
        {
            _users = users;
        }

        public int StartedGetCount => Volatile.Read(ref _startedGetCount);

        public void ReleaseGets() => _gate.TrySetResult();

        public async IAsyncEnumerable<User> StreamUsersAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (User user in _users)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return user;
            }
        }

        public async Task<User> GetAsync(int id, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _startedGetCount);
            await _gate.Task.WaitAsync(cancellationToken);
            return _users.Single(user => user.Id == id);
        }
    }

    [Fact]
    public async Task ProcessUsersAsync_ProcessesUsersInArrivalOrder()
    {
        var repository = new TrackingUserRepository(
            new User(1, "Alice"),
            new User(2, "Bob"),
            new User(3, "Carol"));

        var processed = new List<string>();
        var processor = new UserStreamProcessor(repository, (user, _) =>
        {
            processed.Add(user.Name);
            return Task.CompletedTask;
        });

        await processor.ProcessUsersAsync(CancellationToken.None);

        Assert.Equal(["Alice", "Bob", "Carol"], processed);
    }

    [Fact]
    public async Task GetUsersAsync_StartsAllTasksEagerly_BecauseOfToArray()
    {
        var repository = new TrackingUserRepository(
            new User(1, "Alice"),
            new User(2, "Bob"),
            new User(3, "Carol"));
        var processor = new UserStreamProcessor(repository, (_, _) => Task.CompletedTask);

        Task<User[]> getUsers = processor.GetUsersAsync([1, 2, 3], CancellationToken.None);

        // ToArray() で確定しているので、await の前に 3 件全部のタスクが開始済み
        Assert.Equal(3, repository.StartedGetCount);

        repository.ReleaseGets();
        User[] users = await getUsers.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(["Alice", "Bob", "Carol"], users.Select(user => user.Name));
    }
}
