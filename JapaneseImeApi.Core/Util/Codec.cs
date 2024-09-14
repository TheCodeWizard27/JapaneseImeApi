using System.Text;
using JapaneseImeApi.Core.Constants;
using JapaneseImeApi.Core.SystemDictionary;
using ValueType = JapaneseImeApi.Core.SystemDictionary.ValueType;

namespace JapaneseImeApi.Core.Util;

public static class Codec
{
    public static byte[] EncodeTokens(List<TokenInfo> tokens)
    {
        var result = new List<byte>();
        for (var i = 0; i < tokens.Count; i++)
        {
            result.AddRange(EncodeToken(tokens, i));
        }

        return result.ToArray();
    }

    public static byte GetTokensTerminationFlag()
    {
        return CodecConstants.TokenTerminationFlag;
        //return Encoding.UTF8.GetString([]);
    }

    public static List<TokenInfo> DecodeTokens(byte[] data)
    {
        var tmpData = data;
        var results = new List<TokenInfo>();

        var lastTokenReached = false;

        while (!lastTokenReached)
        {
            lastTokenReached = DecodeToken(tmpData, out var tokenInfo, out var readBytes);
            tmpData = tmpData[readBytes..]; // Maybe needs optimizing.
            results.Add(tokenInfo);
        }

        return results;
    }

    private static bool DecodeToken(byte[] data, out TokenInfo tokenInfo, out int readBytes)
    {
        tokenInfo = new TokenInfo(new Token());
        var flags = ReadFlags(data[0]);

        if ((flags & CodecConstants.SpellingCorrectionFlag) == CodecConstants.SpellingCorrectionFlag)
        {
            tokenInfo.Token.TokenAttribute = TokenAttribute.SpellingCorrection;
        }

        var offset = 1;
        offset = DecodePos(data, flags, offset, tokenInfo);
        offset = DecodeCost(data, offset, tokenInfo);
        offset = DecodeValueInfo(data, flags, offset, tokenInfo);

        readBytes = offset;

        return (flags & CodecConstants.LastTokenFlag) == CodecConstants.LastTokenFlag;
    }

    private static int DecodePos(byte[] data, byte flags, int offset, TokenInfo tokenInfo)
    {
        var posFlag = (flags & CodecConstants.PosTypeFlagMask);
        switch (posFlag)
        {
            case CodecConstants.FrequentPosFlag:
                var posId = data[offset++];
                tokenInfo.PosType = PosType.FrequentPos;
                tokenInfo.IdInFrequentPosMap = posId;
                break;
            case CodecConstants.SameAsPrevPosFlag:
                tokenInfo.PosType = PosType.SameAsPrevPos;
                break;
            case CodecConstants.MonoPosFlag:
                var id = ((data[offset + 1] << 8) | data[offset]);
                tokenInfo.Token.LeftId = tokenInfo.Token.RightId = id;
                offset += 2;
                break;
            case CodecConstants.FullPosFlag:
                tokenInfo.Token.LeftId = data[offset];
                tokenInfo.Token.LeftId += ((data[offset + 1] & 0x0f) << 8);
                tokenInfo.Token.RightId = data[offset + 1] >> 4;
                tokenInfo.Token.RightId += data[offset + 2] << 4;
                offset += 3;
                break;
            default:
                throw new Exception("Invalid pos flag");
        }

        return offset;
    }

    private static int DecodeCost(byte[] data, int offset, TokenInfo tokenInfo)
    {
        if ((data[offset] & CodecConstants.SmallCostFlag) == CodecConstants.SmallCostFlag)
        {
            tokenInfo.Token.Cost = ((data[offset] & CodecConstants.SmallCostFlag) << 8);
            offset++;
        }
        else
        {
            tokenInfo.Token.Cost = data[offset] << 8;
            tokenInfo.Token.Cost += data[offset + 1];
            offset += 2;
        }

        return offset;
    }

    private static int DecodeValueInfo(byte[] data, byte flags, int offset, TokenInfo tokenInfo)
    {
        var valueFlag = (flags & CodecConstants.ValueTypeFlagMask);
        switch (valueFlag)
        {
            case CodecConstants.AsIsHiraganaValueFlag:
                tokenInfo.ValueType = ValueType.AsIsHiragana;
                break;
            case CodecConstants.AsIsKatakanaValueFlag:
                tokenInfo.ValueType = ValueType.AsIsKatakana;
                break;
            case CodecConstants.SameAsPrevValueFlag:
                tokenInfo.ValueType = ValueType.SameAsPrevValue;
                break;
            case CodecConstants.NormalValueFlag:
                tokenInfo.ValueType = ValueType.DefaultValue;
                var id = (uint)((data[offset + 1] << 8) | data[offset]);
                if ((flags & CodecConstants.UpperCrammedIDMask) == CodecConstants.UpperCrammedIDMask)
                {
                    id |= (uint)((data[0] & CodecConstants.UpperCrammedIDMask) << 16);
                    offset += 2;
                }
                else
                {
                    id |= (uint)(data[offset + 2] << 16);
                    offset += 3;
                }

                tokenInfo.IdInValueTrie = (int) id;
                break;
            default:
                throw new Exception("Invalid Value Flag");
                break;
        }

        return offset;
    }

    private static byte ReadFlags(byte flags)
    {
        if((flags & CodecConstants.CrammedIDFlag) == CodecConstants.CrammedIDFlag)
        {
            flags &= CodecConstants.UpperFlagsMask;
        }

        return flags;
    }

    private static byte[] EncodeToken(List<TokenInfo> tokens, int id)
    {
        var flags = GetFlagsForToken(tokens, id);

        var buffer = new byte[9];
        buffer[0] = flags;
        var offset = 1;

        var tokenInfo = tokens[id];
        offset = EncodePos(tokenInfo, flags, buffer, offset);       // <= 3 bytes
        offset = EncodeCost(tokenInfo, buffer, offset);             // <= 2 bytes
        offset = EncodeValueInfo(tokenInfo, flags, buffer, offset); // <= 3 bytes

        return buffer[..offset];
        // Encoding is too much of a hassle.
        //// TODO check if the right encoding.
        //return Encoding.UTF8.GetString(buffer[..offset]);
    }

    private static int EncodeValueInfo(TokenInfo tokenInfo, byte flags, byte[] buffer, int offset)
    {
        var valueTypeFlag = flags & CodecConstants.ValueTypeFlagMask;
        if (valueTypeFlag != CodecConstants.NormalValueFlag)
        {
            // No need to store id for word trie.
            return offset;
        }

        var id = tokenInfo.IdInValueTrie;
        if (id > CodecConstants.ValueTrieIdMax)
        {
            throw new Exception($"Token with id [{id}] too large for Trie!");
        }

        if ((flags & CodecConstants.CrammedIDFlag) == CodecConstants.CrammedIDFlag)
        {
            buffer[offset] = (byte)(id & 255);
            buffer[offset + 1] = (byte)((id >> 8) & 255);
            buffer[0] |= (byte)((id >> 16) & CodecConstants.UpperCrammedIDMask);
            offset += 2;
        }
        else
        {
            buffer[offset] = (byte)(id & 255);
            buffer[offset+1] = (byte)((id >> 8) & 255);
            buffer[offset+2] = (byte)((id >> 16) & 255);
            offset += 3;
        }

        return offset;
    }

    private static int EncodeCost(TokenInfo tokenInfo, byte[] buffer, int offset)
    {
        if (tokenInfo.CostType == CostType.CanUseSmallEncoding)
        {
            buffer[offset] = (byte)((tokenInfo.Token.Cost >> 8) | CodecConstants.SmallCostFlag);
            offset++;
        }
        else
        {
            buffer[offset] = (byte)(tokenInfo.Token.Cost >> 8);
            buffer[offset + 1] = (byte)(tokenInfo.Token.Cost  & 0xFF);
            offset += 2;
        }

        return offset;
    }

    private static int EncodePos(TokenInfo tokenInfo, byte flags, byte[] buffer, int offset)
    {
        byte posFlag = (byte)(flags & CodecConstants.PosTypeFlagMask);
        var lid = tokenInfo.Token.LeftId;
        var rid = tokenInfo.Token.RightId;

        switch (posFlag)
        {
            case CodecConstants.FullPosFlag:
                // 3 Bytes
                buffer[offset] = (byte)(lid & 255);
                buffer[offset + 1] = (byte)(((rid << 4) & 255) | (lid >> 8));
                buffer[offset + 2] = (byte)((rid >> 4) & 255);
                offset += 3;
                break;
            case CodecConstants.MonoPosFlag:
                // 2 bytes
                buffer[offset] = (byte)(lid & 255);
                buffer[offset + 1] = (byte)(lid >> 8);
                offset += 2;
                break;
            case CodecConstants.FrequentPosFlag:
                // Frequent 1 byte pos.
                buffer[offset] = (byte)tokenInfo.IdInFrequentPosMap;
                offset += 1;
                break;
            case CodecConstants.SameAsPrevPosFlag:
                break;
            default:
                throw new Exception($"Couldn't EncodePos for Token with key [{tokenInfo.Token.Key}]");
        }

        return offset;
    }

    private static byte GetFlagsForToken(List<TokenInfo> tokens, int id)
    {
        byte flags = 0;
        if (id == tokens.Count - 1)
        {
            flags |= CodecConstants.LastTokenFlag;
        }

        var tokenInfo = tokens[id];
        var token = tokenInfo.Token;

        // Special treatment for spelling correction.
        if (token.TokenAttribute.HasFlag(TokenAttribute.SpellingCorrection))
        {
            flags |= CodecConstants.SpellingCorrectionFlag;
        }

        flags |= GetFlagForPos(tokenInfo);
        if (id == 0 && (flags & CodecConstants.PosTypeFlagMask) == CodecConstants.SameAsPrevPosFlag)
        {
            throw new Exception("First Token cannot become SameAsPrevPos");
        }

        flags |= GetFlagForValue(tokenInfo);
        if (id == 0 && (flags & CodecConstants.ValueTypeFlagMask) == CodecConstants.SameAsPrevPosFlag)
        {
            throw new Exception("First Token cannot become SameAsPrevPos");
        }

        if ((flags & CodecConstants.UpperCrammedIDMask) == 0)
        {
            // Lower 6bits are available. Use it for value trie id.
            flags |= CodecConstants.CrammedIDFlag;
        }

        return flags;
    }

    private static byte GetFlagForValue(TokenInfo tokenInfo)
    {
        return tokenInfo.ValueType switch
        {
            ValueType.SameAsPrevValue => CodecConstants.SameAsPrevPosFlag,
            ValueType.AsIsHiragana => CodecConstants.AsIsHiraganaValueFlag,
            ValueType.AsIsKatakana => CodecConstants.AsIsKatakanaValueFlag,
            _ => CodecConstants.NormalValueFlag
        };
    }

    private static byte GetFlagForPos(TokenInfo tokenInfo)
    {
        var lid = tokenInfo.Token.LeftId;
        var rid = tokenInfo.Token.RightId;

        if (lid > CodecConstants.PosMax || rid > CodecConstants.PosMax)
        {
            throw new Exception($"Lid [{lid}] or Rid [{rid}] too large max [{CodecConstants.PosMax}]");
        }

        switch (tokenInfo.PosType)
        {
            case PosType.FrequentPos:
                return CodecConstants.FrequentPosFlag;
            case PosType.SameAsPrevPos:
                return CodecConstants.SameAsPrevPosFlag;
            default:
            {
                if (lid == rid)
                {
                    return CodecConstants.MonoPosFlag;
                }

                break;
            }
        }

        return CodecConstants.FullPosFlag;
    }

    public static string DecodeKey(byte[] key)
    {
        return Encoding.UTF8.GetString(EncodeKey(Encoding.UTF8.GetString(key)));
    }

    // Swap the area for Hiragana, prolonged sound mark and middle dot with
    // the one for control codes and alphabets.
    //
    // U+3041 - U+305F ("ぁ" - "た") <=> U+0001 - U+001F
    // U+3060 - U+3095 ("だ" - "ゕ") <=> U+0040 - U+0075
    // U+30FB - U+30FC ("・" - "ー") <=> U+0076 - U+0077
    //
    // U+0020 - U+003F are left intact to represent numbers and hyphen in 1 byte.
    public static byte[] EncodeKey(string source)
    {
        var result = new List<byte>();

        for (var i = 0; i < source.Length; i++)
        {
            if (char.IsLowSurrogate(source[i]))
            {
                // Already processed previously.
                continue;
            }

            var character = (uint)char.ConvertToUtf32(source, i);

            var offset = character switch
            {
                >= 0x0001 and <= 0x001f or >= 0x3041 and <= 0x305f => 0x3041 - 0x0001,
                >= 0x0040 and <= 0x0075 or >= 0x3060 and <= 0x3095 => 0x3060 - 0x0040,
                >= 0x0076 and <= 0x0077 or >= 0x30FB and <= 0x30FC => 0x30FB - 0x0076,
                _ => 0
            };
            if (character < 0x80)
            {
                character += (uint) offset;
            }
            else
            {
                character -= (uint) offset;
            }
            result.AddRange(CharacterToUtf8(character));
        }

        return result.ToArray();
    }

    private static byte[] CharacterToUtf8(uint character)
    {
        var buffer = new List<byte>();

        switch (character)
        {
            case 0:
                break;
            case < 0x00080:
                buffer.Add((byte)(character & 0xFF));
                break;
            case < 0x00800:
                buffer.Add((byte)(0xC0 + ((character >> 6) & 0x1F)));
                buffer.Add((byte)(0x80 + (character & 0x3F)));
                break;
            case < 0x10000:
                buffer.Add((byte)(0xE0 + ((character >> 12) & 0x0F)));
                buffer.Add((byte)(0x80 + ((character >> 6) & 0x3F)));
                buffer.Add((byte)(0x80 + (character & 0x3F)));
                break;
            case < 0x200000:
                buffer.Add((byte)(0xF0 + ((character >> 18) & 0x07)));
                buffer.Add((byte)(0x80 + ((character >> 12) & 0x3F)));
                buffer.Add((byte)(0x80 + ((character >> 6) & 0x3F)));
                buffer.Add((byte)(0x80 + (character & 0x3F)));
                break;
            case < 0x8000000:
                buffer.Add((byte)(0xF8 + ((character >> 24) & 0x03)));
                buffer.Add((byte)(0x80 + ((character >> 18) & 0x3F)));
                buffer.Add((byte)(0x80 + ((character >> 12) & 0x3F)));
                buffer.Add((byte)(0x80 + ((character >> 6) & 0x3F)));
                buffer.Add((byte)(0x80 + (character & 0x3F)));
                break;
            default:
                buffer.Add((byte)(0xFC + ((character >> 30) & 0x01)));
                buffer.Add((byte)(0x80 + ((character >> 24) & 0x3F)));
                buffer.Add((byte)(0x80 + ((character >> 18) & 0x3F)));
                buffer.Add((byte)(0x80 + ((character >> 12) & 0x3F)));
                buffer.Add((byte)(0x80 + ((character >> 6) & 0x3F)));
                buffer.Add((byte)(0x80 + (character & 0x3F)));
                break;
        }

        return buffer.ToArray();
    }

    // This encodes each UTF32 character into following areas
    // The trickier part in this encoding is handling of \0 byte in UTF32
    // character. To avoid \0 in converted string, this function uses
    // VALUE_CHAR_MARK_* markers.
    //  Kanji in 0x4e00~0x97ff -> 0x01 0x00 ~ 0x4a 0xff (74*256 characters)
    //  Hiragana 0x3041~0x3095 -> 0x4b~0x9f (84 characters)
    //  Katakana 0x30a1~0x30fc -> 0x9f~0xfa (91 characters)
    //  0x?? (ASCII) -> VALUE_CHAR_MARK_ASCII ??
    //  0x??00 -> VALUE_CHAR_MARK_XX00 ??
    //  Other 0x?? ?? -> VALUE_CHAR_MARK_OTHER ?? ??
    //  0x?????? -> VALUE_CHAR_MARK_BIG ?? ?? ??
    public static byte[] EncodeValue(string source)
    {
        var resultArray = new List<byte>();

        for(var i = 0; i < source.Length; i++)
        {
            if (char.IsLowSurrogate(source[i]))
            {
                // Already processed previously.
                continue;
            }

            var character = char.ConvertToUtf32(source, i);

            if (character >= CodecConstants.HiraganaRangeMin && character <= CodecConstants.HiraganaRangeMax)
            {
                // Encode Hiragana character into 1 byte.
                resultArray.Add((byte)(character - CodecConstants.HiraganaRangeMin + CodecConstants.ValueHiraganaOffset));
            } else if (character >= CodecConstants.KatakanaRangeMin && character <= CodecConstants.KatakanaRangeMax)
            {
                // Encode Katakana character into 1 byte.
                resultArray.Add((byte)(character - CodecConstants.KatakanaRangeMin + CodecConstants.ValueKatakanaOffset));
            } else if (character < 0x10000 && ((character >> 8) & 255) == 0)
            {
                // 0x00?? Ascii characters are encoded into 2 bytes.
                resultArray.Add(CodecConstants.ValueAsciiCharMark);
                resultArray.Add((byte)(character & 255));

            } else if (character < 0x10000 && (character & 255) == 0)
            {
                // 0x??00 characters are also encoded into 2 bytes.
                resultArray.Add(CodecConstants.ValueXX00CharMark);
                resultArray.Add((byte)((character >> 8) & 255));
            } else if (character >= CodecConstants.KanjiRangeMin && character <= CodecConstants.KanjiRangeMax)
            {
                // Frequent Kanji and others (74*256 characters) are encoded
                // into 2 bytes.
                // (Kanji in 0x9800 to 0x9fff are encoded in 3 bytes)
                resultArray.Add((byte)(((character - CodecConstants.KanjiRangeMin) >> 8) + CodecConstants.ValueKanjiOffset));
                resultArray.Add((byte)(character & 255));
            } else if (character is >= 0x10000 and <= 0x10ffff)
            {
                // These characters are encoded into 2-4 bytes
                var left = ((character >> 16) & 255);
                var middle = ((character >> 8) & 255);
                var right = (character & 255);
                if (middle == 0)
                {
                    left |= CodecConstants.ValueMiddle0CharMark;
                }
                if (right == 0)
                {
                    left |= CodecConstants.ValueRight0CharMark;
                }
                resultArray.Add(CodecConstants.ValueCharMark);
                resultArray.Add((byte)left);
                if (middle != 0)
                {
                    resultArray.Add((byte)middle);
                }
                if (right != 0)
                {
                    resultArray.Add((byte)right);
                }
            }
            else
            {
                resultArray.Add(CodecConstants.ValueUcs2CharMark);
                resultArray.Add((byte)((character >> 8) & 255));
                resultArray.Add((byte)(character & 255));
            }
        }

        return resultArray.ToArray();
    }

    public static string DecodeValue(byte[] data)
    {
        var result = new List<byte>();

        var offset = 0;
        while (offset < data.Length)
        {
            var cc = (int) data[offset];
            var c = 0;

            if (CodecConstants.ValueHiraganaOffset <= cc && cc < CodecConstants.ValueKatakanaOffset)
            {
                // Hiragana
                c = 0x3041 + data[offset] - CodecConstants.ValueHiraganaOffset;
                offset++;
            } else if (CodecConstants.ValueKatakanaOffset <= cc && cc < CodecConstants.ValueAsciiCharMark)
            {
                // Katakana
                c = 0x30a1 + data[offset] - CodecConstants.ValueKatakanaOffset;
                offset++;
            }else if (cc == CodecConstants.ValueAsciiCharMark)
            {
                // Ascii
                c = data[offset + 1];
                offset += 2;
            }else if (cc == CodecConstants.ValueXX00CharMark)
            {
                // xx00
                c = data[offset + 1] << 8;
                offset += 2;
            }else if (cc == CodecConstants.ValueCharMark)
            {
                // UCS4
                c = ((data[offset + 1] & CodecConstants.ValueLeftMaskCharMark) << 16);
                var pos = 2;
                if ((data[offset] & CodecConstants.ValueMiddle0CharMark) != CodecConstants.ValueMiddle0CharMark)
                {
                    c += (data[offset + pos++] << 8);
                }

                if ((data[offset] & CodecConstants.ValueMiddle0CharMark) != CodecConstants.ValueRight0CharMark)
                {
                    c += data[offset + pos++];
                }

                offset += pos;
            } else if (cc == CodecConstants.ValueUcs2CharMark)
            {
                // other
                c = data[offset + 1] << 8;
                c += data[offset + 2];
                offset += 3;
            } else if (cc < CodecConstants.ValueHiraganaOffset)
            {
                // Frequent kanji
                c = (data[offset] - CodecConstants.ValueKanjiOffset << 8) + 0x4e00;
                c += data[offset + 1];
                offset += 2;
            }
            else
            {
                throw new Exception("Cant decode value");
            }

            result.AddRange(CharacterToUtf8((uint) c));

        }

        return Encoding.UTF8.GetString(result.ToArray());
    }
}
