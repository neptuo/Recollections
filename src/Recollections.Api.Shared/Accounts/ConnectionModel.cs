using Neptuo.Recollections.Sharing;

namespace Neptuo.Recollections.Accounts;

public class ConnectionModel
{
    public string OtherUserId { get; set; }
    public string OtherUserName { get; set; }
    public Permission? Permission { get; set; }
    public ConnectionRole Role { get; set; }
    public ConnectionState State { get; set; }
}