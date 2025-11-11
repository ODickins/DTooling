// See https://aka.ms/new-console-template for more information

using DTooling.DnsServer;
using DTooling.DnsServer.Extensions;
using DTooling.DnsServer.Models;

Console.WriteLine("Hello, World!");


using var dnsServer = new DnsServerInstance();
dnsServer.Start();

dnsServer.LookupARecordAsync += async (endpoint, question, token) =>
{
    Console.WriteLine($"lookup received for {question.Name} from {endpoint}");
    return DnsLookupResult.Found(question.AnswerIpAddress("127.0.0.1"));
};

Console.ReadLine();