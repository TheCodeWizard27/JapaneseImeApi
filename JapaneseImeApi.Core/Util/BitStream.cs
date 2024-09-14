namespace JapaneseImeApi.Core.Util;

public class BitStream
{

    private readonly List<byte> _data = [];
    private int _offset;

    public void PushBit(int bit)
    {
        if (bit != 0 && bit != 1)
            throw new ArgumentException("Bit must be 0 or 1");

        if (_offset % 8 == 0)
            _data.Add(0);

        if (bit == 1)
            _data[_offset / 8] |= (byte)(1 << (7 - (_offset % 8)));

        _offset++;
    }

    public int GetSize()
    {
        return (_offset + 7) / 8;
    }

    public void FillPadding32()
    {
        var paddingBits = (32 - (_offset % 32)) % 32;
        for (var i = 0; i < paddingBits; i++)
        {
            PushBit(0);
        }
    }

    public byte[] ToByteArray() => _data.ToArray();

}
