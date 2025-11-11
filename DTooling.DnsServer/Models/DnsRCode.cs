namespace DTooling.DnsServer.Models;

public enum DnsRCode
{
    NoError = 0,
    FormatError = 1,
    ServerFailure = 2,
    NxDomain = 3,
    NotImplemented = 4,
    Refused = 5
}