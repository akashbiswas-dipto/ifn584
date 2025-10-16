using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.DiscSpace;
using LineUpV3.Models.GameSpace;
using LineUpV3.Models.BoardSpace.ConcreteFactory;

namespace LineUpV3.Models.SavingSpace
{
    internal sealed record SaveFileDto(GameState State, bool IsTestMode);
    internal static class SaveGame
    {

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


            // output it all to a file as 'save'
            File.WriteAllText(path, sb.ToString());
        }

        public static Game LoadFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            var sections = ParseSections(lines);

            // Game section
            var gsec = sections["Game"];
            var currentPlayerId = Enum.Parse<PlayerId>(gsec["CurrentPlayer"]);
            var gameMode = int.Parse(gsec["GameMode"]);
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
            var p1 = ReadPlayer(sections["Player1"]);
            var p2 = ReadPlayer(sections["Player2"]);

            //rebuild concrete objects
            IBoard board = new Board(rows, cols);
            board.LoadBoard(boardState);

            IPlayer player1 = p1.Type == PlayerType.Human
                ? new HumanPlayer(p1.Id, p1.Name)
                : new ComputerPlayer(p1.Id, p1.Name);

            IPlayer player2 = p2.Type == PlayerType.Human
                ? new HumanPlayer(p2.Id, p2.Name)
                : new ComputerPlayer(p2.Id, p2.Name);

            IRotation? rotation = new RotationByModeFactory().Create(gameMode);

            // hand remade game back
            var game = Game.Create(board, player1, player2, gameMode, rotation, isTestMode);
            game.LoadGameState(new GameState(boardState, p1, p2, currentPlayerId, gameMode));

            return game;
        }

        // ========== Helpers ==========

        private static void WritePlayer(StringBuilder sb, string sectionName, PlayerState p)
        {
            sb.AppendLine($"[{sectionName}]");
            sb.AppendLine($"Id={p.Id}");
            sb.AppendLine($"Type={p.Type}");
            sb.AppendLine($"Name={p.Name}");
            // Bag counts (Ordinary, Boring, Exploding, and now Magnetic) are already in PlayerState
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

            // making a dictionary out of what was saved, to aid in pulling the right bits to reload
            var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string>? cur = null;

            foreach (var raw in lines)
            {
                // Safety cleanup
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                if (line.StartsWith("[") && line.EndsWith("]")) // pull out the sections as the new sub dictionary key
                {
                    var name = line.Substring(1, line.Length - 2);
                    cur = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict[name] = cur;
                }
                else if (cur != null) // once we have a section header
                {
                    int eq = line.IndexOf('='); // find the =. Left is key, right is value
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
