using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JapaneseImeApi.Core.Util;
using JapaneseImeApi.Packager.Util;

namespace JapaneseImeApi.Packager.Models;

public class BitVectorBasedArrayBuilder
{
    private readonly List<byte[]> _elements = [];

    private int _baseLength = 4;
    private int _stepLength = 1;

    public MemoryStream? Stream { get; private set; }

    public void Add(byte[] element) => _elements.Add(element);

    public void SetSize(int baseLength, int stepLength)
    {
        _baseLength = baseLength;
        _stepLength = stepLength;
    }

    public void Build()
    {
        var stream = new MemoryStream();

        var bitStream = new BitStream();
        var data = new List<byte>();

        foreach (var element in _elements)
        {
            // Counts how many steps are needed.
            var stepCount = 0;
            for (var length = element.Length; length > _baseLength; length -= _stepLength)
            {
                stepCount++;
            }

            // Output '0' as a beginning bit, followed by the num_steps of '1'-bits.
            var outputLength = _baseLength + stepCount + _stepLength;
            bitStream.PushBit(0);

            for (var i = 0; i < stepCount; i++)
            {
                bitStream.PushBit(1);
            }

            // Output word data (excluding '\0' termination) and then padding by '\0'
            // to align to the output length.
            data.AddRange(element);
            for (var i = 0; i < outputLength - element.Length; i++)
            {
                data.Add(0); // '\0' Termination character
            }
        }

        bitStream.PushBit(0);
        bitStream.FillPadding32();

        stream.Write(BitHelper.GetSizeBytes(bitStream.GetSize()));
        stream.Write(BitHelper.GetSizeBytes(_baseLength));
        stream.Write(BitHelper.GetSizeBytes(_stepLength));
        stream.Write(BitHelper.GetSizeBytes(0));

        stream.Write(bitStream.ToByteArray());
        stream.Write(data.ToArray());

        Stream = stream;
    }

}
