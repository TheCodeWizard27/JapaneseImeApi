namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public class LoudsTrieEntry(byte[] word, int originalId)
{
    public byte[] Word { get; set; } = word;
    public int OriginalId { get; set; } = originalId;
}
