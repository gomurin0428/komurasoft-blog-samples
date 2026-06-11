using OneOf;

namespace KomuraSoft.AlgebraicDataTypes.OneOfSamples;

// 記事 8 章: OneOf のようなライブラリを使う
// 専用の基底クラスを作らずに「User / DuplicateEmail / WeakPassword のどれか 1 つ」を表す

public sealed class CreateUserCommand
{
    public CreateUserCommand(string email, string password)
    {
        if (email == null) throw new ArgumentNullException(nameof(email));
        if (password == null) throw new ArgumentNullException(nameof(password));
        Email = email;
        Password = password;
    }

    public string Email { get; }
    public string Password { get; }
}

public sealed class DuplicateEmail
{
    public DuplicateEmail(string email)
    {
        Email = email;
    }

    public string Email { get; }
}

public sealed class WeakPassword
{
    public WeakPassword(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

public sealed class UserRegistrationService
{
    private readonly HashSet<string> _existingEmails;
    private int _nextUserNumber = 1;

    public UserRegistrationService(IEnumerable<string>? existingEmails = null)
    {
        _existingEmails = new HashSet<string>(
            existingEmails ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public OneOf<User, DuplicateEmail, WeakPassword> CreateUser(CreateUserCommand command)
    {
        if (EmailExists(command.Email))
        {
            return new DuplicateEmail(command.Email);
        }

        if (!IsStrongPassword(command.Password))
        {
            return new WeakPassword("12文字以上にしてください。");
        }

        return CreateUserCore(command);
    }

    private bool EmailExists(string email)
        => _existingEmails.Contains(email);

    private static bool IsStrongPassword(string password)
        => password.Length >= 12;

    private User CreateUserCore(CreateUserCommand command)
    {
        _existingEmails.Add(command.Email);
        return new User($"U-{_nextUserNumber++:D4}", command.Email);
    }
}
