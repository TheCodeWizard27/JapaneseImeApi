using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JapaneseImeApi.Core.Util;

namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public class LoudsTrie
{
    private readonly int _loudsSize;
    private readonly int _terminalSize;
    private readonly int _characterBitCount;
    private readonly int _edgeCharacterSize;

    private readonly Louds _louds;
    private readonly SimpleSuccinctBitVectorIndex _terminalBitVector;
    private readonly byte[] _edgeCharacters;

    // Reads a binary image data, which is compatible with rx.
    // The format is as follows:
    // [trie size: little endian 4byte int]
    // [terminal size: little endian 4byte int]
    // [num bits for each character annotated to an edge:
    //  little endian 4 byte int. Currently, this class supports only 8-bits]
    // [edge character image size: little endian 4 byte int]
    // [trie image: "trie size" bytes]
    // [terminal image: "terminal size" bytes]
    // [edge character image: "edge character image size" bytes]
    //
    // Here, "terminal" means "the node is one of the end of a word."
    // For example, if we have a trie for "aa" and "aaa", the trie looks like:
    //         [0]
    //        a/
    //       [1]
    //      a/
    //     [2]
    //    a/
    //   [3]
    // In this case, [0] and [1] are not terminal (as the original words contains
    // neither "" nor "a"), and [2] and [3] are terminal.
    public LoudsTrie(byte[] data, 
        int loudsLb0CacheSize, int loudsLb1CacheSize,
        int loudsSelect0CacheSize, int loudsSelect1CacheSize,
        int termVecLb1CacheSize)
    {
        using var stream = new MemoryStream(data);
        var reader = new ByteStreamReader(stream);

        _loudsSize = reader.ReadNextInt();
        _terminalSize = reader.ReadNextInt();
        _characterBitCount = reader.ReadNextInt();
        _edgeCharacterSize = reader.ReadNextInt();

        var loudsData = reader.Read(_loudsSize);
        var terminalData = reader.Read(_terminalSize);
        var edgeCharacterData = reader.Read(_edgeCharacterSize);

        _louds = new Louds(loudsData, loudsLb0CacheSize, loudsLb1CacheSize, loudsSelect0CacheSize,
            loudsSelect1CacheSize);
        _terminalBitVector = new SimpleSuccinctBitVectorIndex(terminalData, 0, termVecLb1CacheSize);
        _edgeCharacters = edgeCharacterData;
    }

    public HashSet<int> FindKeyIdsOfAllPrefixes(byte[] key)
    {
        var resultSet = new HashSet<int>();
        var node = new LoudsNode(); // Root node

        for (var i = 0; i < key.Length;) // i incrementation purposefully later in the loop.
        {
            if (!MoveToChildByLabel(key[i], node))
            {
                break;
            }
            i++;
            if (IsTerminalNode(node))
            {
                resultSet.Add(GetKeyIdOfTerminalNode(node));
            }
        }

        return resultSet;
    }

    public byte[] RestoreKeyString(int keyId)
    {
        return keyId < 0 ? [] : RestoreKeyString(GetTerminalNodeFromKeyId(keyId));
    }

    private LoudsNode GetTerminalNodeFromKeyId(int keyId)
    {
        var node = new LoudsNode();
        var nodeId = _terminalBitVector.Select1(keyId + 1) + 1;
        _louds.InitNodeFromNodeId(nodeId, node);
        return node;
    }

    private byte[] RestoreKeyString(LoudsNode node)
    {
        var result = new List<byte>();
        while (!_louds.IsRoot(node))
        {
            result.Add(GetEdgeLabelToParentNode(node));
            _louds.MoveToParent(node);
        }

        result.Reverse();

        result.Add(0);

        return result.ToArray();
    }

    private int GetKeyIdOfTerminalNode(LoudsNode node)
    {
        return _terminalBitVector.Rank1(node.NodeId - 1);
    }

    private bool IsTerminalNode(LoudsNode node)
    {
        return _terminalBitVector.Get(node.NodeId - 1) != 0;
    }

    private bool MoveToChildByLabel(byte label, LoudsNode node)
    {
        MoveToFirstChild(node);
        while (IsValidNode(node))
        {
            if (GetEdgeLabelToParentNode(node) == label)
            {
                return true;
            }

            MoveToNextSibling(node);
        }

        return false;
    }

    private bool IsValidNode(LoudsNode node)
    {
        return _louds.IsValidNode(node);
    }

    private void MoveToNextSibling(LoudsNode node)
    {
        node.EdgeId++;
        node.NodeId++;
    }
    private void MoveToFirstChild(LoudsNode node)
    {
        _louds.MoveToFirstChild(node);
    }

    private byte GetEdgeLabelToParentNode(LoudsNode node)
    {
        return _edgeCharacters[node.NodeId - 1];
    }

}
