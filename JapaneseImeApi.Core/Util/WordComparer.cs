namespace JapaneseImeApi.Core.Util;
public class WordComparer : IComparer<byte[]>, IEqualityComparer<byte[]>
{

    public int Compare(byte[] x, byte[] y)
    {
        if (x.Length != y.Length)
        {
            return x.Length.CompareTo(y.Length);
        }

        for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
        {
            if (x[i] != y[i])
            {
                return x[i].CompareTo(y[i]);
            }
        }

        return 0;
    }

    public bool Equals(byte[]? x, byte[]? y)
    {
        return Compare(x, y) == 0;
    }

    public int GetHashCode(byte[] obj)
    {
        if (obj == null) return 0;
        int hash = 17;
        foreach (byte b in obj)
        {
            hash = hash * 31 + b;
        }
        return hash;
    }
}