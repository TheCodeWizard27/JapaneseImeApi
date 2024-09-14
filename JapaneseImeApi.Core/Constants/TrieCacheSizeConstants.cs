using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseImeApi.Core.Constants;

public static class TrieCacheSizeConstants
{

    public const int KeyTrieLb0CacheSize = 1 * 1024;
    public const int KeyTrieLb1CacheSize = 1 * 1024;
    public const int KeyTrieSelect0CacheSize = 4 * 1024;
    public const int KeyTrieSelect1CacheSize = 4 * 1024;
    public const int KeyTrieTermvecCacheSize = 1 * 1024;

    public const int ValueTrieLb0CacheSize = 1 * 1024;
    public const int ValueTrieLb1CacheSize = 1 * 1024;
    public const int ValueTrieSelect0CacheSize = 1 * 1024;
    public const int ValueTrieSelect1CacheSize = 16 * 1024;
    public const int ValueTrieTermvecCacheSize = 4 * 1024;

}
