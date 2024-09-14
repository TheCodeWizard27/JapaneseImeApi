using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public class Louds
{

    private readonly SimpleSuccinctBitVectorIndex _index;
    private readonly int _select0CacheSize;
    private readonly int _select1CacheSize;
    private readonly int[] _selectCache;
    private readonly int _select1CacheOffset;

    public Louds(byte[] data,
        int bitVecLb0CacheSize, int bitVecLb1CacheSize,
        int select0CacheSize, int select1CacheSize)
    {
        _index = new SimpleSuccinctBitVectorIndex(data, bitVecLb0CacheSize, bitVecLb1CacheSize);

        _select0CacheSize = Math.Min(select0CacheSize, _index.Get0BitCount());
        _select1CacheSize = Math.Min(select1CacheSize, _index.Get1BitCount());

        var cacheSize = _select0CacheSize + _select1CacheSize;
        if (cacheSize <= 0) return;

        _selectCache = new int[cacheSize];
        if (_select0CacheSize > 0)
        {
            _selectCache[0] = 0;
            for (var i = 1; i < _select0CacheSize; i++)
            {
                _selectCache[i] = _index.Select0(i) + 1;
            }
        }

        if (_select1CacheSize > 0)
        {
            var offset = _select1CacheOffset = _select0CacheSize;
            _selectCache[offset] = 0;
            for (var i = 1; i < _select1CacheSize; i++)
            {
                _selectCache[offset + i] = _index.Select1(i);
            }
        }
    }

    public bool IsValidNode(LoudsNode node)
    {
        return _index.Get(node.EdgeId) != 0;
    }

    public void MoveToFirstChild(LoudsNode node)
    {
        node.EdgeId = node.NodeId < _select0CacheSize 
            ? _selectCache[node.NodeId] : _index.Select0(node.NodeId) + 1;
        node.NodeId = node.EdgeId - node.NodeId + 1;
    }

    public void InitNodeFromNodeId(int nodeId, LoudsNode node)
    {
        node.NodeId = nodeId;
        node.EdgeId = nodeId < _select1CacheSize
            ? _selectCache[_select1CacheOffset + nodeId]
            : _index.Select1(nodeId);
    }

    public bool IsRoot(LoudsNode node)
    {
        return node.NodeId == 1;
    }

    public void MoveToParent(LoudsNode node)
    {
        node.NodeId = node.EdgeId - node.NodeId + 1;
        node.EdgeId = node.NodeId < _select1CacheSize
            ? _selectCache[_select1CacheOffset + node.NodeId]
            : _index.Select1(node.NodeId);
    }

}
