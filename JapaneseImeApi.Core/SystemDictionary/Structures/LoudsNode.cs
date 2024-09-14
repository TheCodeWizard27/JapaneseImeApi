using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseImeApi.Core.SystemDictionary.Structures;

public record LoudsNode
{

    public int NodeId { get; set; } = 1;
    public int EdgeId { get; set; }

}
