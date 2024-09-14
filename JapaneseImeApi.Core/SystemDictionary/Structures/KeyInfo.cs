namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public record KeyInfo(string Key)
{
    public int IdInKeyTrie { get; set; } = -1;
    public List<TokenInfo> TokenInfos { get; init; } = new();
};
