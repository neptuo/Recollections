namespace Neptuo.Recollections.Accounts;

public class ConnectionModel
{
    public string OtherUserName { get; set; }
    public ConnectionRole Role { get; set; }
    public ConnectionState State { get; set; }
}