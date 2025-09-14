using System.Text.Json;
using Battleships_Pantoja_Saavedra.Models;

public static class GameStore
{
    private static readonly object _lock = new object();
    private static readonly string _dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
    private static readonly string _filePath = Path.Combine(_dataFolder, "games.json");

    private static JsonSerializerOptions JsonOptions => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static List<PlayerState> LoadAll()
    {
        lock (_lock)
        {
            if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
            if (!File.Exists(_filePath)) return new List<PlayerState>();

            var raw = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(raw)) return new List<PlayerState>();

            try
            {
                // Intento normal (PlayerState[])
                var states = JsonSerializer.Deserialize<List<PlayerState>>(raw, JsonOptions);
                if (states != null && states.Any())
                {
                    // Dedupe por nombre por si hay duplicados
                    var deduped = states
                        .GroupBy(s => (s.Player?.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.Last()) // keep last occurrence
                        .Where(s => !string.IsNullOrWhiteSpace(s.Player?.Name))
                        .ToList();

                    if (deduped.Count != states.Count)
                        SaveAll(deduped);

                    return deduped;
                }
            }
            catch
            {
            }

            try
            {
                var legacy = JsonSerializer.Deserialize<List<Player>>(raw, JsonOptions);
                if (legacy != null && legacy.Any())
                {
                    var converted = legacy
                        .GroupBy(p => p.Name?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                        .Select(g => new PlayerState { Player = g.Last(), Board = null })
                        .Where(s => !string.IsNullOrWhiteSpace(s.Player?.Name))
                        .ToList();

                    SaveAll(converted);
                    return converted;
                }
            }
            catch
            {
            }

            return new List<PlayerState>();
        }
    }

    private static void SaveAll(List<PlayerState> list)
    {
        lock (_lock)
        {
            if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
            var raw = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(_filePath, raw);
        }
    }

    public static PlayerState? Get(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return null;
        var list = LoadAll();
        return list.FirstOrDefault(p => p.Player != null &&
                                       p.Player.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
    }

    public static void Upsert(PlayerState state)
    {
        if (state == null || state.Player == null || string.IsNullOrWhiteSpace(state.Player.Name))
            return;

        var list = LoadAll();
        var existing = list.FirstOrDefault(p => p.Player != null &&
                                                p.Player.Name.Equals(state.Player.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Player = state.Player;
            existing.Board = state.Board;
        }
        else
        {
            list.Add(state);
        }


        var deduped = list
            .GroupBy(s => s.Player.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        SaveAll(deduped);
    }

    public static void Remove(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        var list = LoadAll();
        var existing = list.FirstOrDefault(p => p.Player != null &&
                                                p.Player.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            list.Remove(existing);
            SaveAll(list);
        }
    }
}
