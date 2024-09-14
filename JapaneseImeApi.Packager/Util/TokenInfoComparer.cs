using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JapaneseImeApi.Core.SystemDictionary;
using JapaneseImeApi.Packager.Models;

namespace JapaneseImeApi.Packager.Util;
public class TokenInfoComparer : IComparer<TokenInfo>
{

    public int Compare(TokenInfo x, TokenInfo y)
    {
        if (x.Token.LeftId != y.Token.LeftId)
        {
            return y.Token.LeftId.CompareTo(x.Token.LeftId);
        }
        if (x.Token.RightId != y.Token.RightId)
        {
            return y.Token.RightId.CompareTo(x.Token.RightId);
        }
        if (x.IdInValueTrie != y.IdInValueTrie)
        {
            return x.IdInValueTrie.CompareTo(y.IdInValueTrie);
        }
        return x.Token.TokenAttribute.CompareTo(y.Token.TokenAttribute);
    }

}