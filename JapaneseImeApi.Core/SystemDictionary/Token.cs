namespace JapaneseImeApi.Core.SystemDictionary;

public record Token
{
    public string Key {get; set; }
    public string Value {get; set; }
    public int Cost {get; set; }
    public int LeftId {get; set; }
    public int RightId  {get; set; }
    public TokenAttribute TokenAttribute { get; set; } = TokenAttribute.None;

}
