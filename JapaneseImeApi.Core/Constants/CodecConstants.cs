using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseImeApi.Core.Constants;

public static class CodecConstants
{

    // Seed value for name string fingerprint
    // Made it mutable for reading sections.
    public const int Seed = 2135654146;
    // Magic value for simple file validation
    public const int FileMagic = 20110701;

    // Validation constants
    // 12 bits
    public const int PosMax = 0x0fff;
    // 15 bits
    public const int CostMax = 0x7fff;
    // 22 bits
    public const int ValueTrieIdMax = 0x3fffff;

    // Flags
    public const byte LastTokenFlag = 0x80;
    public const byte SpellingCorrectionFlag = 0x10;
    // There are 4 mutually exclusive cases
    //  1) Same pos with previous Token
    //  2) Not same, frequent 1 byte pos
    //  3) Not same, full_pos but lid==rid, 2 byte
    //  4) Not same, full_pos 4 byte (no flag for this)
    public const byte PosTypeFlagMask = 0x0c;
    // Pos(left/right ID) is coded into 3 bytes
    // Note that lid/rid is less than 12 bits
    // We need 24 bits (= 3 bytes) to store full pos.
    public const byte FullPosFlag = 0x04;
    // lid == rid 8 bits
    public const byte MonoPosFlag = 0x08;
    // has same left/right id as previous Token
    public const byte SameAsPrevPosFlag = 0x0c;
    // frequent
    public const byte FrequentPosFlag = 0x00;
    //// Flags for Token ////
    public const byte TokenTerminationFlag = 0xff;
    // Note that the flag for the first Token for a certain key cannot be 0xff.
    // First Token cannot be kSameAsPrevValueFlag(0x33) nor kSameAsPrevPosFlag(0x0c)

    // 7 kLastTokenFlag
    // 6  <id encoding>
    // below bits will be used for upper 6 bits of Token value
    // when CRAM_VALUE_FLAG is set.
    // 5    <reserved(unused)>
    // 4     kSpellingCorrectionFlag
    // 3      <pos encoding(high)>
    // 2       <pos encoding(low)>
    // 1        <value encoding(high)>
    // 0         <value encoding(low)>
    //// Value encoding flag ////
    // There are 4 mutually exclusive cases
    //  1) Same as index hiragana key
    //  2) Value is katakana
    //  3) Same as previous Token
    //  4) Others. We have to store the value
    public const byte ValueTypeFlagMask = 0x03;
    // Same as index hiragana word
    public const byte AsIsHiraganaValueFlag = 0x01;
    // Same as index katakana word
    public const byte AsIsKatakanaValueFlag = 0x2;
    // has same word
    public const byte SameAsPrevValueFlag = 0x03;
    // other cases
    public const byte NormalValueFlag = 0x00;
    //// Id encoding flag ////
    // According to lower 6 bits of flags there are 2 patterns.
    //  1) lower 6 bits are used.
    //   - Store an id in a trie use 3 bytes
    //  2) lower 6 bits are not used.
    //   - Set CRAM_VALUE_FLAGS and use lower 6 bits.
    //     We need another 2 bytes to store the id in the trie.
    //     Note that we are assuming each id in the trie is less than 22 bits.
    // Lower 6 bits of flags field are used to store upper part of id
    // in value trie.
    public const byte CrammedIDFlag = 0x40;
    // Mask to cover upper valid 2bits when kCrammedIDFlag is used
    public const byte UpperFlagsMask = 0xc0;
    // Mask to get upper 6bits from flags value
    public const byte UpperCrammedIDMask = 0x3f;
    //// Cost encoding flag ////
    public const byte SmallCostFlag = 0x80;
    public const byte SmallCostMask = 0x7f;


    // Marks
    public const byte ValueUcs2CharMark = 0xfe;
    public const byte ValueCharMark = 0xff;
    public const byte ValueAsciiCharMark = 0xfc;
    public const byte ValueXX00CharMark = 0xfd;
    public const byte ValueMiddle0CharMark = 0x80;
    public const byte ValueRight0CharMark = 0x40;
    public const byte ValueLeftMaskCharMark = 0x1f;

    public const int ValueHiraganaOffset = 0x4b;
    public const int HiraganaRangeMin = 0x3041;
    public const int HiraganaRangeMax = 0x3095;

    public const int ValueKatakanaOffset = 0x9f;
    public const int KatakanaRangeMin = 0x30a1;
    public const int KatakanaRangeMax = 0x30fd;

    public const int ValueKanjiOffset = 0x01;
    public const int KanjiRangeMin = 0x4e00;
    public const int KanjiRangeMax = 0x9800;
}
