using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Com.Skylanders.Kiosk
{
    public class SkylanderCollectionTracker
    {
        public class SkylanderData
        {
            [JsonPropertyName("characterId")]
            public int CharacterId { get; set; }

            [JsonPropertyName("variantId")]
            public int VariantId { get; set; }

            [JsonPropertyName("xp")]
            public int Xp { get; set; }

            [JsonPropertyName("gold")]
            public int Gold { get; set; }

            [JsonPropertyName("hatId")]
            public int HatId { get; set; }

            [JsonPropertyName("decoId")]
            public int decoId { get; set; }

            [JsonPropertyName("cumulativeTime")]
            public int cumulativeTime { get; set; }

            [JsonPropertyName("areaSequence")]
            public byte areaSequence { get; set; }

            [JsonPropertyName("platform")]
            public int platform { get; set; }

            [JsonPropertyName("nickname")]
            public string nickname { get; set; }

            [JsonPropertyName("LastUsedDate")]
            public string LastUsedDate { get; set; }
        }
        public class GameEntry
        {
            [JsonPropertyName("year")]
            public int Year { get; set; }

            [JsonPropertyName("normalFigures")]
            public List<SkylanderData> NormalFigures { get; set; } = new();

            [JsonPropertyName("variants")]
            public List<SkylanderData> Variants { get; set; } = new();
        }
        public class SkylandersCollectionRoot
        {
            [JsonPropertyName("games")]
            public List<GameEntry> Games { get; set; } = new();
        }



        public static async Task SaveFigureData( int targetYear, int decoId, SkylanderData readSkylander)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            SkylandersCollectionRoot root;
            using (FileStream filestream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OwnedSkylanders.json"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                if (filestream.Length > 0)
                {
                    root = await JsonSerializer.DeserializeAsync<SkylandersCollectionRoot>(filestream) ?? new SkylandersCollectionRoot();
                }
                else
                {
                    root = new SkylandersCollectionRoot();
                }
                var targetGame = root.Games.FirstOrDefault(g => g.Year == targetYear);
                if (decoId != 0 && decoId != 1 && decoId != 9 && decoId != 10 && decoId != 11)
                {
                    bool alreadyExists = targetGame.Variants.Any(skylander =>
                    skylander.CharacterId == readSkylander.CharacterId &&
                    skylander.VariantId == readSkylander.VariantId);
                    if (alreadyExists == false)
                    {
                        targetGame.Variants.Add(readSkylander);
                    }
                }
                else
                {
                    bool alreadyExists = targetGame.NormalFigures.Any(skylander =>
                    skylander.CharacterId == readSkylander.CharacterId);
                    if (alreadyExists == false)
                    {
                        targetGame.NormalFigures.Add(readSkylander);
                    }
                }
                filestream.SetLength(0);
                await JsonSerializer.SerializeAsync(filestream, root, options);
            }
        }
    }
}
