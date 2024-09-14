using JapaneseImeApi.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public class BitVectorBasedArray
{

    private const int Lb0CacheSize = 1024;
    private const int Lb1CacheSize = 0;

    private readonly SimpleSuccinctBitVectorIndex _index;
    private readonly int _baseLength;
    private readonly int _stepLength;
    private readonly byte[] _data;

    public BitVectorBasedArray(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var reader = new ByteStreamReader(stream);

        var indexLength = reader.ReadNextInt();
        _baseLength = reader.ReadNextInt();
        _stepLength = reader.ReadNextInt();

        if (reader.ReadNextInt() != 0) throw new Exception("Invalid data no 0 padding found!");

        var indexData = reader.Read(indexLength);
        _index = new SimpleSuccinctBitVectorIndex(indexData, Lb0CacheSize, Lb1CacheSize);

        _data = reader.ReadToEnd();
    }

    public List<TokenInfo> GetTokenInfos(int id)
    {
        var dataView = Get(id);
        return Codec.DecodeTokens(dataView);
    }

    private byte[] Get(int id)
    {
        var bitId = _index.Select0(id + 1);
        var dataId = _baseLength * id + _stepLength * _index.Rank1(bitId);

        var i = bitId + 1;
        while (_index.Get(i) == 1)
        {
            i++;
        }

        var length = _baseLength + _stepLength * (i - bitId - 1);
        return _data[dataId..];
    }

}
