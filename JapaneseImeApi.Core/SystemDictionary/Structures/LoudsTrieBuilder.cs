using JapaneseImeApi.Core.Util;

namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public class LoudsTrieBuilder
{
    private readonly WordComparer _stringComparer = new();
    private List<byte[]> _words = [];
    private Dictionary<byte[], int> _wordLookup;
    private int[] _ids = [];

    public MemoryStream? Stream { get; private set; }

    public void Add(byte[] word) => _words.Add(word);

    public int GetId(byte[] word)
    {
        //var comparer = new WordComparer();
        //foreach (var kvp in _wordLookup)
        //{
        //    if (comparer.Compare(kvp.Key, word) == 0)
        //    {
        //        Console.WriteLine("huh?");
        //        return _ids[kvp.Value];
        //    }
        //}

        var originalId = _wordLookup.GetValueOrDefault(word, -1);

        return originalId < 0 ? -1 : _ids[originalId];
    }

    public void Build()
    {
        var stream = new MemoryStream();

        _words.Sort(_stringComparer);
        _words = _words.DistinctBy(x => x, _stringComparer).ToList();

        _wordLookup = new Dictionary<byte[], int>(new WordComparer());

        var entries = new List<LoudsTrieEntry>();

        var wordId = 0;
        foreach (var word in _words)
        {
            entries.Add(new LoudsTrieEntry(word, wordId));
            _wordLookup.Add(word, wordId);
            wordId++;
        }

        _ids = new int[_words.Count];
        Array.Fill(_ids, -1);

        var trieStream = new BitStream();
        var terminalStream = new BitStream();
        var edgeCharacter = new List<byte>();

        // Push root Node.
        trieStream.PushBit(1);
        trieStream.PushBit(0);
        edgeCharacter.Add(0x00); // '\0'
        terminalStream.PushBit(0);

        var id = 0;
        for (var depth = 0; entries.Any(); depth++)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var word = entries[i].Word;

                // Hacky solution but pad the word with '\0' characters to not get an index out of bounds.
                if (word.Length > depth && (i == 0 || !CurrentEqualsPrevious(entries, i, depth)))
                {
                    // This is the first string of this node. Output an edge.
                    trieStream.PushBit(1);
                    edgeCharacter.Add(word[depth]);

                    if (word.Length == depth + 1)
                    {
                        // This is a terminal node.
                        // Note that the terminal string should be at the first of
                        // strings sharing the node. So the check above should work well.
                        terminalStream.PushBit(1);
                        _ids[entries[i].OriginalId] = id++;
                    }
                    else
                    {
                        // Not a terminal node.
                        terminalStream.PushBit(0);
                    }
                }

                if (i == entries.Count - 1 || !CurrentEqualsNext(entries, i, depth))
                {
                    // This is the last child for the parent.
                    trieStream.PushBit(0);
                }
            }

            entries.RemoveAll(entry => entry.Word.Length < depth + 1);

        }

        trieStream.FillPadding32();
        terminalStream.FillPadding32();

        stream.Write(BitHelper.GetSizeBytes(trieStream.GetSize()));
        stream.Write(BitHelper.GetSizeBytes(terminalStream.GetSize()));
        // The num bits of each character annotated to each edge.
        stream.Write(BitHelper.GetSizeBytes(8));
        stream.Write(BitHelper.GetSizeBytes(edgeCharacter.Count));

        stream.Write(trieStream.ToByteArray());
        stream.Write(terminalStream.ToByteArray());
        stream.Write(edgeCharacter.ToArray());

        Stream = stream;
    }

    // This method will throw when i = count-1 so make sure to check that first.
    private bool CurrentEqualsNext(List<LoudsTrieEntry> entries, int i, int depth)
    {
        var currentWord = entries[i].Word;
        // Pad to be in line with depth.
        var nextWord = entries[i + 1].Word;

        return CompareWords(currentWord, nextWord, depth);
    }

    // A bit of a hacky method but compare if two words are the same till a certain depth.
    private static bool CompareWords(byte[] x, byte[] y, int depth)
    {
        for (var i = 0; i < depth; i++)
        {
            if (x[i] != y[i]) return false;
        }

        return true;
    }

    // This method will throw when i = 0 so make sure to check that first.
    private bool CurrentEqualsPrevious(List<LoudsTrieEntry> entries, int i, int depth)
    {
        var currentWord = entries[i].Word;
        // Pad to be in line with depth.
        var previousWord = PadArray(entries[i - 1].Word, depth + 1);
        Array.Copy(previousWord, previousWord, previousWord.Length);

        return _stringComparer.Compare(currentWord, previousWord) == 0;
    }

    private byte[] PadArray(byte[] baseArray, int length)
    {
        if (baseArray.Length >= length) return baseArray;

        var resultArray = new byte[length];

        for (var i = 0; i < length; i++)
        {
            resultArray[i] = 0;
        }

        Array.Copy(baseArray, resultArray, baseArray.Length);

        return resultArray;
    }

}
