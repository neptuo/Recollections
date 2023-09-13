namespace Neptuo.Recollections.Accounts;

public class UserConnection
{
    public User User { get; set; }
    public string UserId { get; set; }

    public User OtherUser { get; set; }
    public string OtherUserId { get; set; }

    public int State { get; set; }
}