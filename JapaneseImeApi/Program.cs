
using System.Reflection;
using System.Text;
using JapaneseImeApi.Core;
using JapaneseImeApi.Core.Constants;
using JapaneseImeApi.Core.SystemDictionary;
using JapaneseImeApi.Core.Util;

namespace JapaneseImeApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            LoadDataManager();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static void LoadDataManager()
        {
            var assembly = Assembly.Load("JapaneseImeApi.Core");
            var resourceName = "JapaneseImeApi.Core.Data.data.dict";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            var reader = new ByteStreamReader(stream);

            var magic = reader.ReadNextInt();

            if (magic != CodecConstants.FileMagic) throw new Exception("Invalid file");

            var seed = reader.ReadNextInt();

            if (seed != CodecConstants.Seed) throw new Exception("Invalid file");

            var sections = new List<DictionaryFileSection>();

            var continueReading = true;
            while (continueReading)
            {
                var size = (long) reader.ReadNextInt();
                var sectionLength = Math.Ceiling(size / 4d) * 4; // Pad to 4 bytes.
                var name = Encoding.UTF32.GetString(BitConverter.GetBytes(reader.ReadNextInt())); // Assume that its just one character.
                var data = reader.Read((int) sectionLength);

                sections.Add(new DictionaryFileSection(name, data));

                if (reader.PeekNextInt() == 0)
                {
                    continueReading = false;
                }
            }

            var dataManager = new DataManager(new DictionaryFileSections
            {
                ValueTrieSection = sections.Single(x => x.Name == DictionarySectionConstants.ValueSectionName),
                KeyTrieSection = sections.Single(x => x.Name == DictionarySectionConstants.KeySectionName),
                TokenArraySection = sections.Single(x => x.Name == DictionarySectionConstants.TokensSectionName),
                FrequentPosSection = sections.Single(x => x.Name == DictionarySectionConstants.PosSectionName)
            });

        }

    }
}
