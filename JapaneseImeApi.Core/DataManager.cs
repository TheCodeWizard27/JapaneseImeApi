using System.Text;
using JapaneseImeApi.Core.Constants;
using JapaneseImeApi.Core.SystemDictionary;
using JapaneseImeApi.Core.SystemDictionary.Structures;
using JapaneseImeApi.Core.Util;
using ValueType = JapaneseImeApi.Core.SystemDictionary.ValueType;

namespace JapaneseImeApi.Core;

public class DataManager
{

    private LoudsTrie _keyTrie;
    private LoudsTrie _valueTrie;
    private BitVectorBasedArray _tokenArray;
    private uint[] _frequentPos;

    public DataManager(DictionaryFileSections sections)
    {
        _keyTrie = new LoudsTrie(
            sections.KeyTrieSection.Data,
            TrieCacheSizeConstants.KeyTrieLb0CacheSize,
            TrieCacheSizeConstants.KeyTrieLb1CacheSize,
            TrieCacheSizeConstants.KeyTrieSelect0CacheSize,
            TrieCacheSizeConstants.KeyTrieSelect1CacheSize,
            TrieCacheSizeConstants.KeyTrieTermvecCacheSize);

        _valueTrie = new LoudsTrie(
            sections.ValueTrieSection.Data,
            TrieCacheSizeConstants.ValueTrieLb0CacheSize,
            TrieCacheSizeConstants.ValueTrieLb1CacheSize,
            TrieCacheSizeConstants.ValueTrieSelect0CacheSize,
            TrieCacheSizeConstants.ValueTrieSelect1CacheSize,
            TrieCacheSizeConstants.ValueTrieTermvecCacheSize);

        _tokenArray = new BitVectorBasedArray(sections.TokenArraySection.Data);

        var frequentPosData = sections.FrequentPosSection.Data;
        var frequentPosSize = frequentPosData.Length / 4;
        _frequentPos = new uint[frequentPosSize];
        for (var i = 0; i < frequentPosSize; i++)
        {
            _frequentPos[i] = BitConverter.ToUInt32(frequentPosData, i * 4);
        }

        var testVal = "ぼく";
        var encodedKey = Codec.EncodeKey(testVal);
        var back = Codec.DecodeKey(encodedKey);

        var keyResultSet = _keyTrie.FindKeyIdsOfAllPrefixes(encodedKey);
        var test2 = _keyTrie.RestoreKeyString(keyResultSet.First());
        var tokenInfos = new List<TokenInfo>();

        foreach (var result in keyResultSet)
        {
            tokenInfos.AddRange(_tokenArray.GetTokenInfos(result));
        }

        foreach (var tokenInfo in tokenInfos)
        {
            if (tokenInfo.ValueType != ValueType.DefaultValue) continue;

            var keyData = _valueTrie.RestoreKeyString(tokenInfo.IdInValueTrie);
            var value = Codec.DecodeValue(keyData);
        }

        Console.WriteLine("no Way");
    }

}