using JapaneseImeApi.Packager.Util;
using Microsoft.Extensions.Logging;
using System.Text;
using JapaneseImeApi.Core.Constants;
using JapaneseImeApi.Core.SystemDictionary;
using JapaneseImeApi.Core.SystemDictionary.Structures;
using JapaneseImeApi.Core.Util;
using BitVectorBasedArrayBuilder = JapaneseImeApi.Packager.Models.BitVectorBasedArrayBuilder;
using Codec = JapaneseImeApi.Core.Util.Codec;
using KeyInfo = JapaneseImeApi.Packager.Models.KeyInfo;
using ValueType = JapaneseImeApi.Core.SystemDictionary.ValueType;
using static System.Collections.Specialized.BitVector32;

namespace JapaneseImeApi.Packager.SystemDictionary;

public class SystemDictionaryBuilder(ILogger logger)
{
    private const int CorrectionKey = 1;
    private const int CorrectionValue = 2;

    private const int CostPenalty = 2302; // -log(1/100) * 500
    private const int MinKeyLengthToUseSmallCostEncoding = 6;

    private readonly ILogger _logger = logger;
    private readonly SystemDictionaryLoader _loader = new(logger);
    private readonly SystemDictionaryWriter _writer = new(logger);
    private readonly TokenInfoComparer _tokenInfoComparer = new();

    private readonly LoudsTrieBuilder _valueTrieBuilder = new();
    private readonly LoudsTrieBuilder _keyTrieBuilder = new();
    private readonly BitVectorBasedArrayBuilder _tokenArrayBuilder = new();
    private Dictionary<uint, int> _frequentPosMap = new();

    private readonly List<Token> _tokens = new();

    public SystemDictionaryBuilder Load(IEnumerable<string> dictionaryFiles)
    {
        _tokens.AddRange(_loader.ParseDictionaryTokens(dictionaryFiles));
        //_tokens.AddRange(ParseReadingCorrectionTokens("Data/reading_correction.tsv", _tokens));

        return this;
    }

    public SystemDictionaryBuilder Build()
    {
        var keyInfos = CreateKeyInfos(_tokens);
        _frequentPosMap = BuildFrequentPosMap(keyInfos);
        BuildValueTrie(keyInfos);
        BuildKeyTrie(keyInfos);

        SetIdForValue(keyInfos);
        SetIdForKey(keyInfos);
        SortTokenInfo(keyInfos);
        SetCostType(keyInfos);
        SetPosType(keyInfos);
        SetValueType(keyInfos);

        BuildTokenArray(keyInfos);

        var testStr = "ぼく";

        var keyTest = new LoudsTrie(
            _keyTrieBuilder.Stream.ToArray(),
            TrieCacheSizeConstants.KeyTrieLb0CacheSize,
            TrieCacheSizeConstants.KeyTrieLb1CacheSize,
            TrieCacheSizeConstants.KeyTrieSelect0CacheSize,
            TrieCacheSizeConstants.KeyTrieSelect1CacheSize,
            TrieCacheSizeConstants.KeyTrieTermvecCacheSize);

        var arrTest = new BitVectorBasedArray(_tokenArrayBuilder.Stream.ToArray());

        var valTest = new LoudsTrie(
            _valueTrieBuilder.Stream.ToArray(),
            TrieCacheSizeConstants.ValueTrieLb0CacheSize,
            TrieCacheSizeConstants.ValueTrieLb1CacheSize,
            TrieCacheSizeConstants.ValueTrieSelect0CacheSize,
            TrieCacheSizeConstants.ValueTrieSelect1CacheSize,
            TrieCacheSizeConstants.ValueTrieTermvecCacheSize);

        var foundKey = keyTest.FindKeyIdsOfAllPrefixes(Codec.EncodeKey(testStr));
        var doubleCheck = keyTest.RestoreKeyString(foundKey.First());

        var tokens = arrTest.GetTokenInfos(foundKey.First());

        return this;
    }

    public void WriteTo(string file)
    {
        var frequentPosArray = new uint[256];
        Array.Fill(frequentPosArray, 0u);
        foreach (var (key, value) in _frequentPosMap)
        {
            frequentPosArray[value] = key;
        }

        var frequentPosData = frequentPosArray.SelectMany(BitConverter.GetBytes).ToArray();

        var sections = new DictionaryFileSections
        {
            ValueTrieSection = new DictionaryFileSection(DictionarySectionConstants.ValueSectionName, _valueTrieBuilder.Stream!.GetBuffer()),
            KeyTrieSection = new DictionaryFileSection(DictionarySectionConstants.KeySectionName, _keyTrieBuilder.Stream!.GetBuffer()),
            TokenArraySection = new DictionaryFileSection(DictionarySectionConstants.TokensSectionName, _tokenArrayBuilder.Stream!.GetBuffer()),
            FrequentPosSection = new DictionaryFileSection(DictionarySectionConstants.PosSectionName, frequentPosData)
        };

        _writer.WriteSections(file, sections);
    }

    private void SetIdForValue(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            foreach (var tokenInfo in keyInfo.TokenInfos)
            {
                var encodedValue = Codec.EncodeValue(tokenInfo.Token.Value);
                tokenInfo.IdInValueTrie = _valueTrieBuilder.GetId(encodedValue);
            }
        }
    }

    private void SetIdForKey(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            var encodedKey = Codec.EncodeKey(keyInfo.Key);
            keyInfo.IdInKeyTrie = _keyTrieBuilder.GetId(encodedKey);
        }
    }

    private void SortTokenInfo(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            keyInfo.TokenInfos.Sort(_tokenInfoComparer);
        }
    }

    private void SetCostType(List<KeyInfo> keyInfos)
    {
        var heterophoneValues = new HashSet<string>();
        var seenReadingMap = new Dictionary<string, string>();

        foreach (var keyInfo in keyInfos)
        {
            foreach (var tokenInfo in keyInfo.TokenInfos)
            {
                var token = tokenInfo.Token;

                if (heterophoneValues.Contains(token.Value))
                {
                    continue;
                }

                if (!seenReadingMap.TryGetValue(token.Value, out var existingKey))
                {
                    seenReadingMap[token.Value] = token.Value;
                    continue;
                }

                if (existingKey == token.Key)
                {
                    continue;
                }

                heterophoneValues.Add(token.Value);
            }
        }

        var minKeyLength = MinKeyLengthToUseSmallCostEncoding;
        foreach (var keyInfo in keyInfos)
        {
            // TODO check if this is the right way to get utf8 length?
            if (Encoding.UTF8.GetBytes(keyInfo.Key).Length < minKeyLength)
            {
                // Do not use small cost encoding for short keys.
                continue;
            }

            if (HasHomonymsInSamePos(keyInfo))
            {
                continue;
            }

            if (HasHeterophones(keyInfo, heterophoneValues))
            {
                // We want to keep the cost order for LookupReverse().
                continue;
            }

            foreach(var tokenInfo in keyInfo.TokenInfos)
            {
                if (tokenInfo.Token.Cost < 0x100)
                {
                    // Small cost encoding ignores lower 8 bits.
                    continue;
                }

                tokenInfo.CostType = CostType.CanUseSmallEncoding;
            }
        }
    }

    private void SetPosType(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            for (var i = 0; i < keyInfo.TokenInfos.Count; i++)
            {
                var tokenInfo = keyInfo.TokenInfos[i];
                var token = tokenInfo.Token;
                var pos = GetCombinedPos((ushort)token.LeftId, (ushort)token.RightId);

                if (_frequentPosMap.TryGetValue(pos, out var entry))
                {
                    tokenInfo.PosType = PosType.FrequentPos;
                    tokenInfo.IdInFrequentPosMap = entry;
                }

                if (i < 1) continue;

                var prevTokenInfo = keyInfo.TokenInfos[i-1];
                var prevPos = GetCombinedPos((ushort)prevTokenInfo.Token.LeftId, (ushort)prevTokenInfo.Token.RightId);

                if (prevPos == pos)
                {
                    // We can overwrite FREQUENT_POS
                    tokenInfo.PosType = PosType.SameAsPrevPos;
                }
            }
        }
    }

    private void SetValueType(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            for (var i = 1; i < keyInfo.TokenInfos.Count; i++)
            {
                var prevTokenInfo = keyInfo.TokenInfos[i - 1];
                var tokenInfo = keyInfo.TokenInfos[i];

                if (tokenInfo.ValueType != ValueType.AsIsHiragana &&
                    tokenInfo.ValueType != ValueType.AsIsKatakana &&
                    tokenInfo.Token.Value == prevTokenInfo.Token.Value)
                {
                    tokenInfo.ValueType = ValueType.SameAsPrevValue;
                }

            }
        }
    }

    private void BuildTokenArray(List<KeyInfo> keyInfos)
    {
        // Here we make a reverse lookup table as follows:
        //   |key_info_list[X].id_in_key_trie| -> |key_info_list[X]|
        // assuming |key_info_list[X].id_in_key_trie| is unique and successive.
        var idToKeyInfoTable = new KeyInfo[keyInfos.Count];
        foreach(var keyInfo in keyInfos)
        {
            idToKeyInfoTable[keyInfo.IdInKeyTrie] = keyInfo;
        }

        foreach(var keyInfo in idToKeyInfoTable)
        {
            // I do not understand how this was supposed to work in the original code?!?
            if (keyInfo == null) continue;

            var encodedTokens = Codec.EncodeTokens(keyInfo.TokenInfos);
            _tokenArrayBuilder.Add(encodedTokens);
        }

        _tokenArrayBuilder.Add([Codec.GetTokensTerminationFlag()]);
        _tokenArrayBuilder.Build();
    }

    private bool HasHeterophones(KeyInfo keyInfo, HashSet<string> heteronymValues)
    {
        return keyInfo.TokenInfos.Any(tokenInfo => heteronymValues.Contains(tokenInfo.Token.Value));
    }
    private bool HasHomonymsInSamePos(KeyInfo keyInfo)
    {
        if (keyInfo.TokenInfos.Count == 1) return false;

        var seenSet = new HashSet<int>();
        foreach (var tokenInfo in keyInfo.TokenInfos)
        {
            var token = tokenInfo.Token;
            var pos = GetCombinedPos((ushort)token.LeftId, (ushort)token.RightId);
            if (!seenSet.Add((int)pos))
            {
                // Insertion failed, which means we already have |pos|.
                return true;
            }
        }
        
        return false;
    }

    private void BuildValueTrie(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            foreach (var tokenInfo in keyInfo.TokenInfos)
            {
                if (tokenInfo.ValueType is ValueType.AsIsHiragana or ValueType.AsIsKatakana) continue;

                _valueTrieBuilder.Add(Codec.EncodeValue(tokenInfo.Token.Value));
            }
        }

        _valueTrieBuilder.Build();
    }

    private void BuildKeyTrie(List<KeyInfo> keyInfos)
    {
        foreach (var keyInfo in keyInfos)
        {
            _keyTrieBuilder.Add(Codec.EncodeKey(keyInfo.Key));
        }

        _keyTrieBuilder.Build();
    }

    private Dictionary<uint, int> BuildFrequentPosMap(List<KeyInfo> keyInfos)
    {
        var frequentPosMap = new Dictionary<uint, int>();
        var posMap = new Dictionary<uint, int>();

        // Calculate the frequency of each PartOfSpeech
        foreach (var keyInfo in keyInfos)
        {
            foreach (var tokenInfo in keyInfo.TokenInfos)
            {
                var token = tokenInfo.Token;
                var key = GetCombinedPos((ushort)token.LeftId, (ushort)token.RightId);
                posMap.TryAdd(key, 0);
                posMap[key]++;
            }
        }

        // Calculate frequency map
        var frequencyMap = new Dictionary<int, int>();
        foreach (var (_, frequency) in posMap)
        {
            frequencyMap.TryAdd(frequency, 0);
            frequencyMap[frequency]++;
        }

        // Find frequency threshold
        var posFrequencyCount = 0;
        var frequencyThreshold = int.MaxValue;
        foreach (var (key, value) in frequencyMap.OrderByDescending(entry => entry.Key))
        {
            if (posFrequencyCount + value > 255) break;

            frequencyThreshold = key;
            posFrequencyCount += value;
        }

        _logger.LogDebug($"{nameof(SystemDictionaryBuilder)}: posFrequencyCount [{posFrequencyCount}]");
        _logger.LogDebug($"{nameof(SystemDictionaryBuilder)}: frequencyThreshold [{frequencyThreshold}]");

        var frequentPosId = 0;
        var tokenCount = 0;
        foreach (var (combinedPos, frequency) in posMap)
        {
            if (frequency < frequencyThreshold) continue;

            frequentPosMap[combinedPos] = frequentPosId++;
            tokenCount += frequency;
        }

        if (frequentPosId != posFrequencyCount) throw new Exception("Inconsistent result to find frequent pos!");

        _logger.LogDebug($"{nameof(SystemDictionaryBuilder)}: frequentPosId [{frequentPosId}] contains [{tokenCount}] tokens");

        return frequentPosMap;
    }

    private static uint GetCombinedPos(ushort leftId, ushort rightId)
    {
        return (uint)((leftId << 16) | rightId);
    }

    private static List<KeyInfo> CreateKeyInfos(List<Token> tokens)
    {
        var keyInfoList = new List<KeyInfo>();

        tokens = tokens.OrderBy(token => token.Key).ToList();

        KeyInfo? keyInfo = null;
        foreach (var token in tokens)
        {
            if (keyInfo?.Key != token.Key)
            {
                keyInfo = new KeyInfo(token.Key);
                keyInfoList.Add(keyInfo);
            }

            keyInfo.TokenInfos.Add(new TokenInfo(token) with
            {
                ValueType = GetValueType(token)
            });

        }

        if (keyInfo != null)
        {
            keyInfoList.Add(keyInfo);
        }

        return keyInfoList;
    }

    private static ValueType GetValueType(Token token)
    {
        if (token.Value == token.Key) return ValueType.AsIsHiragana;
        if (token.Value == token.Key.HiraganaToKatakana()) return ValueType.AsIsKatakana;

        return ValueType.DefaultValue;
    }

    private List<Token> ParseReadingCorrectionTokens(string readingCorrectionFile,
            List<Token> systemTokens)
    {

        // Order first by Value and then Key this allows to use a faster query later on.
        var orderedTokens = systemTokens.OrderBy(token => token.Value).ThenBy(token => token.Key).ToList();
        var lookupId = 0;
        var tokenLookup = new Dictionary<string, int>();
        foreach (var token in orderedTokens)
        {
            tokenLookup.TryAdd(token.Value, lookupId);
            lookupId++;
        }

        var lines = File.ReadAllLines(readingCorrectionFile);
        var correctionTokens = new List<Token>();

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                continue;
            }

            var (key, value) = ParseReadingCorrectionTsv(line.Split("\t"));

            if (orderedTokens.Any(token => token.Key == key && token.Value == value))
            {
                _logger.LogDebug($"{nameof(SystemDictionaryBuilder)}: Key [{key}] and Value [{value}] already exist in system dictionary.");
            }

            //var currentSimilarTokenId = orderedTokens.FindIndex(Token => Token.Value == value);
            var currentSimilarTokenId = tokenLookup.GetValueOrDefault(value, -1);

            if (currentSimilarTokenId == -1)
            {
                _logger.LogDebug($"{nameof(SystemDictionaryBuilder)}: Did not find value [{value}] in system dictionary.");
                continue;
            }

            // Loop through neighbouring tokens that have the same value because the list is sorted by value.
            // ! The list needs to be ordered by values first
            var maxToken = orderedTokens[currentSimilarTokenId];
            var currToken = orderedTokens[currentSimilarTokenId];
            while (currToken.Value == value)
            {

                if (maxToken.Cost < currToken.Cost)
                {
                    maxToken = currToken;
                }

                if (orderedTokens.Count <= currentSimilarTokenId)
                {
                    currToken = default;
                }

                currToken = orderedTokens[++currentSimilarTokenId];
            }

            correctionTokens.Add(maxToken with
            {
                Key = key,
                Cost = maxToken.Cost + CostPenalty,
                TokenAttribute = (int) TokenAttribute.None
            });

        }

        return correctionTokens;
    }

    private static (string key, string value) ParseReadingCorrectionTsv(string[] columns)
    {
        CorrectionTsvGuard(columns);
        return (columns[CorrectionKey], columns[CorrectionValue]);
    }

    private static void CorrectionTsvGuard(string[] columns)
    {
        if (columns.Length >= 3) return;

        throw new Exception($"Invalid ReadingCorrection data, received [{columns.Length}] columns instead of [2]");
    }

}
