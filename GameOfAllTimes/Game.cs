using RLNET;
using GameOfAllTimes.Core;
using GameOfAllTimes.Systems;
using System;
using RogueSharp.Random;
using System.Linq;
using System.Collections.Generic;

namespace GameOfAllTimes
{
    class Game
    {
        //Main Console
        private static readonly int _screenWidth = 100;
        private static readonly int _screenHeight = 70;
        private static RLRootConsole _rootConsole;

        //Map console
        private static readonly int _mapWidth = 80;
        private static readonly int _mapHeight = 48;
        private static RLConsole _mapConsole;

        //Message console
        private static readonly int _messageWidth = 80;
        private static readonly int _messageHeight = 11;
        private static RLConsole _messageConsole;

        //Stats console
        private static readonly int _statWidth = 20;
        private static readonly int _statHeight = 70;
        private static RLConsole _statConsole;

        //Inventory console
        private static readonly int _inventoryWidth = 80;
        private static readonly int _inventoryHeight = 11;
        private static RLConsole _inventoryConsole;

        private static bool _renderRequired = true;

        private static int _mapLevel = 1;

        public static Player Player { get; set; }
        public static DungeonMap DungeonMap { get; private set; }
        public static CommandSystem CommandSystem { get; private set; }
        public static IRandom Random { get; private set; }
        public static MessageLog MessageLog { get; private set; }
        public static SchedulingSystem SchedulingSystem { get; private set; }

        public static LinkedList<SchedulingSystem> SchedulingSystems;
        public static LinkedListNode<SchedulingSystem> SchedSystem;

        public static LinkedList<DungeonMap> Levels;
        public static LinkedListNode<DungeonMap> that;

        public static PerlinNoise noise;


        static void Main()
        {   
            string FontFileName = "terminal8x8.png";
            string ConsoleTitile = "Tutorial";
            int Seed = (int)DateTime.UtcNow.Ticks;
            Random = new DotNetRandom(Seed);

            Levels = new LinkedList<DungeonMap>();
            SchedulingSystems = new LinkedList<SchedulingSystem>();

            MessageLog = new MessageLog();
            MessageLog.Add("The rogue arrives on level 1");
            MessageLog.Add($"Level created with seed '{Seed}'");
            _rootConsole = new RLRootConsole(FontFileName, _screenWidth, _screenHeight, 8, 8, 1f, ConsoleTitile);
            _mapConsole = new RLConsole(_mapWidth, _mapHeight);
            _messageConsole = new RLConsole(_messageWidth, _messageHeight);
            _statConsole = new RLConsole(_statWidth, _statHeight);
            _inventoryConsole = new RLConsole(_inventoryWidth, _inventoryHeight);

            Player = new Player();
            SchedulingSystem = new SchedulingSystem();
            SchedSystem = SchedulingSystems.AddFirst(SchedulingSystem);

            // Creating the first level of dungeon
            CommandSystem = new CommandSystem();
            DungeonGenerator MapGenerator = new DungeonGenerator(_mapWidth, _mapHeight, 20, 17, 5, _mapLevel);
            DungeonMap = MapGenerator.CreateMap();
            DungeonMap.UpdatePlayerFieldOfView();
            that = Levels.AddFirst(DungeonMap);

            _rootConsole.Update += OnRootConsoleUpdate;
            _rootConsole.Render += OnRootConsoleRender;
            _rootConsole.Run();
        }
        private static void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            _inventoryConsole.SetBackColor(0, 0, _inventoryWidth, _inventoryHeight, Swatch.DbWood);
            _inventoryConsole.Print(1, 1, "Inventory", Colors.TextHeading);
            bool didPlayerAct = false;
            RLKeyPress keyPress = _rootConsole.Keyboard.GetKeyPress();
            if (CommandSystem.IsPlayerTurn)
            {
                if (keyPress != null)
                {
                    if (keyPress.Key == RLKey.Up)
                        didPlayerAct = CommandSystem.MovePlayer(Direction.Up);
                    else if (keyPress.Key == RLKey.Down)
                        didPlayerAct = CommandSystem.MovePlayer(Direction.Down);
                    else if (keyPress.Key == RLKey.Left)
                        didPlayerAct = CommandSystem.MovePlayer(Direction.Left);
                    else if (keyPress.Key == RLKey.Right)
                        didPlayerAct = CommandSystem.MovePlayer(Direction.Right);
                    else if (keyPress.Key == RLKey.Escape)
                        _rootConsole.Close();
                    else if (keyPress.Key == RLKey.Period)
                    {
                        if (DungeonMap.CanMoveDownToNextLevel())
                        {
                            if (that.Next == null)
                            {
                                DungeonMap.SetIsWalkable(Player.X, Player.Y, true);
                                if (_mapLevel == 1)
                                {
                                    SchedulingSystem A_SchedulingSystem = new SchedulingSystem();
                                    SchedSystem = SchedSystem.Next;
                                    SchedSystem = SchedulingSystems.AddLast(A_SchedulingSystem);
                                    SchedulingSystem = A_SchedulingSystem;

                                    DungeonGenerator mapGenerator = new DungeonGenerator(_mapWidth, _mapHeight, 20, 15, 7, ++_mapLevel);
                                    DungeonMap = mapGenerator.CreateFinalLevel();

                                    MessageLog = new MessageLog();
                                    CommandSystem = new CommandSystem();
                                    _rootConsole.Title = $"RougeSharp RLNet Tutorial - Level {_mapLevel}";
                                    didPlayerAct = true;
                                }
                                else
                                {

                                    SchedulingSystem A_SchedulingSystem = new SchedulingSystem();
                                    SchedSystem = SchedSystem.Next;
                                    SchedSystem = SchedulingSystems.AddLast(A_SchedulingSystem);
                                    SchedulingSystem = A_SchedulingSystem;

                                    DungeonGenerator mapGenerator = new DungeonGenerator(_mapWidth, _mapHeight, 20, 15, 7, ++_mapLevel);
                                    DungeonMap = mapGenerator.CreateMap();

                                    MessageLog = new MessageLog();
                                    CommandSystem = new CommandSystem();
                                    _rootConsole.Title = $"RougeSharp RLNet Tutorial - Level {_mapLevel}";
                                    didPlayerAct = true;

                                }
                                that = that.Next;
                                that = Levels.AddLast(DungeonMap);
                            }
                            else
                            {
                                DungeonMap.SetIsWalkable(Player.X, Player.Y, true);
                                DungeonMap = that.Next.Value;

                                Player.X = DungeonMap.Rooms[0].Center.X - 1;
                                Player.Y = DungeonMap.Rooms[0].Center.Y;

                                SchedulingSystem = SchedSystem.Next.Value;
                                SchedSystem = SchedSystem.Next;

                                MessageLog = new MessageLog();
                                CommandSystem = new CommandSystem();


                                _rootConsole.Title = $"RougeSharp RLNet Tutorial - Level {++_mapLevel}";
                                didPlayerAct = true;
                                that = that.Next;
                            }
                        }
                        if (DungeonMap.CanMoveUpToPreviousLevel())
                        {
                            if (that.Previous != null)
                            {
                                DungeonMap.SetIsWalkable(Player.X, Player.Y, true);
                                DungeonMap = that.Previous.Value;

                                SchedulingSystem = SchedSystem.Previous.Value;
                                SchedSystem = SchedSystem.Previous;

                                Player.X = DungeonMap.Rooms[DungeonMap.Rooms.Count - 1].Center.X - 1;
                                Player.Y = DungeonMap.Rooms[DungeonMap.Rooms.Count - 1].Center.Y;

                                MessageLog = new MessageLog();
                                CommandSystem = new CommandSystem();
                                _rootConsole.Title = $"RougeSharp RLNet Tutorial - Level {--_mapLevel}";
                                didPlayerAct = true;
                                that = that.Previous;
                            }
                        }
                    }
                }
                if (didPlayerAct)
                {
                    _renderRequired = true;
                    CommandSystem.EndPlayerTurn();
                }
            }
            else
            {
                CommandSystem.ActivateMonsters();
                _renderRequired = true;
            }
        }
        private static void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            if (_renderRequired)
            {
                _mapConsole.Clear();
                _statConsole.Clear();
                _messageConsole.Clear();

                MessageLog.Draw(_messageConsole);
                DungeonMap.Draw(_mapConsole, _statConsole);
                Player.Draw(_mapConsole, DungeonMap);
                Player.DrawStats(_statConsole);

                RLConsole.Blit(_mapConsole, 0, 0, _mapWidth, _mapHeight, _rootConsole, 0, _inventoryHeight);
                RLConsole.Blit(_statConsole, 0, 0, _statWidth, _statHeight, _rootConsole, _mapWidth, 0);
                RLConsole.Blit(_messageConsole, 0, 0, _messageWidth, _messageHeight, _rootConsole, 0, _screenHeight - _messageHeight);
                RLConsole.Blit(_inventoryConsole, 0, 0, _inventoryWidth, _inventoryHeight, _rootConsole, 0, 0);

                _rootConsole.Draw();
            }
            _renderRequired = false;
        }
    }
}
