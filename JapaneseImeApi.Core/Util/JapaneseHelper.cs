using System.Text;
using JapaneseImeApi.Core.Constants.ConversionTables;
using DoubleEntry = (int Base, uint Check);

namespace JapaneseImeApi.Core.Util;

public static class JapaneseHelper
{

    public static string NormalizeVoicedSoundMark(this string input)
    {
        return ConvertUsingDoubleArray(NormalizeVoicedSound.DoubleArray, NormalizeVoicedSound.Table, input);
    }
    public static string KatakanaToHiragana(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.KatakanaToHiragana.DoubleArray, Constants.ConversionTables.KatakanaToHiragana.Table, input);
    }
    public static string FullWidthAsciiToHalfWidthAscii(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.FullWidthAsciiToHalfWidthAscii.DoubleArray, Constants.ConversionTables.FullWidthAsciiToHalfWidthAscii.Table, input);
    }
    public static string FullWidthKatakanaToHalfWidthKatakana(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.FullWidthKatakanaToHalfWidthKatakana.DoubleArray, Constants.ConversionTables.FullWidthKatakanaToHalfWidthKatakana.Table, input);
    }
    public static string HalfWidthAsciiToFullWidthAscii(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.HalfWidthAsciiToFullWidthAscii.DoubleArray, Constants.ConversionTables.HalfWidthAsciiToFullWidthAscii.Table, input);
    }
    public static string HalfWidthKatakanaToFullWidthKatakana(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.HalfWidthKatakanaToFullWidthKatakana.DoubleArray, Constants.ConversionTables.HalfWidthKatakanaToFullWidthKatakana.Table, input);
    }
    public static string HiraganaToKatakana(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.HiraganaToKatakana.DoubleArray, Constants.ConversionTables.HiraganaToKatakana.Table, input);
    }
    public static string HiraganaToRomanji(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.HiraganaToRomanji.DoubleArray, Constants.ConversionTables.HiraganaToRomanji.Table, input);
    }
    public static string KanjiNumberToArabicNumber(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.KanjiNumberToArabicNumber.DoubleArray, Constants.ConversionTables.KanjiNumberToArabicNumber.Table, input);
    }
    public static string RomanjiToHiragana(this string input)
    {
        return ConvertUsingDoubleArray(Constants.ConversionTables.RomanjiToHiragana.DoubleArray, Constants.ConversionTables.RomanjiToHiragana.Table, input);
    }

    private static string ConvertUsingDoubleArray(DoubleEntry[] doubleArray, byte[] table, string input)
    {
        int multiByteLength;
        List<byte> output = [];

        var inputArray = Encoding.UTF8.GetBytes(input);
        var tableArray = table;

        for (var i = 0; i < inputArray.Length; i += multiByteLength)
        {
            var result = doubleArray.Lookup(inputArray[i..]);
            if (result.seekto <= 0)
            {
                // Nothing to replace.
                multiByteLength = 1;
                //output.AddRange(Encoding.Convert(Encoding.UTF8, Encoding.UTF32, [inputArray[i]]));
                output.Add(inputArray[i]);
                continue;
            }

            var lookupValue = tableArray.TableSubstring(result.index);
            //output.AddRange(Encoding.Convert(Encoding.UTF8, Encoding.UTF32, lookupValue));
            output.AddRange(lookupValue);
            multiByteLength = result.seekto;
        }

        //return Encoding.UTF32.GetString(output.ToArray());
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static (int index, int seekto) Lookup(this DoubleEntry[] array, byte[] key)
    {
        int nodeIndex;
        var currentBase = array[0].Base;
        uint pos;
        var result = (index: 0, seekto: 0);

        for (var i = 0; i < key.Length; ++i)
        {
            pos = (uint)currentBase;
            nodeIndex = array[pos].Base;
            if ((uint)currentBase == array[pos].Check && nodeIndex < 0)
            {
                result = (-nodeIndex - 1, i);
            }
            pos = (uint)(currentBase + key[i] + 1);
            if ((uint)currentBase == array[pos].Check)
            {
                currentBase = array[pos].Base;
            }
            else
            {
                return result;
            }
        }

        pos = (uint)currentBase;
        nodeIndex = array[pos].Base;
        if ((uint)currentBase == array[pos].Check && nodeIndex < 0)
        {
            result.seekto = key.Length;
            result.index = -nodeIndex - 1;
        }

        return result;
    }

    private static byte[] TableSubstring(this byte[] table, int index)
    {
        for (var i = index; i < table.Length; i++)
        {
            if (table[i] == 0)
            {
                return table[new Range(index, i)];
            }
        }

        return table[index..];
    }

}