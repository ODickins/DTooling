using System.Net;
using System.Text;
using DTooling.DnsServer.Models;

namespace DTooling.DnsServer.Extensions;

public static class DnsMessageExtensions
{
    public static DnsAnswer AnswerIpAddress(this DnsQuestion dnsQuestion, string answer, int ttl = 300)
    {
        var data = dnsQuestion.Type switch
        {
            DnsType.A => IPAddress.Parse(answer).GetAddressBytes(),
            DnsType.Aaaa => IPAddress.Parse(answer).GetAddressBytes(),
            _ => throw new InvalidOperationException($"Cannot answer question with type {dnsQuestion.Type}")
        };
        return DnsAnswer.Create(dnsQuestion.Name, dnsQuestion.Type, dnsQuestion.Class, ttl, data);
    }

    public static DnsAnswer AnswerDomainName(this DnsQuestion dnsQuestion, string answer, int ttl = 300)
    {
        var data = dnsQuestion.Type switch
        {
            DnsType.Cname => EncodeDomainName(answer),
            DnsType.Ns => EncodeDomainName(answer),
            DnsType.Ptr => EncodeDomainName(answer),
            DnsType.Mx => EncodeDomainName(answer),
            _ => throw new InvalidOperationException($"Cannot answer question with type {dnsQuestion.Type}")
        };
        return DnsAnswer.Create(dnsQuestion.Name, dnsQuestion.Type, dnsQuestion.Class, ttl, data);
    }

    public static DnsAnswer AnswerSrv(this DnsQuestion dnsQuestion, ushort priority, ushort weight,
        ushort port,
        string target, int ttl = 300)
    {
        if (dnsQuestion.Type != DnsType.Srv) throw new InvalidOperationException("Question must be SRV");
        var data = new List<byte>();
        data.Add((byte)(priority >> 8));
        data.Add((byte)(priority & 0xFF));
        data.Add((byte)(weight >> 8));
        data.Add((byte)(weight & 0xFF));
        data.Add((byte)(port >> 8));
        data.Add((byte)(port & 0xFF));
        data.AddRange(EncodeDomainName(target));
        return new DnsAnswer
        {
            Name = dnsQuestion.Name,
            Type = dnsQuestion.Type,
            Class = @dnsQuestion.Class,
            Ttl = ttl,
            Data = data.ToArray()
        };
    }

    public static DnsAnswer AnswerSoa(this DnsQuestion dnsQuestion,
        string mname, string rname, uint serial, uint refresh, uint retry, uint expire, uint minimum, int ttl = 300)
    {
        if (dnsQuestion.Type != DnsType.Soa) throw new InvalidOperationException("Question must be SOA");
        var data = new List<byte>();
        data.AddRange(EncodeDomainName(mname));
        data.AddRange(EncodeDomainName(rname));
        data.Add((byte)(serial >> 24));
        data.Add((byte)(serial >> 16));
        data.Add((byte)(serial >> 8));
        data.Add((byte)serial);
        data.Add((byte)(refresh >> 24));
        data.Add((byte)(refresh >> 16));
        data.Add((byte)(refresh >> 8));
        data.Add((byte)refresh);
        data.Add((byte)(retry >> 24));
        data.Add((byte)(retry >> 16));
        data.Add((byte)(retry >> 8));
        data.Add((byte)retry);
        data.Add((byte)(expire >> 24));
        data.Add((byte)(expire >> 16));
        data.Add((byte)(expire >> 8));
        data.Add((byte)expire);
        data.Add((byte)(minimum >> 24));
        data.Add((byte)(minimum >> 16));
        data.Add((byte)(minimum >> 8));
        data.Add((byte)minimum);

        return new DnsAnswer
        {
            Name = dnsQuestion.Name,
            Type = dnsQuestion.Type,
            Class = dnsQuestion.Class,
            Ttl = ttl,
            Data = data.ToArray()
        };
    }

    public static DnsAnswer AnswerTxt(DnsQuestion dnsQuestion, string text, int ttl = 300)
    {
        if (dnsQuestion.Type != DnsType.Txt) throw new InvalidOperationException("Question must be TXT");
        List<byte> data = new();

        var bytes = Encoding.ASCII.GetBytes(text);
        int offset = 0;
        while (offset < bytes.Length)
        {
            int chunkLength = Math.Min(255, bytes.Length - offset);
            data.Add((byte)chunkLength);
            data.AddRange(bytes.AsSpan(offset, chunkLength).ToArray());
            offset += chunkLength;
        }

        return new DnsAnswer
        {
            Name = dnsQuestion.Name,
            Type = dnsQuestion.Type,
            Class = dnsQuestion.Class,
            Ttl = ttl,
            Data = data.ToArray()
        };
    }


    private static byte[] EncodeDomainName(string name)
    {
        var labels = name.Split('.');
        List<byte> bytes = new();
        foreach (var label in labels)
        {
            var labelBytes = System.Text.Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)labelBytes.Length);
            bytes.AddRange(labelBytes);
        }

        bytes.Add(0);
        return bytes.ToArray();
    }

    internal static void AddAnswer(this DnsMessage dnsMessage, DnsAnswer answer)
    {
        dnsMessage.DnsAnswers ??= new List<DnsAnswer>();
        dnsMessage.DnsAnswers.Add(answer);
        dnsMessage.AnCount = (short)dnsMessage.DnsAnswers.Count;
    }

    internal static byte[] ToBytes(this DnsMessage dnsMessage)
    {
        List<byte> bytes = new();

        // Helper to write 16-bit big-endian
        void WriteInt16Be(short value)
        {
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value & 0xFF));
        }

        // Header
        WriteInt16Be(dnsMessage.Id);

        byte flags1 = 0;
        flags1 |= (byte)(dnsMessage.Qr ? 0b1000_0000 : 0);
        flags1 |= (byte)(((byte)dnsMessage.OpCode & 0b1111) << 3);
        flags1 |= (byte)(dnsMessage.Aa ? 0b0000_0100 : 0);
        flags1 |= (byte)(dnsMessage.Tc ? 0b0000_0010 : 0);
        flags1 |= (byte)(dnsMessage.Rd ? 0b0000_0001 : 0);
        bytes.Add(flags1);

        byte flags2 = 0;
        flags2 |= (byte)(dnsMessage.Ra ? 0b1000_0000 : 0);
        flags2 |= (byte)(((byte)dnsMessage.DnsReserved & 0b0111) << 4);
        flags2 |= (byte)((byte)dnsMessage.RCode & 0b1111);
        bytes.Add(flags2);

        WriteInt16Be((short)(dnsMessage.DnsQuestions?.Count ?? 0));
        WriteInt16Be((short)(dnsMessage.DnsAnswers?.Count ?? 0));
        WriteInt16Be(dnsMessage.NsCount);
        WriteInt16Be(dnsMessage.ArCount);

        // Questions
        foreach (var q in dnsMessage.DnsQuestions ?? [])
        {
            WriteDomainName(bytes, q.Name);
            bytes.Add((byte)((ushort)q.Type >> 8));
            bytes.Add((byte)((ushort)q.Type & 0xFF));
            bytes.Add((byte)((ushort)q.Class >> 8));
            bytes.Add((byte)((ushort)q.Class & 0xFF));
        }

        // Answers
        foreach (var a in dnsMessage.DnsAnswers ?? [])
        {
            WriteDomainName(bytes, a.Name);
            bytes.Add((byte)((ushort)a.Type >> 8));
            bytes.Add((byte)((ushort)a.Type & 0xFF));
            bytes.Add((byte)((ushort)a.Class >> 8));
            bytes.Add((byte)((ushort)a.Class & 0xFF));

            bytes.Add((byte)((a.Ttl >> 24) & 0xFF));
            bytes.Add((byte)((a.Ttl >> 16) & 0xFF));
            bytes.Add((byte)((a.Ttl >> 8) & 0xFF));
            bytes.Add((byte)(a.Ttl & 0xFF));

            bytes.Add((byte)((a.Data.Length >> 8) & 0xFF));
            bytes.Add((byte)(a.Data.Length & 0xFF));
            bytes.AddRange(a.Data);
        }

        return bytes.ToArray();
    }

    private static void WriteDomainName(List<byte> bytes, string name)
    {
        var labels = name.Split('.');
        foreach (var label in labels)
        {
            var labelBytes = System.Text.Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)labelBytes.Length);
            bytes.AddRange(labelBytes);
        }

        bytes.Add(0); // null byte to terminate
    }
}