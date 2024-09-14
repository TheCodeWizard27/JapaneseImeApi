namespace JapaneseImeApi.Core.SystemDictionary;

[Flags]
public enum TokenAttribute
{
    None = 0,
    SpellingCorrection = 1,
    LabelSize = 2,
    // * CAUTION *
    // If you are going to add new attributes, make sure that they have larger
    // values than LABEL_SIZE!! The attributes having less values than it are
    // tightly integrated with the system dictionary codec.

    // The following attribute is not stored in the system dictionary but is
    // added by dictionary modules when looking up from user dictionary.
    SuffixDictionary = 1 << 6,
    UserDictionary = 1 << 7,
}
