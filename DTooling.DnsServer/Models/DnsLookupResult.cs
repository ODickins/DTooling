namespace DTooling.DnsServer.Models;

public class DnsLookupResult
{
    public bool IsSuccessful { get; set; }
    public DnsAnswer? DnsAnswer { get; set; }

    public static DnsLookupResult NotFound = new();
    public static DnsLookupResult Found(DnsAnswer dnsAnswer) => new() { DnsAnswer = dnsAnswer, IsSuccessful = true };
}