using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JapaneseImeApi.Core.SystemDictionary;

namespace JapaneseImeApi.Packager.Models;

public record KeyInfo(string Key)
{
    public int IdInKeyTrie { get; set; } = -1;
    public List<TokenInfo> TokenInfos { get; init; } = new();
};
