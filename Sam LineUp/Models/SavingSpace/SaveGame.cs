using System;
using System.IO;
using System.Text;
using System.Text.Json;
using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.BoardSpace.ConcreteFactory;
using LineUpV3.Models.DiscSpace;
using LineUpV3.Models.GameSpace;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.PlayerSpace.ConcreteFactory;
using LineUpV3.UtilSpace;

namespace LineUpV3.Models.SavingSpace
{
    internal static class SaveGame
    {
        // turns states into strings, which can be easily read
        public static void SaveToFile(Game game, string path)
        {
            var state = game.SaveGameState();
            var history = game.History;
            var b = state.Board;
            var p1 = state.Player1;
            var p2 = state.Player2;

            var sb = new StringBuilder();

            // Game info
            sb.AppendLine("[Game]");
            sb.AppendLine($"CurrentPlayer={state.CurrentPlayerId}");
            sb.AppendLine($"GameMode={state.GameMode}");
            sb.AppendLine($"IsTestMode={game.IsTestMode}");
            sb.AppendLine();

            // Board info
            sb.AppendLine("[Board]");
            sb.AppendLine($"Rows={b.Rows}");
            sb.AppendLine($"Cols={b.Cols}");
            sb.AppendLine($"LastMove={b.LastMove}");
            sb.AppendLine($"Cells={b.Cells}");
            sb.AppendLine();

            //Players
            WritePlayer(sb, "Player1", p1);
            WritePlayer(sb, "Player2", p2);

            //Undo History
            sb.AppendLine("[History]");
            // each step on a new line.
            sb.AppendLine($"Count={history.Length}");
            for (int i = 0; i < history.Length; i++)
            {
                string stateJson = JsonSerializer.Serialize(history[i]);
                sb.AppendLine($"State{i}={stateJson}");
            }
            sb.AppendLine();

            // output it all to a file as 'save'
            File.WriteAllText(path, sb.ToString());
        }

        public static Game LoadFromFile(string path)
        {
            /// Three step process. Read in the state of the original from the file
            /// Then create a new object of those dimensions, and overrite with the originals state

            if (!File.Exists(path))
                throw new FileNotFoundException($"Save file not found: {path}");

            var lines = File.ReadAllLines(path);
            var sections = ParseSections(lines);

            // Game section
            var gsec = sections["Game"];
            var currentPlayerId = Enum.Parse<PlayerId>(gsec["CurrentPlayer"]);
            var gameMode = int.Parse(gsec["GameMode"]);
            var turnNumber = int.Parse(gsec.GetValueOrDefault("TurnNumber", "0"));
            var isTestMode = bool.TryParse(gsec.GetValueOrDefault("IsTestMode", "false"), out var f) && f;

            // Board section
            var bsec = sections["Board"];
            int rows = int.Parse(bsec["Rows"]);
            int cols = int.Parse(bsec["Cols"]);
            string cells = bsec["Cells"];

            (int row, int col)? last = null;
            var lastRaw = bsec.GetValueOrDefault("LastMove", "");
            if (!string.IsNullOrWhiteSpace(lastRaw))
            {
                var s = lastRaw.Trim('(', ')');
                var parts = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2) last = (int.Parse(parts[0]), int.Parse(parts[1]));
            }

            var boardState = new BoardState(rows, cols, cells, last);

            // Players section
            var p1State = ReadPlayer(sections["Player1"]);
            var p2State = ReadPlayer(sections["Player2"]);

            // History reload
            var history = ReadHistory(sections["History"]);

            /// Rebuild Concrete objects in the shape of the loaded
            /// Override with the loaded's state
            /// Now using the same factories as creation to make the concrete object
            IBoardFactory boardF = (gameMode == 2) ? new ClassicBoardFactory() : new CustomBoardFactory();

            var runMode = 1;
            if (p1State.Type == PlayerType.Human && p2State.Type == PlayerType.Computer)
                runMode = 2;

            IPlayersFactory playersF = runMode switch
            {
                1 => new HumanVsHumanFactory(),
                2 => new HumanVsCpuFactory(),
                _ => throw new InvalidOperationException("Invalid run mode")
            };

            IRotationFactory rotationF = new RotationByModeFactory();

            IGameFactory gameF = new CompositeGameFactory(boardF, playersF, rotationF);
            Game game = gameF.Build(new GameConfig(rows, cols, gameMode));

            var gameState = new GameState(boardState, p1State, p2State, currentPlayerId, gameMode, turnNumber);
            game.LoadGameState(gameState);
            game.LoadHistory(history);

            return game;
        }

        private static void WritePlayer(StringBuilder sb, string sectionName, PlayerState p)
        {
            sb.AppendLine($"[{sectionName}]");
            sb.AppendLine($"Id={p.Id}");
            sb.AppendLine($"Type={p.Type}");
            sb.AppendLine($"Name={p.Name}");

            // Save out current inventory
            int o = p.AllCounts.TryGetValue(DiscType.Ordinary, out var _o) ? _o : 0;
            int b = p.AllCounts.TryGetValue(DiscType.Boring, out var _b) ? _b : 0;
            int e = p.AllCounts.TryGetValue(DiscType.Exploding, out var _e) ? _e : 0;
            int m = p.AllCounts.TryGetValue(DiscType.Magnetic, out var _m) ? _m : 0;
            sb.AppendLine($"Ordinary={o}");
            sb.AppendLine($"Boring={b}");
            sb.AppendLine($"Exploding={e}");
            sb.AppendLine($"Magnetic={m}");
            sb.AppendLine();
        }

        private static PlayerState ReadPlayer(Dictionary<string, string> sec)
        {
            var id = Enum.Parse<PlayerId>(sec["Id"]);
            var type = Enum.Parse<PlayerType>(sec["Type"]);
            var name = sec["Name"];

            // read in inventory
            var counts = new Dictionary<DiscType, int>
            {
                [DiscType.Ordinary] = int.Parse(sec.GetValueOrDefault("Ordinary", "0")),
                [DiscType.Boring] = int.Parse(sec.GetValueOrDefault("Boring", "0")),
                [DiscType.Exploding] = int.Parse(sec.GetValueOrDefault("Exploding", "0")),
                [DiscType.Magnetic] = int.Parse(sec.GetValueOrDefault("Magnetic", "0"))
            };

            return new PlayerState(id, type, counts.Values.Sum(), counts, name);
        }

        private static GameState[] ReadHistory(Dictionary<string, string> sec)
        {
            if (!sec.TryGetValue("Count", out var countStr) || !int.TryParse(countStr, out int count))
                return Array.Empty<GameState>();

            var history = new GameState[count];
            for (int i = 0; i < count; i++)
            {
                if (sec.TryGetValue($"State{i}", out var stateJson))
                {
                    history[i] = JsonSerializer.Deserialize<GameState>(stateJson)!;
                }
            }
            return (history);
        }

        private static Dictionary<string, Dictionary<string, string>> ParseSections(string[] lines)
        {
            var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string>? cur = null;

            foreach (var raw in lines)
            {
                // Safety cleanup
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                // looking for the section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var name = line.Substring(1, line.Length - 2);
                    cur = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[name] = cur;
                }
                // once we have a section header, we can start reading in key value pairs
                else if (cur != null)
                {
                    int eq = line.IndexOf('='); // find the =. Left is key, right is value
                    if (eq > 0)
                    {
                        var key = line.Substring(0, eq).Trim();
                        var value = line.Substring(eq + 1).Trim();
                        cur[key] = value;
                    }
                }
            }
            return dict;
        }
    }
}
