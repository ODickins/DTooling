using System.Net;
using System.Net.Sockets;
using DTooling.DnsServer.Extensions;
using DTooling.DnsServer.Models;
using Microsoft.Extensions.Logging;

namespace DTooling.DnsServer;

public partial class DnsServerInstance
{
    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 53));
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                if (result.Buffer.Length < 12) continue; // Too small to be a DNS packet
                _ = ProcessUdpRequestAsync(udpClient, result, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Error processing DNS request");
        }
    }

    private async Task ProcessUdpRequestAsync(UdpClient udpClient, UdpReceiveResult udpReceiveResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var dnsResult = DnsMessage.FromBytes(udpReceiveResult.Buffer);
            dnsResult.Qr = true;
            dnsResult.RCode = DnsRCode.NxDomain;

            try
            {
                foreach (var question in dnsResult.DnsQuestions ?? [])
                {
                    var lookupResponse =
                        await LookupAsync(udpReceiveResult.RemoteEndPoint, question, cancellationToken);
                    if (lookupResponse is not null)
                    {
                        dnsResult.AddAnswer(lookupResponse);
                        dnsResult.RCode = DnsRCode.NoError;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error looking up DNS question");
                dnsResult.RCode = DnsRCode.ServerFailure;
            }

            var response = dnsResult.ToBytes();
            await udpClient.SendAsync(response, response.Length, udpReceiveResult.RemoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Error processing DNS request");
        }
    }
}