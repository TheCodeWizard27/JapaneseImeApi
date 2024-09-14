using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseImeApi.Core.Util;

public class ByteStreamReader(Stream stream)
{
    private readonly Stream _stream = stream;

    public int ReadNextInt()
    {
        return BitConverter.ToInt32(ReadBase(sizeof(int)));
    }

    public byte[] ReadToEnd()
    {
        return Read(_stream.Length - _stream.Position + 1);
    }

    public byte[] Read(int length)
    {
        return Read((long) length);
    }
    public byte[] Read(long length)
    {
        return ReadBase(length);
    }

    public int PeekNextInt()
    {
        var next = ReadNextInt();
        _stream.Position -= sizeof(int);
        return next;
    }

    private byte[] ReadBase(long length)
    {
        var buffer = new byte[length];
        var _ = _stream.Read(buffer);
        return buffer;
    }

}
