using System.Text;

namespace DTooling.DnsServer.Models;

public class DnsQuestion
{
    public string Name { get; set; }
    public DnsType Type { get; set; }
    public DnsClass Class { get; set; }

    internal static DnsQuestion FromBytes(byte[] bytes, ref int offset)
    {
        string name = ReadDomainName(bytes, ref offset);

        DnsType type = (DnsType)((bytes[offset] << 8) | bytes[offset + 1]);
        DnsClass qclass = (DnsClass)((bytes[offset + 2] << 8) | bytes[offset + 3]);
        offset += 4;

        return new DnsQuestion
        {
            Name = name,
            Type = type,
            Class = qclass
        };
    }

    private static string ReadDomainName(byte[] data, ref int offset)
    {
        StringBuilder name = new();
        while (true)
        {
            byte len = data[offset++];
            if (len == 0) break;

            if ((len & 0b1100_0000) == 0b1100_0000)
            {
                int pointer = ((len & 0b0011_1111) << 8) | data[offset++];
                int tempOffset = pointer;
                name.Append(ReadDomainName(data, ref tempOffset));
                break;
            }
            else
            {
                name.Append(Encoding.ASCII.GetString(data, offset, len));
                offset += len;
                if (data[offset] != 0) name.Append('.');
            }
        }

        return name.ToString();
    }
}