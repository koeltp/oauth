namespace OAuth.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? GetUserId();
    bool IsAuthenticated();
    string? GetUserEmail();
}