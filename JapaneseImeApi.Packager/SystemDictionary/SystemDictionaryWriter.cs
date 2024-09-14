using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JapaneseImeApi.Core.Constants;
using JapaneseImeApi.Core.SystemDictionary;
using JapaneseImeApi.Packager.Models;
using Microsoft.Extensions.Logging;

namespace JapaneseImeApi.Packager.SystemDictionary;

public class SystemDictionaryWriter(ILogger logger)
{

    public void WriteSections(string basePath, DictionaryFileSections sections)
    {
        logger.LogDebug($"{nameof(SystemDictionaryWriter)}: Writing debug files to [{basePath}].");

        File.WriteAllBytes(Path.ChangeExtension(basePath, ".value"), sections.ValueTrieSection.Data);
        File.WriteAllBytes(Path.ChangeExtension(basePath, ".key"), sections.KeyTrieSection.Data);
        File.WriteAllBytes(Path.ChangeExtension(basePath, ".tokens"), sections.TokenArraySection.Data);
        File.WriteAllBytes(Path.ChangeExtension(basePath, ".freq_pos"), sections.FrequentPosSection.Data);

        logger.LogDebug($"{nameof(SystemDictionaryWriter)}: Writing complete dictionary under [{basePath}].");

        using var stream = File.Create(Path.ChangeExtension(basePath, ".dict"));

        // Header
        stream.Write(BitConverter.GetBytes(CodecConstants.FileMagic));
        stream.Write(BitConverter.GetBytes(CodecConstants.Seed));

        WriteSection(stream, sections.ValueTrieSection);
        WriteSection(stream, sections.TokenArraySection);
        WriteSection(stream, sections.KeyTrieSection);
        WriteSection(stream, sections.FrequentPosSection);

        stream.Write(BitConverter.GetBytes(0));
    }

    private static void WriteSection(Stream stream, DictionaryFileSection section)
    {
        stream.Write(BitConverter.GetBytes(section.Data.Length));
        stream.Write(Encoding.UTF32.GetBytes(section.Name));
        stream.Write(section.Data);

        // A bit of a hacky padding to 4 bytes.
        for (var i = section.Data.Length; (i % 4) != 0; ++i)
        {
            stream.Write(new byte[] { 0x00 });
        }

    }

}
