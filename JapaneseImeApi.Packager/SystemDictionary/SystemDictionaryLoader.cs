using JapaneseImeApi.Core.SystemDictionary;
using JapaneseImeApi.Core.Util;
using Microsoft.Extensions.Logging;

namespace JapaneseImeApi.Packager.SystemDictionary;

public class SystemDictionaryLoader(
    ILogger logger)
{
    private const int DictionaryKey = 0;
    private const int DictionaryLeftId = 1;
    private const int DictionaryRightId = 2;
    private const int DictionaryCost = 3;
    private const int DictionaryValue = 4;

    private const int DictionaryLabel = 5;

    private const string SpellingCorrectionLabel = "SPELLING_CORRECTION";
    private const string ZipCodeLabel = "ZIP_CODE";
    private const string EnglishLabel = "ENGLISH";

    public List<Token> ParseDictionaryTokens(IEnumerable<string> dictionaryFiles)
    {
        logger.LogDebug($"{nameof(SystemDictionaryLoader)}: Loading files");

        var loadingTasks = dictionaryFiles
            .Select(fileName => File.ReadAllLinesAsync(fileName))
            .ToList();

        Task.WhenAll(loadingTasks.ToArray()).Wait();

        var dictionaryEntry = loadingTasks.Select(task => task.Result);
        var tokens = new List<Token>();

        foreach (var entry in dictionaryEntry)
        {
            tokens.AddRange(ParseLines(entry));
        }

        logger.LogDebug($"{nameof(SystemDictionaryLoader)}: Parsed [{tokens.Count}] tokens");

        return tokens;
    }

    private static List<Token> ParseLines(string[] lines)
    {
        return lines
            .Select(line => ParseTsv(line.Split("\t")))
            .ToList();
    }

    private static Token ParseTsv(string[] columns)
    {
        TsvGuard(columns);

        var token = new Token
        {
            Key = columns[DictionaryKey].NormalizeVoicedSoundMark(),
            Value = columns[DictionaryValue].NormalizeVoicedSoundMark(),
            Cost = int.TryParse(columns[DictionaryCost], out var cost)
                ? cost
                : throw new Exception($"Invalid cost value [{columns[DictionaryCost]}]"),
            LeftId = int.TryParse(columns[DictionaryLeftId], out var leftId)
                ? leftId
                : throw new Exception($"Invalid leftId value [{columns[DictionaryLeftId]}]"),
            RightId = int.TryParse(columns[DictionaryRightId], out var rightId)
                ? rightId
                : throw new Exception($"Invalid rightId value [{columns[DictionaryRightId]}]")
        };

        if (columns.Length == 6)
        {
            // If there is a 6th column parse in a special way.
            return ParseTsvSpecial(token, columns[DictionaryLabel]);
        }

        return token;
    }

    private static Token ParseTsvSpecial(Token baseToken, string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return baseToken;
        }

        if (label.StartsWith(SpellingCorrectionLabel))
        {
            return baseToken with
            {
                TokenAttribute = TokenAttribute.SpellingCorrection
            };
        }

        if (label.StartsWith(ZipCodeLabel))
        {
            return baseToken with
            {
                LeftId = -1, // TODO Read from PosMatcher ZipCodeId
                RightId = -1 // TODO Read from PosMatcher ZipCodeId
            };
        }

        if (label.StartsWith(EnglishLabel))
        {
            return baseToken with
            {
                LeftId = -1, // TODO Read from PosMatcher IsolatedWordId
                RightId = -1 // TODO Read from PosMatcher IsolatedWordId
            };
        }

        throw new Exception($"Couldn't parse special label with value [{label}]");
    }

    private static void TsvGuard(string[] columns)
    {
        if (columns.Length >= 5) return;

        throw new Exception($"Invalid Dictionary data, received [{columns.Length}] columns instead of [5]");
    }

}
