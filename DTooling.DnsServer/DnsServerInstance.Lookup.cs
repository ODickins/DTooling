using System.Net;
using DTooling.DnsServer.Models;

namespace DTooling.DnsServer;

public partial class DnsServerInstance
{
    private async Task<DnsAnswer?> LookupAsync(IPEndPoint endPoint,
        DnsQuestion question,
        CancellationToken cancellationToken)
    {
        var eventHandler = question.Type switch
        {
            DnsType.A => LookupARecordAsync,
            DnsType.Ns => LookupNsRecordAsync,
            DnsType.Cname => LookupCnameRecordAsync,
            DnsType.Soa => LookupSoaRecordAsync,
            DnsType.Ptr => LookupPtrRecordAsync,
            DnsType.Mx => LookupMxRecordAsync,
            DnsType.Txt => LookupTxtRecordAsync,
            DnsType.Aaaa => LookupAaaaRecordAsync,
            DnsType.Any => LookupAnyRecordAsync,
            _ => null
        };

        if (eventHandler is null) return null;

        foreach (var handler in eventHandler.GetInvocationList())
        {
            var result =
                await (ValueTask<DnsLookupResult>)handler.DynamicInvoke(endPoint, question, cancellationToken)!;
            if (result.IsSuccessful) return result.DnsAnswer;
        }

        return null;
    }

    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupARecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupNsRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupCnameRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupSoaRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupPtrRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupMxRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupTxtRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupAaaaRecordAsync;
    public event Func<IPEndPoint, DnsQuestion, CancellationToken, ValueTask<DnsLookupResult>> LookupAnyRecordAsync;
}