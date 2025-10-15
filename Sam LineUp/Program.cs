using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.BoardSpace.ConcreteFactory;
using LineUpV3.Models.GameSpace;
using LineUpV3.Models.PlayerSpace;
using LineUpV3.Models.PlayerSpace.ConcreteFactory;
using LineUpV3.Models.RunnerSpace;
using LineUpV3.Models.SavingSpace;
using LineUpV3.UtilSpace;



namespace LineUpV3
{
    internal class Program
    {

        // controls for menu loop
        private enum MainAction { New, Load, Help, Quit }

        private static MainAction PromptMainAction()
        {
            Console.WriteLine("What would you like to do?");
            Console.WriteLine("\n[N]ew Game,\n[L]oad Game,\n[H]elp,\n[Q]uit\n (default = N):\n");

            while (true)
            {

                var response = Console.ReadLine()?.Trim().ToUpperInvariant();

                if (string.IsNullOrEmpty(response) || response == "N") return MainAction.New;
                if (response == "L") return MainAction.Load;
                if (response == "H") return MainAction.Help;
                if (response == "Q") return MainAction.Quit;

                Console.WriteLine("Invalid Selection. Try Again.");
                Console.Write("[N]ew Game,[L]oad Game,[H]elp,[Q]uit (default = N):");
            }
        }


        static void Main()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Welcome to Line Up!");

                var action = PromptMainAction();

                if (action == MainAction.Quit)
                {
                    return;
                }

                if (action == MainAction.Help)
                {
                    HelpText.ShowHelpInMain(); continue;
                }

                Game? game = null;

                if (action == MainAction.Load)
                {
                    var path = SavingUtils.PickSavePath();
                    if (path == null)
                    {
                        Console.WriteLine("Load Cancelled.");
                        continue;
                    }
                    game = SaveGame.LoadFromFile(path);
                    Console.WriteLine($"Loaded game from {path}");
                }
                else
                {
                    int mode = Utils.PromptGameMode();

                    int cols, rows;
                    if (mode != 2) (cols, rows) = Utils.PromptGameSettings();
                    else { cols = 8; rows = 9; }

                    int runMode = Utils.PromptRunMode();

                    // Factory Method for Game Construction
                    IBoardFactory boardF = (mode == 2) ? new ClassicBoardFactory() : new CustomBoardFactory();
                    IPlayersFactory playersF = runMode switch
                    {
                        1 => new HumanVsHumanFactory(),
                        2 => new HumanVsCpuFactory(),
                        3 => new CpuVsCpuFactory(),
                        4 => new TestHumanVsHumanFactory(),
                        _ => throw new InvalidOperationException("Invalid run mode")
                    };
                    IRotationFactory rotationF = new RotationByModeFactory();

                    // build game via composite
                    IGameFactory gameF = new CompositeGameFactory(boardF, playersF, rotationF);
                    game = gameF.Build(new GameConfig(rows, cols, mode));

                    var printer = new ConsoleGamePrinter();


                    IGameRunner runner = game.IsTestMode
                        ? new TestRunner(game, printer)
                        : new ConsoleRunner(game, printer);
                    runner.Run();

                    Console.WriteLine("Game over. Thanks for playing!");

                    Console.WriteLine("\n(Returning to Main Menu)");
                    Console.ReadLine();

                }
            }
        }
    }
}
