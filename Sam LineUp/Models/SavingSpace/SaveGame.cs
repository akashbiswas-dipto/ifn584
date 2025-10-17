using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.BoardSpace.ConcreteFactory;
using LineUpV3.Models.DiscSpace;
using LineUpV3.Models.GameSpace;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.UtilSpace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LineUpV3.Models.SavingSpace
{
    internal static class SaveGame
    {
        // Save directory is managed via SavingUtils
        public static void SaveToFile(Game game, string fileName)
        {
            string path = SavingUtils.SavePaths.MakePath(fileName);

            var state = game.SaveGameState();
            var b = state.Board;
            var p1 = state.Player1;
            var p2 = state.Player2;

            var sb = new StringBuilder();

            // Game info
            sb.AppendLine("[Game]");
            sb.AppendLine($"CurrentPlayer={state.CurrentPlayerId}");
            sb.AppendLine($"GameMode={state.GameMode}");
            sb.AppendLine($"TurnNumber={state.TurnNumber}");
            sb.AppendLine($"IsTestMode={game.IsTestMode}");
            sb.AppendLine();

            // Board info
            sb.AppendLine("[Board]");
            sb.AppendLine($"Rows={b.Rows}");
            sb.AppendLine($"Cols={b.Cols}");
            sb.AppendLine($"LastMove={b.LastMove}");
            sb.AppendLine($"Cells={b.Cells}");
            sb.AppendLine();

            // Players
            WritePlayer(sb, "Player1", p1);
            WritePlayer(sb, "Player2", p2);

            File.WriteAllText(path, sb.ToString());
        }

        public static Game LoadFromFile(string path)
        {
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

            // Players
            var p1State = ReadPlayer(sections["Player1"]);
            var p2State = ReadPlayer(sections["Player2"]);

            // Rebuild concrete objects
            IBoard board = new Board(rows, cols);
            IPlayer player1 = p1State.Type == PlayerType.Human
                ? new HumanPlayer(p1State.Id, p1State.Name)
                : new ComputerPlayer(p1State.Id, p1State.Name);

            IPlayer player2 = p2State.Type == PlayerType.Human
                ? new HumanPlayer(p2State.Id, p2State.Name)
                : new ComputerPlayer(p2State.Id, p2State.Name);

            player1.LoadPlayer(p1State);
            player2.LoadPlayer(p2State);

            IRotation? rotation = new RotationByModeFactory().Create(gameMode);

            var game = Game.Create(board, player1, player2, gameMode, rotation, isTestMode);

            var gameState = new GameState(boardState, p1State, p2State, currentPlayerId, gameMode, turnNumber);
            game.LoadGameState(gameState);
            game.InitializeHistoryFromFileLoad(gameState);

            return game;
        }

        private static void WritePlayer(StringBuilder sb, string sectionName, PlayerState p)
        {
            sb.AppendLine($"[{sectionName}]");
            sb.AppendLine($"Id={p.Id}");
            sb.AppendLine($"Type={p.Type}");
            sb.AppendLine($"Name={p.Name}");
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

            var counts = new Dictionary<DiscType, int>
            {
                [DiscType.Ordinary] = int.Parse(sec.GetValueOrDefault("Ordinary", "0")),
                [DiscType.Boring] = int.Parse(sec.GetValueOrDefault("Boring", "0")),
                [DiscType.Exploding] = int.Parse(sec.GetValueOrDefault("Exploding", "0")),
                [DiscType.Magnetic] = int.Parse(sec.GetValueOrDefault("Magnetic", "0"))
            };

            return new PlayerState(id, type, counts.Values.Sum(), counts, name);
        }

        private static Dictionary<string, Dictionary<string, string>> ParseSections(string[] lines)
        {
            var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string>? cur = null;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var name = line.Substring(1, line.Length - 2);
                    cur = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[name] = cur;
                }
                else if (cur != null)
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        var k = line.Substring(0, eq).Trim();
                        var v = line.Substring(eq + 1).Trim();
                        cur[k] = v;
                    }
                }
            }
            return dict;
        }
    }
}
