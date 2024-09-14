using JapaneseImeApi.Core.Constants;
using JapaneseImeApi.Core.SystemDictionary.Structures;
using JapaneseImeApi.Packager.SystemDictionary;
using Microsoft.Extensions.Logging;

namespace JapaneseImeApi.Packager
{
    internal static class Program
    {

        static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });

            //var testTrie = new LoudsTrieBuilder();
            //testTrie.Add("a"u8.ToArray());
            //testTrie.Add("aa"u8.ToArray());
            //testTrie.Add("aaa"u8.ToArray());
            //testTrie.Add("ab"u8.ToArray());
            //testTrie.Build();

            //var aId = testTrie.GetId("ab"u8.ToArray());

            //var readTrie = new LoudsTrie(testTrie.Stream.ToArray(),
            //    TrieCacheSizeConstants.ValueTrieLb0CacheSize,
            //    TrieCacheSizeConstants.ValueTrieLb1CacheSize,
            //    TrieCacheSizeConstants.ValueTrieSelect0CacheSize,
            //    TrieCacheSizeConstants.ValueTrieSelect1CacheSize,
            //    TrieCacheSizeConstants.ValueTrieTermvecCacheSize);

            //var results = readTrie.FindKeyIdsOfAllPrefixes("a"u8.ToArray());
            //var aResult = readTrie.RestoreKeyString(aId);

            var builder = new SystemDictionaryBuilder(loggerFactory.CreateLogger(nameof(SystemDictionaryBuilder)));

            var dictionaryFiles = Enumerable.Range(0, 10)
                .Select(i => $"Data/dictionary{i:00}.txt")
                .ToList();

            Directory.CreateDirectory("result");

            builder.Load(dictionaryFiles).Build().WriteTo("result/data");

            Console.WriteLine("Done");

        }

    }
}
