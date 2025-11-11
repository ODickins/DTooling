namespace DTooling.DnsServer.Models;

public enum DnsOpCode
{
    Query = 0,
    IQuery = 1,
    Status = 2,
    Notify = 4,
    Update = 5
}