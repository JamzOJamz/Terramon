using System.Globalization;
using CsvHelper;
using Terramon.Core;
using Terramon.DataGen.Models;
using Terramon.ID;

namespace Terramon.DataGen;

internal static class MoveService
{
    private static readonly HttpClient HttpClient = new();

    public static async Task<string> DownloadMovesCsv(string cacheDir)
    {
        const string movesCsvUrl = "https://raw.githubusercontent.com/PokeAPI/pokeapi/refs/heads/master/data/v2/csv/moves.csv";
        var localFilePath = Path.Combine(cacheDir, "moves.csv");

        if (File.Exists(localFilePath))
        {
            Console.WriteLine($"Found cached moves.csv in {localFilePath}\n");
            return localFilePath;
        }

        Console.WriteLine("Downloading moves.csv from PokéAPI GitHub repository...");
        var response = await HttpClient.GetAsync(movesCsvUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync(localFilePath, content);

        Console.WriteLine($"Saved moves.csv to {localFilePath}\n");
        return localFilePath;
    }

    public static Dictionary<ushort, DatabaseV2.MoveSchema> ProcessMovesCsv(string csvPath)
    {
        Console.WriteLine($"Processing moves...");
        
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<MoveCsvMap>();

        var records = csv.GetRecords<MoveCsvModel>().ToList();

        var moves = new Dictionary<ushort, DatabaseV2.MoveSchema>();
        var overage = 0;

        foreach (var record in records)
        {
            if (record.ID > 10000) break;

            var selectiveClass = false;
            if (record.Identifier.Contains("--"))
            {
                overage++;
                if (overage % 2 != 0)
                    continue;
                selectiveClass = true;
            }

            record.ID -= (ushort)(overage / 2);

            moves[record.ID] = new DatabaseV2.MoveSchema
            {
                Type = (PokemonType)record.TypeID,
                Power = record.Power,
                PP = record.PP!.Value,
                Accuracy = record.Accuracy,
                Category = selectiveClass ? MoveCategory.Dynamic : (MoveCategory)record.DamageClassID,
                Effect = record.EffectID,
                EffectChance = record.EffectChance
            };
        }
        
        Console.WriteLine($"Moves processing complete. {moves.Count} moves loaded.\n");

        return moves;
    }
}
