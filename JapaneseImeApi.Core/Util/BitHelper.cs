namespace JapaneseImeApi.Core.Util;

public static class BitHelper
{

    public static byte[] GetSizeBytes(int value)
    {
        return
        [
            (byte)(value & 0xFF),
            (byte)(value >> 8 & 0xFF),
            (byte)(value >> 16 & 0xFF),
            (byte)(value >> 24 & 0xFF)
        ];
    }

}