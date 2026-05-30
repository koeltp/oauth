namespace OAuth.Domain.Exceptions;

public class TwoFactorRequiredException : Exception
{
    public Guid UserId { get; }

    public TwoFactorRequiredException(Guid userId)
        : base("需要两步验证")
    {
        UserId = userId;
    }
}