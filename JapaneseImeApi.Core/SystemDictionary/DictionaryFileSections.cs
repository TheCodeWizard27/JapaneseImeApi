namespace JapaneseImeApi.Core.SystemDictionary;

public class DictionaryFileSections
{

    public required DictionaryFileSection ValueTrieSection { get; init; }
    public required DictionaryFileSection KeyTrieSection { get; init; }
    public required DictionaryFileSection TokenArraySection { get; init; }
    public required DictionaryFileSection FrequentPosSection { get; init; }

}
