namespace AumoFinance.Services;

public class UserContext
{
    public Guid UserId { get; set; }

    public UserContext(Guid userId)
    {
        UserId = userId;
    }
}
