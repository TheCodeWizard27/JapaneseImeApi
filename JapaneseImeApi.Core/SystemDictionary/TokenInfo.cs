namespace JapaneseImeApi.Core.SystemDictionary;

public record TokenInfo(Token Token)
{

    public int IdInValueTrie { get; set; }
    public int IdInFrequentPosMap { get; set; }
    public PosType PosType { get; set; }
    public ValueType ValueType { get; set; }
    public CostType CostType { get; set; }
    public AccentEncodingType AccentEncodingType { get; set; }
    public int AccentType { get; set; }

}

public enum PosType
{
    DefaultPos = 0,
    FrequentPos = 1,
    SameAsPrevPos = 2,
    PosTypeSize = 3,
};
public enum ValueType
{
    DefaultValue = 0,
    // value is same as prev Token's value
    SameAsPrevValue = 1,
    // value is same as key
    AsIsHiragana = 2,
    // we can get the value by converting key to katakana form.
    AsIsKatakana = 3,
    ValueTypeSize = 4,
};
public enum CostType
{
    DefaultCost = 0,
    CanUseSmallEncoding = 1,
    CostTypeSize = 2,
};
public enum AccentEncodingType
{
    EncodedInValue = 0,
    EmbeddedInToken = 1,
    AccentEncodingTypeSize = 2,
};