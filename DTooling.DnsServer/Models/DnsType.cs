namespace DTooling.DnsServer.Models;

public enum DnsType : ushort
{
    A = 1,
    Ns = 2,
    Cname = 5,
    Soa = 6,
    Ptr = 12,
    Mx = 15,
    Txt = 16,
    Aaaa = 28,
    Srv = 33,
    Any = 255
}