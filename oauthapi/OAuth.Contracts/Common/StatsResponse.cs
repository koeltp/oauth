namespace OAuth.Contracts.Common;

public class StatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int TotalAuthorizations { get; set; }
}