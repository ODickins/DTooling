using System.Net;

namespace DTooling.DnsServer.Models;

public class DnsAnswer
{
    public required string Name { get; set; }
    public DnsType Type { get; set; }
    public DnsClass Class { get; set; }
    public int Ttl { get; set; }
    public required byte[] Data { get; set; }


    public static DnsAnswer Create(string name, DnsType type, DnsClass @class, int ttl, byte[] data)
    {
        return new DnsAnswer()
        {
            Name = name,
            Type = type,
            Class = @class,
            Ttl = ttl,
            Data = data
        };
    }
}