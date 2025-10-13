using LineUpV3.Models.BoardSpace;
using LineUpV3.Models.GameSpace;
using LineUpV3.Models.PlayerSpace;
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

            var game = new Game();
            var printer = new ConsoleGamePrinter();

            bool startNewGame = false;
            bool isTestMode = false;
            int mode = -1;
            int runMode = -1;

            int width = -1;
            int height = -1;


            // offer to load a save game
            if (Utils.YesNo("Load saved game?"))
            {
                var path = SavingUtils.PickSavePath();
                if (path != null)
                {
                    // overwrite with loaded values
                    SaveGame.LoadFromFile(game, path);
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

                if (mode != 2)
                {
                    (width, height) = Utils.PromptGameSettings();
                }
                else //Overwrite with classic presets
                {
                    width = 8;
                    height = 9;
                }

                runMode = Utils.PromptRunMode();

                IPlayer player1, player2;
                switch (runMode)
                {
                    case 1:
                        player1 = new HumanPlayer(PlayerId.Player1, "P1");
                        player2 = new HumanPlayer(PlayerId.Player2, "P2");
                        break;
                    case 2:
                        player1 = new HumanPlayer(PlayerId.Player1, "P1");
                        player2 = new ComputerPlayer(PlayerId.Player2, "CPU");
                        break;
                    case 3:
                        player1 = new ComputerPlayer(PlayerId.Player1, "CPU1");
                        player2 = new ComputerPlayer(PlayerId.Player2, "CPU2");
                        break;
                    case 4:
                        player1 = new HumanPlayer(PlayerId.Player1, "Test1");
                        player2 = new HumanPlayer(PlayerId.Player2, "Test2");
                        isTestMode = true;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid game mode");
                }

                var board = new Board(rows: height, cols: width);
                game.SetUp(board, player1, player2, mode);
            }

            IGameRunner runner = (isTestMode)
                ? new TestRunner(game, printer)   // scripted input for test mode
                : new ConsoleRunner(game, printer); // normal play for all other modes
            runner.Run();
            
            Console.WriteLine("Game over. Thanks for playing!");

            if (!isTestMode) Console.ReadLine(); // to pause at the end of normal play
            
            
        }
    }
}
            