using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public class SimpleSuccinctBitVectorIndex
{

    private readonly List<int> _index = new();
    private readonly byte[] _data;
    private readonly int _chunkSize = 32;
    private int _lb0CacheIncrement;
    private int _lb1CacheIncrement;
    //private readonly List<int[]> _lb0Cache = new(); // Maybe switch to different datatype?
    //private readonly List<int[]> _lb1Cache = new();
    private List<int> _lb0Cache = [];
    private List<int> _lb1Cache = [];

    public SimpleSuccinctBitVectorIndex(byte[] data, int lb0CacheSize, int lb1CacheSize)
    {
        _data = data;

        Init();

        _lb0CacheIncrement = lb0CacheSize == 0 ? Get0BitCount() : Get0BitCount() / lb0CacheSize;
        if (_lb0CacheIncrement == 0)
        {
            _lb0CacheIncrement = 1;
        }
        InitLowerBound0Cache(_lb0CacheIncrement, lb0CacheSize);

        _lb1CacheIncrement = lb1CacheSize == 0 ? Get1BitCount() : Get1BitCount() / lb1CacheSize;
        if (_lb1CacheIncrement == 0)
        {
            _lb1CacheIncrement = 1;
        }
        InitLowerBound1Cache(_lb1CacheIncrement, lb1CacheSize);

    }

    // Returns the bit at the index in data. The index in a byte is as follows;
    // MSB|XXXXXXXX|LSB
    //     76543210
    public int Get(int index)
    {
        return (_data[index / 8] >> (index % 8)) & 1;
    }

    public int Rank1(int n)
    {
        // Look up pre-computed 1-bits for the preceding chunks.
        var chunkCount = n / (_chunkSize * 8);
        var result = _index[n / (_chunkSize * 8)];

        var readSize = (n/8 - chunkCount * _chunkSize) / 4;

        // Count 1-bits for remaining "words".
        result += Count1Bit(_data, readSize, chunkCount * _chunkSize);

        // Count 1-bits for remaining "bits".
        if (n % 32 > 0)
        {
            var offset = 4 * (n / 32);
            var shift = 32 - n % 32;
            result += BitOperations.PopCount(BitConverter.ToUInt32(_data, offset) << shift);
        }

        return result;
    }

    public int Select0(int n)
    {
        var lb0Id = Math.Min(n / _lb0CacheIncrement, _lb0Cache.Count-2);

        // Start counting from previous index which is why -1 is at the end.
        // This also means that n of value 0 is not allowed.
        // Search chunk
        var chunkId = LowerBound(_index, n, (i) => ZeroBitIndex(i, _chunkSize, _index),
            _lb0Cache[lb0Id], _lb0Cache[lb0Id + 1]) -1;
        n -= _chunkSize * 8 * chunkId - _index[chunkId];

        // Linear search on remaining "words"
        var offset = (chunkId * _chunkSize) & ~3;
        while (true)
        {
            var bitCount = Count0Bit(BitConverter.ToUInt32(_data, offset));
            if (bitCount >= n)
            {
                break;
            }
            n -= bitCount;
            offset += 4;
        }

        var index = offset * 8;
        for (var word = ~BitConverter.ToUInt32(_data[offset..(offset + 4)]); n > 0; word >>= 1, index++)
        {
            n -= (int)(word & 1);
        }

        // Index points to the "next bit" of the target one.
        // Thus, subtract one to adjust.
        return index -1;
    }

    public int Select1(int n)
    {
        var lb1Id = Math.Min(n / _lb1CacheIncrement, _lb1Cache.Count - 2);

        // Start counting from previous index which is why -1 is at the end.
        // This also means that n of value 0 is not allowed.
        // Search chunk
        var chunkId = LowerBound(_index, n, _lb1Cache[lb1Id], _lb1Cache[lb1Id + 1]) -1;
        n -= _index[chunkId];

        // Linear search on remaining "words"
        var offset = (chunkId * _chunkSize) & ~3;
        while (true)
        {
            var bitCount = BitOperations.PopCount(BitConverter.ToUInt32(_data, offset));
            if (bitCount >= n)
            {
                break;
            }
            n -= bitCount;
            offset += 4;
        }

        var index = offset * 8;
        for (var word = BitConverter.ToUInt32(_data, offset); n > 0; word >>= 1, index++)
        {
            n -= (int)(word & 1);
        }

        // Index points to the "next bit" of the target one.
        // Thus, subtract one to adjust.
        return index - 1;
    }

    private void Init()
    {
        // Counting bits in 32 bit sized words.
        var offset = 0;
        var bitCount = 0;
        for (
            var remainingWordCount = _data.Length / 4;
            remainingWordCount > 0;
            offset += _chunkSize, remainingWordCount -= _chunkSize / 4)
        {
            _index.Add(bitCount);
            bitCount += Count1Bit(_data, Math.Min(_chunkSize / 4, remainingWordCount), offset);
        }
        _index.Add(bitCount);
    }

    private void InitLowerBound0Cache(int cacheIncrement, int cacheSize)
    {
        _lb0Cache.Clear();
        _lb0Cache.Add(0);

        for (var i = 1; i < cacheSize; i++)
        {
            var targetIndex = cacheIncrement * i;
            var id = LowerBound(_index, targetIndex, (i) => ZeroBitIndex(i, _chunkSize, _index));
            _lb0Cache.Add(id);
        }
        _lb0Cache.Add(_index.Count - 1);
    }

    private void InitLowerBound1Cache(int cacheIncrement, int cacheSize)
    {
        _lb1Cache.Clear();
        _lb1Cache.Add(0);

        for (var i = 1; i < cacheSize; i++)
        {
            var targetIndex = cacheIncrement * i;
            var id = LowerBound(_index, targetIndex);
            _lb1Cache.Add(id);
        }
        _lb1Cache.Add(_index.Count - 1);
    }

    private int LowerBound(List<int> array, int value, Func<int, int> valueExpr, int offset = 0, int? end = null)
    {
        end ??= array.Count;
        var lastId = Math.Max(0, end.Value - 1);

        if (offset == end) return offset; // Hacky way to deal with edge case where the array is empty.

        for (var i = offset; i < end; i++)
        {
            if (i >= array.Count) return lastId;

            if (valueExpr(i) >= value) return i;
        }

        return lastId;
    }

    //private int LowerBound(List<int> array, int value, Func<int, int> iterator, int offset = 0, int? end = null)
    //{
    //    end ??= array.Count;
    //    var lastId = Math.Max(0, end.Value -1);
    //    lastId = Math.Min(lastId, iterator(lastId));

    //    for (var i = offset; iterator(i) < end; i++)
    //    {
    //        var currentId = iterator(i);
    //        if (currentId >= array.Count) return lastId;
            
    //        if (array[currentId] >= value) return currentId;
    //    }

    //    return lastId;
    //}

    private int LowerBound(List<int> array, int value, int offset = 0, int? end = null)
    {
        end ??= array.Count;
        var lastId = Math.Max(0, end.Value -1);

        if (offset == end) return offset; // Hacky way to deal with edge case where the array is empty.

        for (var i = offset; i < end; i++)
        {
            if (i >= array.Count) return lastId;

            if (array[i] >= value) return i;
        }

        return lastId;
    }

    private static int ZeroBitIndex(int id, int chunkSize, List<int> data)
    {
        return chunkSize * 8 * id - data[id];
    }

    public int Get1BitCount()
    {
        return _index.Last();
    }
    public int Get0BitCount()
    {
        return 8 * _data.Length - _index.Last();
    }

    private int Count1Bit(byte[] data, int length, int offset)
    {
        var bitCount = 0;
        var i = 0;

        while (length > 0)
        {
            bitCount += BitOperations.PopCount(BitConverter.ToUInt32(data, offset + i));
            i += sizeof(uint);
            length--;
        }

        return bitCount;
    }

    private static int Count0Bit(uint x)
    {
        return BitOperations.PopCount(~x);
    }

}
