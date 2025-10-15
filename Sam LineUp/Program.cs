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
        static void Main() 
            {                                
            Console.WriteLine("Welcome to Line Up!");

            var printer = new ConsoleGamePrinter();
            Game? game = null;

            bool startNewGame = false;
            bool isTestMode = false;
            int mode = -1;
            int runMode = -1;


            // offer to load a save game
            if (Utils.YesNo("Load saved game?"))
            {
                var path = SavingUtils.PickSavePath();
                if (path != null)
                {
                    // overwrite with loaded values
                    game = SaveGame.LoadFromFile(path);
                    Console.WriteLine($"Loaded game from {path}");

                    startNewGame = false;
                    isTestMode = false;
                }
                else
                {
                    Console.WriteLine("Load cancelled. Starting new game.");
                    startNewGame = true;
                }
            }
            else
            {
                startNewGame = true;
            }

            // if new game, prompt for settings
            if (startNewGame)
            {
                mode = Utils.PromptGameMode();

                int cols, rows;
                if (mode != 2) (cols, rows) = Utils.PromptGameSettings();
                else { cols = 8; rows = 9; }

                runMode = Utils.PromptRunMode();


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
            }

            IGameRunner runner = game.IsTestMode
                ? new TestRunner(game, printer)
                : new ConsoleRunner(game, printer);
            runner.Run();

            Console.WriteLine("Game over. Thanks for playing!");

            if (!isTestMode) Console.ReadLine(); // to pause at the end of normal play

        }
        
    }
}
            