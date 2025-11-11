namespace DTooling.DnsServer.Models;

public class DnsMessage
{
    public short Id { get; set; }

    public bool Qr { get; set; }

    public DnsOpCode OpCode { get; set; }

    public bool Aa { get; set; }

    public bool Tc { get; set; }
    public bool Rd { get; set; }
    public bool Ra { get; set; }

    public DnsReserved DnsReserved { get; set; }

    public DnsRCode RCode { get; set; }

    public short QdCount { get; set; }
    public short AnCount { get; set; }
    public short NsCount { get; set; }
    public short ArCount { get; set; }

    public required ICollection<DnsQuestion> DnsQuestions { get; set; }
    public required ICollection<DnsAnswer> DnsAnswers { get; set; }

    internal static DnsMessage FromBytes(byte[] bytes)
    {
        short ReadInt16Be(int offset) =>
            (short)((bytes[offset] << 8) | bytes[offset + 1]);


        var dnsMessage = new DnsMessage()
        {
            Id = ReadInt16Be(0),

            Qr = (bytes[2] & 0b1000_0000) != 0,
            OpCode = (DnsOpCode)((bytes[2] >> 3) & 0b1111),
            Aa = (bytes[2] & 0b0000_0100) != 0,
            Tc = (bytes[2] & 0b0000_0010) != 0,
            Rd = (bytes[2] & 0b0000_0001) != 0,

            Ra = (bytes[3] & 0b1000_0000) != 0,
            DnsReserved = (DnsReserved)((bytes[3] >> 4) & 0b0111),
            RCode = (DnsRCode)(bytes[3] & 0b0000_1111),

            QdCount = ReadInt16Be(4),
            AnCount = ReadInt16Be(6),
            NsCount = ReadInt16Be(8),
            ArCount = ReadInt16Be(10),
            DnsQuestions = new List<DnsQuestion>(),
            DnsAnswers = new List<DnsAnswer>()
        };

        int offset = 12; // After header
        for (int i = 0; i < dnsMessage.QdCount; i++)
        {
            var question = DnsQuestion.FromBytes(bytes, ref offset);
            dnsMessage.DnsQuestions.Add(question);
        }

        return dnsMessage;
    }
}