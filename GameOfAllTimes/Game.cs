using RLNET;
using MagiCave.Core;
using MagiCave.Systems;
using System;
using RogueSharp.Random;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using RogueSharp;
using System.Runtime.InteropServices;
using System.Media;
using System.IO;
using System.Xml;

namespace MagiCave
{
    static class Game
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static SoundPlayer music = new SoundPlayer(@"Materials\menu.wav");
        static SoundPlayer death = new SoundPlayer(@"Materials\death.wav");

        static int position = 0;
        static string[] helpText = { "Here you are, master! Didn't forget to take a sword?" ,
        "Nice! Your target for today is big and strong", "It is said, that no one haven't return from this cave yet",
        "Hahahahahahahahahahahahaha!!!", "But serious, be careful there...",
        "Good luck and have fun :)", "Press arrows to walk and attack", "Press . or > to use stairs"};
        static int indexHelpText = 0;
        static int indexInventory = 0;
        static string[] menuOptions = { "New game", "Load game", "Help", "Exit" };
        static string[] pauseOptions = { "Resume", "Restart", "To main menu", "Exit" };

        public static char[] symbols = { (char)205, (char)186, (char)187, (char)188, (char)200, (char)201,
                                    (char)185, (char)202, (char)203, (char)204};
        
        public static Stopwatch time = new Stopwatch();
        public static TimeSpan ts;
        
        public static int steps;
        
        private static readonly int screenWidth = 100;
        private static readonly int screenHeight = 70;
        public static RLRootConsole rootConsole;

        public static RLConsole menuConsole;
        
        private static readonly int mapWidth = 80;
        private static readonly int mapHeight = 48;
        private static RLConsole mapConsole;
        
        private static readonly int messageWidth = 80;
        private static readonly int messageHeight = 11;
        private static RLConsole messageConsole;
        
        private static readonly int statWidth = 20;
        private static readonly int statHeight = 70;
        private static RLConsole statConsole;
        
        private static readonly int inventoryWidth = 80;
        private static readonly int inventoryHeight = 11;
        private static RLConsole inventoryConsole;

        public static bool renderRequired = true;

        public static int mapLevel = 1;

        public static int seed;

        public static Player Player { get; set; }
        public static DungeonMap DungeonMap { get; private set; }
        public static IRandom Random { get; private set; }
        public static MessageLog MessageLog = new MessageLog();
        public static SchedulingSystem SchedulingSystem = new SchedulingSystem();
        public static LinkedList<SchedulingSystem> SchedulingSystems = new LinkedList<SchedulingSystem>();
        public static LinkedListNode<SchedulingSystem> CurrentSchedulingSystem;
        public static LinkedList<DungeonMap> Levels = new LinkedList<DungeonMap>();
        public static LinkedListNode<DungeonMap> CurrentLevel;


        static void Main()
        {
            ShowWindow(GetConsoleWindow(), 0);
            string FontFileName = "terminal8x8.png";
            string ConsoleTitile = "MagiCave";
            rootConsole = new RLRootConsole(FontFileName, screenWidth, screenHeight, 8, 8, 1f, ConsoleTitile);
            mapConsole = new RLConsole(mapWidth, mapHeight);
            messageConsole = new RLConsole(messageWidth, messageHeight);
            statConsole = new RLConsole(statWidth, statHeight);
            inventoryConsole = new RLConsole(inventoryWidth, inventoryHeight);
            menuConsole = new RLConsole(screenWidth, screenHeight);
            FillMenu();
            rootConsole.Update += OnMenuUpdate;
            rootConsole.Render += OnMenuRender;
            rootConsole.Run();
        }

        #region Пауза
        private static void OnPauseUpdate(object sender, UpdateEventArgs e)
        {
            RLKeyPress inputKey = rootConsole.Keyboard.GetKeyPress();
            renderRequired = true;
            if (inputKey != null)
            {
                if (inputKey.Key == RLKey.Up)
                {
                    menuConsole.Print((menuConsole.Width - pauseOptions[position].Length) / 2, 40 + position * 5, pauseOptions[position], RLColor.White);
                    position--;
                    if (position == -1)
                        position = 3;
                }
                if (inputKey.Key == RLKey.Down)
                {
                    menuConsole.Print((menuConsole.Width - pauseOptions[position].Length) / 2, 40 + position * 5, pauseOptions[position], RLColor.White);
                    position++;
                    if (position == 4)
                        position = 0;
                }
                menuConsole.Print((menuConsole.Width - pauseOptions[position].Length) / 2, 40 + position * 5, pauseOptions[position], RLColor.LightRed);
                if (inputKey.Key == RLKey.Enter)
                {
                    switch (position)
                    {
                        case 0:
                            {
                                rootConsole.Update -= OnPauseUpdate;
                                rootConsole.Render -= OnMenuRender;
                                rootConsole.Update += OnGameUpdate;
                                rootConsole.Render += OnGameRender;
                                break;
                            }
                        case 1:
                            {
                                rootConsole.Update -= OnPauseUpdate;
                                rootConsole.Render -= OnMenuRender;
                                rootConsole.Update += OnGameUpdate;
                                rootConsole.Render += OnGameRender;
                                StartNewGame();
                                position = 0;
                                break;
                            }
                        case 2:
                            {
                                menuConsole.Clear();
                                rootConsole.Update -= OnPauseUpdate;
                                rootConsole.Render -= OnMenuRender;
                                rootConsole.Update += OnMenuUpdate;
                                rootConsole.Render += OnMenuRender;
                                if (File.Exists("Save.xml"))
                                {
                                    File.Delete("Save.xml");
                                }
                                SerializedGame game = new SerializedGame(true);
                                XmlWriter writer = XmlWriter.Create("Save.xml");
                                game.WriteXml(writer);
                                writer.Close();
                                FillMenu();
                                position = 0;
                                break;
                            }
                        case 3:
                            {
                                if (File.Exists("Save.xml"))
                                {
                                    File.Delete("Save.xml");
                                }
                                SerializedGame game = new SerializedGame(true);
                                XmlWriter writer = XmlWriter.Create("Save.xml");
                                game.WriteXml(writer);
                                writer.Close();
                                rootConsole.Close();
                                break;
                            }
                    }
                }
            }
        }

        private static void Pause()
        {
            time.Stop();
            ts = time.Elapsed;
            rootConsole.Update -= OnGameUpdate;
            rootConsole.Render -= OnGameRender;
            rootConsole.Update += OnPauseUpdate;
            rootConsole.Render += OnMenuRender;
        }
        #endregion
        #region Процессы меню
        private static void DrawLogo()
        {
            char[] str1 = { symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[2], '─', '─', '─', '─',
                            symbols[5], symbols[0], symbols[0], symbols[2], '─',
                            symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[2], '─'};
            string line1 = new string(str1);
            char[] str2 = { symbols[1], '─', '─', '─', '─', '─', '─', '─', '─', '─', '─', '─', '─', '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0], symbols[2],'─','─',symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[3],'─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─', '─', '─', '─', '─', '─', '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0], symbols[2],'─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─', symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─', '─', '─', '─', '─', '─', '─', '─', symbols[1], '─'};
            string line2 = new string(str2);
            char[] str3 = { symbols[1], '─', '─', symbols[5], symbols[0], symbols[0], symbols[2], '─', '─', symbols[5], symbols[0], symbols[0], symbols[2], '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1], '─', '─', '─', '─', '─', '─', '─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0],symbols[0], symbols[0],symbols[0], symbols[3], '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─', symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0],symbols[0], symbols[0],symbols[0], symbols[3], '─'};
            string line3 = new string(str3);
            char[] str4 = { symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─',
                            symbols[1], '─', '─', symbols[4], symbols[0], symbols[0], symbols[3], '─', '─', symbols[1],'─',
                            symbols[1], '─', '─', symbols[4], symbols[0],symbols[0],symbols[0], symbols[0], symbols[0], symbols[2],'─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─','─','─','─',
                            symbols[1], '─', '─', symbols[4], symbols[0], symbols[0], symbols[3], '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─', symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─', '─', symbols[4], symbols[0], symbols[0],symbols[0], symbols[0], symbols[0], symbols[2], '─'};
            string line4 = new string(str4);
            char[] str5 = { symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0], symbols[2],'─','─',symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0], symbols[2],'─','─',symbols[1], '─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─','─','─','─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0], symbols[2],'─','─',symbols[1], '─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─', symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', '─','─','─','─','─','─', symbols[1],'─'};
            string line5 = new string(str5);
            char[] str6 = { symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1],'─','─', '─','─','─', '─','─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1], '─','─','─','─', symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[5], symbols[0], symbols[0],symbols[0], symbols[0],symbols[0], symbols[3], '─' };
            string line6 = new string(str6);
            char[] str7 = { symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─', '─', symbols[1], '─', '─', '─', '─', '─', '─', '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[4],  symbols[0], symbols[0], symbols[0], symbols[0], symbols[3], '─','─', symbols[1], '─',
                            symbols[1], '─', '─', symbols[4], symbols[0], symbols[0],symbols[0], symbols[0], symbols[0], symbols[2], '─'};
            string line7 = new string(str7);
            char[] str8 = { symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─', '─', symbols[1], '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[4], symbols[0], symbols[0], symbols[3],'─','─', symbols[1],'─',
                            symbols[1], '─','─', symbols[1], '─',
                            symbols[1], '─','─', symbols[4], symbols[0], symbols[0],symbols[0], symbols[0],symbols[0], symbols[2], '─',
                            symbols[1], '─','─', symbols[1],'─','─', symbols[1],'─','─', symbols[1],'─',
                            symbols[4], symbols[0], symbols[0], symbols[2], '─','─','─','─', symbols[5], symbols[0], symbols[0], symbols[3],'─',
                            symbols[1], '─', '─', '─', '─', '─', '─', '─', '─', symbols[1], '─' };
            string line8 = new string(str8);
            char[] str9 = { symbols[4], symbols[0], symbols[0], symbols[3], '─', '─', symbols[4], symbols[0], symbols[0], symbols[3], '─', '─', symbols[4], symbols[0], symbols[0], symbols[3], '─',
                            symbols[4], symbols[0], symbols[0], symbols[3], '─', '─', symbols[4], symbols[0], symbols[0], symbols[3], '─',
                            symbols[4], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[3], '─',
                            symbols[4], symbols[0], symbols[0], symbols[3], '─',
                            symbols[4], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[3], '─',
                            symbols[4], symbols[0], symbols[0], symbols[3], '─', '─', symbols[4], symbols[0], symbols[0], symbols[3], '─',
                            '─', '─', '─', symbols[4], symbols[0],symbols[0],symbols[0], symbols[0],symbols[3], '─', '─', '─','─',
                            symbols[4], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[0], symbols[3], '─'};
            string line9 = new string(str9);
            menuConsole.Print((menuConsole.Width - line1.Length) / 2, 10, line1, RLColor.White);
            menuConsole.Print((menuConsole.Width - line2.Length) / 2, 11, line2, RLColor.White);
            menuConsole.Print((menuConsole.Width - line3.Length) / 2, 12, line3, RLColor.White);
            menuConsole.Print((menuConsole.Width - line4.Length) / 2, 13, line4, RLColor.White);
            menuConsole.Print((menuConsole.Width - line5.Length) / 2, 14, line5, RLColor.White);
            menuConsole.Print((menuConsole.Width - line6.Length) / 2, 15, line6, RLColor.White);
            menuConsole.Print((menuConsole.Width - line7.Length) / 2, 16, line7, RLColor.White);
            menuConsole.Print((menuConsole.Width - line8.Length) / 2, 17, line8, RLColor.White);
            menuConsole.Print((menuConsole.Width - line9.Length) / 2, 18, line9, RLColor.White);
        }
        private static void FillMenu()
        {
            DrawLogo();
            music.PlayLooping();
            for (int i = 0; i < 4; i++)
            {
                menuConsole.Print((menuConsole.Width - menuOptions[i].Length) / 2, 40 + i * 5, menuOptions[i], RLColor.White);
            }
            menuConsole.Print((menuConsole.Width - menuOptions[0].Length) / 2, 40, menuOptions[0], RLColor.LightRed);
        }
        private static void OnMenuUpdate(object sender, UpdateEventArgs e)
        {
            RLKeyPress inputKey = rootConsole.Keyboard.GetKeyPress();
            renderRequired = true;
            if (inputKey != null)
            {
                if (inputKey.Key == RLKey.Up)
                {
                    menuConsole.Print((menuConsole.Width - menuOptions[position].Length) / 2, 40 + position * 5, menuOptions[position], RLColor.White);
                    position--;
                    if (position == -1)
                        position = 3;
                }
                if (inputKey.Key == RLKey.Down)
                {
                    menuConsole.Print((menuConsole.Width - menuOptions[position].Length) / 2, 40 + position * 5, menuOptions[position], RLColor.White);
                    position++;
                    if (position == 4)
                        position = 0;
                }
                menuConsole.Print((menuConsole.Width - menuOptions[position].Length) / 2, 40 + position * 5, menuOptions[position], RLColor.LightRed);
                if (inputKey.Key == RLKey.Enter)
                {
                    switch (position)
                    {
                        case 0:
                            {
                                rootConsole.Update -= OnMenuUpdate;
                                rootConsole.Render -= OnMenuRender;
                                rootConsole.Update += OnGameUpdate;
                                rootConsole.Render += OnGameRender;
                                music.Stop();
                                StartNewGame();
                                position = 0;
                                break;
                            }
                        case 1:
                            {
                                music.Stop();
                                try
                                {
                                    if (File.Exists("Save.xml"))
                                    {
                                        XmlReader reader = XmlReader.Create("Save.xml");
                                        ShowWindow(GetConsoleWindow(), 0);
                                        SerializedGame game = new SerializedGame(false);
                                        game.ReadXml(reader);
                                        reader.Close();
                                        ContinueOldGame(game);
                                    }
                                }
                                catch
                                {
                                    MessageLog.Add("Failed to load the game, new game is started instead");
                                    StartNewGame();
                                }
                                finally
                                {

                                    rootConsole.Update -= OnMenuUpdate;
                                    rootConsole.Render -= OnMenuRender;
                                    rootConsole.Update += OnGameUpdate;
                                    rootConsole.Render += OnGameRender;
                                    position = 0;
                                }
                                break;
                            }
                        case 2:
                            {
                                Help();
                                position = 0;
                                break;
                            }
                        case 3:
                            {
                                rootConsole.Close();
                                break;
                            }
                    }
                }

            }
        }
        private static void OnMenuRender(object sender, UpdateEventArgs e)
        {
            if (renderRequired == true)
            {
                rootConsole.Clear();
                RLConsole.Blit(menuConsole, 0, 0, menuConsole.Width, menuConsole.Height, rootConsole, 0, 0);
                rootConsole.Draw();
                renderRequired = false;
            }
        }
        #endregion
        #region Игровая сессия
        public static void ContinueOldGame(SerializedGame game)
        {
            Check();
            Random = new DotNetRandom();
            mapLevel = game.mapLevel;
            string ConsoleTitle = $"MagiCave - Level {mapLevel}";
            rootConsole.Title = ConsoleTitle;
            Random.Restore(game.Random);
            steps = game.steps;
            Player = game.Player;
            for(int i = 0; i < game.Levels.Length; i++)
            {
                SchedulingSystem = new SchedulingSystem();
                DungeonMap levelmap = new DungeonMap();
                levelmap.Restore(game.Levels[i]);
                levelmap.Doors = game.Doors[i].ToList();
                for (int k = 0; k < mapWidth * mapHeight; k++)
                {
                    if ((game.Levels[i].Cells[k] & MapState.CellProperties.Explored) == MapState.CellProperties.Explored)
                    {
                        int l;
                        int j;
                        if (k >= mapWidth)
                        {
                            j = k / mapWidth;
                            l = k - j * mapWidth;
                        }
                        else
                        {
                            l = k;
                            j = 0;
                        }
                        levelmap.SetCellProperties(l, j, levelmap.GetCell(l, j).IsTransparent, levelmap.GetCell(l, j).IsWalkable, true);
                    }
                }
                //levelmap.Rooms = game.Rooms[i].ToList();
                levelmap.Monsters = game.MonstersOnLevel[i].ToList();
                foreach (Monster m in levelmap.Monsters)
                {
                    if (m.Items == null)
                    {
                        m.Items = new List<Interfaces.IItem>();
                    }
                }
                DungeonGenerator gen = new DungeonGenerator(levelmap);
                gen.Restore(SchedulingSystem);
                SchedulingSystems.AddLast(SchedulingSystem);
                Levels.AddLast(levelmap);
                i++;
            }
            CurrentSchedulingSystem = SchedulingSystems.First;
            CurrentLevel = Levels.First;
            for (int j = 0; j < mapLevel - 1; j++)
            {
                CurrentSchedulingSystem = CurrentSchedulingSystem.Next;
                CurrentLevel = CurrentLevel.Next;
            }
            SchedulingSystem = CurrentSchedulingSystem.Value;
            DungeonMap = CurrentLevel.Value;
            
            MessageLog.Add("Game loaded successfully");

            Player = game.Player;
            
            DungeonMap.UpdatePlayerFieldOfView();

            ts.Add(game.ts);
            time.Start();

            renderRequired = true;
        }
        private static void Check()
        {

            if (Levels.Count != 0)
            {
                messageConsole.Clear();
                inventoryConsole.Clear();
                statConsole.Clear();
                CommandSystem.PlayerIsDead = false;
                CommandSystem.IsGameEnded = false;
                Player = null;
                Levels.Clear();
                SchedulingSystems.Clear();
                CurrentLevel = null;
                CurrentSchedulingSystem = null;
                DungeonMap = null;
                SchedulingSystem = new SchedulingSystem();
                MessageLog.Clear();
            }
        }
        public static void StartNewGame()
        {
            Check();
            rootConsole.Title = "MagiCave - Level 1";
            mapLevel = 1;
            time.Restart();
            seed = (int)DateTime.UtcNow.Ticks;
            Random = new DotNetRandom(seed);
            steps = 0;
            
            MessageLog.Add("The rogue arrives on level 1");
            Player = new Player();
            CurrentSchedulingSystem = SchedulingSystems.AddFirst(SchedulingSystem);
            
            DungeonGenerator MapGenerator = new DungeonGenerator(mapWidth, mapHeight, 20, 17, 5, mapLevel);
            DungeonMap = MapGenerator.CreateMap(SchedulingSystem);
            DungeonMap.UpdatePlayerFieldOfView();
            CurrentLevel = Levels.AddFirst(DungeonMap);
            renderRequired = true;
        }
#endregion
        #region Помощь
        private static void Help()
        {
            rootConsole.Update -= OnMenuUpdate;
            rootConsole.Update += OnHelpUpdate;
            menuConsole.Clear();
            menuConsole.Print((screenWidth - helpText[indexHelpText].Length) / 2, 10 + 5 * indexHelpText, helpText[indexHelpText], RLColor.White, null);
            indexHelpText++;
        }
        private static void OnHelpUpdate(object sender, UpdateEventArgs e)
        {
            RLKeyPress inputKey = rootConsole.Keyboard.GetKeyPress();
            renderRequired = true;
            if (inputKey != null)
            {
                if (indexHelpText < 6)
                {
                    menuConsole.Print((screenWidth - helpText[indexHelpText].Length) / 2, 10 + 5 * indexHelpText, helpText[indexHelpText], RLColor.White, null);
                    indexHelpText++;
                }
                else if (indexHelpText < 8)
                {
                    menuConsole.Print((screenWidth - helpText[indexHelpText].Length) / 2, 20 + 5 * indexHelpText, helpText[indexHelpText], RLColor.White, null);
                    indexHelpText++;
                }
                else
                {
                    indexHelpText = 0;
                    menuConsole.Clear();
                    FillMenu();
                    rootConsole.Update -= OnHelpUpdate;
                    rootConsole.Update += OnMenuUpdate;
                }
            }
        }
        #endregion
        #region Завершение игры
        private static void DrawStats()
        {
            char[] str1 = new char[] { symbols[5], symbols[0], symbols[0], symbols[8], symbols[0], symbols[0], symbols[0], symbols[0], symbols[8], symbols[0], symbols[0], symbols[8], symbols[0], symbols[0], symbols[0], symbols[0], symbols[8], symbols[0], symbols[0], symbols[2] };
            char[] str2 = new char[] { symbols[1], symbols[5], symbols[0], symbols[7], symbols[0], symbols[2], symbols[5], symbols[0], symbols[6], symbols[5], symbols[2], symbols[9], symbols[0], symbols[2], symbols[5], symbols[0], symbols[6], symbols[5], symbols[0], symbols[3] };
            char[] str3 = new char[] { symbols[1], symbols[4], symbols[0], symbols[2], '─', symbols[1], symbols[1], '─', symbols[1], symbols[4], symbols[3], symbols[1], '─', symbols[1], symbols[1], '─', symbols[1], symbols[4], symbols[0], symbols[2] };
            char[] str4 = new char[] { symbols[4], symbols[0], symbols[2], symbols[1], '─', symbols[1], symbols[1], '─', symbols[1], symbols[5], symbols[2], symbols[1], '─', symbols[1], symbols[1], '─', symbols[4], symbols[0], symbols[2], symbols[1] };
            char[] str5 = new char[] { symbols[5], symbols[0], symbols[3], symbols[1], '─', symbols[1], symbols[1], '─', symbols[1], symbols[1], symbols[1], symbols[1], '─', symbols[1], symbols[1], '─', symbols[5], symbols[0], symbols[3], symbols[1] };
            char[] str6 = new char[] { symbols[4], symbols[0], symbols[0], symbols[3], '─', symbols[4], symbols[3], '─', symbols[4], symbols[3], symbols[4], symbols[3], '─', symbols[4], symbols[3], '─', symbols[4], symbols[0], symbols[0], symbols[3] };
            string line1 = new string(str1);
            string line2 = new string(str2);
            string line3 = new string(str3);
            string line4 = new string(str4);
            string line5 = new string(str5);
            string line6 = new string(str6);
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
            ts.Hours, ts.Minutes, ts.Seconds);
            menuConsole.Print((screenWidth - line1.Length) / 2, 30, line1, RLColor.White);
            menuConsole.Print((screenWidth - line1.Length) / 2, 31, line2, RLColor.White);
            menuConsole.Print((screenWidth - line1.Length) / 2, 32, line3, RLColor.White);
            menuConsole.Print((screenWidth - line1.Length) / 2, 33, line4, RLColor.White);
            menuConsole.Print((screenWidth - line1.Length) / 2, 34, line5, RLColor.White);
            menuConsole.Print((screenWidth - line1.Length) / 2, 35, line6, RLColor.White);
            if (CommandSystem.PlayerIsDead)
            {
                menuConsole.Print((screenWidth - "Dungeoneer: ".Length) / 2, 40, "Dungeoneer: " + Player.Name, RLColor.White);
                menuConsole.Print((screenWidth - "Level: ".Length) / 2, 41, "Level: " + mapLevel, RLColor.White);
                menuConsole.Print((screenWidth - "Time: ".Length) / 2, 42, "Time: " + elapsedTime, RLColor.White);
                menuConsole.Print((screenWidth - "Moves: ".Length) / 2, 43, "Moves: " + steps, RLColor.White);
                menuConsole.Print((screenWidth - "Gold: ".Length) / 2, 44, "Gold: " + Player.Gold, RLColor.White);
                menuConsole.Print((screenWidth - "Kills: ".Length) / 2, 45, "Kills: " + Player.Kills, RLColor.White);
                menuConsole.Print((screenWidth - "Killed by: ".Length) / 2, 46, "Killed by: " + CommandSystem.KilledBy, RLColor.White);

            }
            else
            {
                menuConsole.Print((screenWidth - "Time: ".Length) / 2, 40, "Time: " + elapsedTime, RLColor.White);
                menuConsole.Print((screenWidth - "Moves: ".Length) / 2, 41, "Moves: " + steps, RLColor.White);
                menuConsole.Print((screenWidth - "Gold: ".Length) / 2, 42, "Gold: " + Player.Gold, RLColor.White);
                menuConsole.Print((screenWidth - "Kills: ".Length) / 2, 43, "Kills: " + Player.Kills, RLColor.White);
                menuConsole.Print((screenWidth - "KNOCKOUT!".Length) / 2, 44, "KNOCKOUT!", RLColor.White);
            }
        }
        private static void OnEndUpdate(object sender, UpdateEventArgs e)
        {
            RLKeyPress inputKey = rootConsole.Keyboard.GetKeyPress();
            renderRequired = true;
            if (inputKey != null)
            {
                if (inputKey.Key == RLKey.Escape)
                {
                    rootConsole.Close();
                }
                else
                {
                    menuConsole.Clear();
                    FillMenu();
                    rootConsole.Update -= OnEndUpdate;
                    rootConsole.Update += OnMenuUpdate;
                }
            }
        }
        private static void DeathMenu()
        {
            rootConsole.Title = "MagiCave";
            menuConsole.Clear();
            menuConsole.Print(screenWidth / 2, 5, "You died", RLColor.White, null);
            DrawStats();
            rootConsole.Update -= OnGameUpdate;
            rootConsole.Render -= OnGameRender;
            rootConsole.Update += OnEndUpdate;
            rootConsole.Render += OnMenuRender;
            death.Play();
        }
        private static void WinMenu()
        {
            rootConsole.Title = "MagiCave";
            menuConsole.Clear();
            menuConsole.Print(screenWidth / 2, 5, "You won", RLColor.White, null);
            DrawStats();
            rootConsole.Update -= OnGameUpdate;
            rootConsole.Render -= OnGameRender;
            rootConsole.Update += OnEndUpdate;
            rootConsole.Render += OnMenuRender;
            death.Play();

        }
        #endregion
        #region Игровой процесс
        
        private static void OnItemUpdate(object sender, UpdateEventArgs e)
        {
            RLKeyPress keyPress = rootConsole.Keyboard.GetKeyPress();
            if (keyPress != null)
            {
                renderRequired = true;
                if (keyPress.Key == RLKey.Up)
                {
                    inventoryConsole.Print(0, indexInventory + 2, " ", Colors.Text);
                    inventoryConsole.Print(1, indexInventory + 2, Player.Items[indexInventory].ToString(), Colors.Text);
                    indexInventory--;
                    if (indexInventory == -1)
                    {
                        indexInventory = Player.Items.Count - 1;
                    }
                    inventoryConsole.Print(0, indexInventory + 2, ">", Colors.Text);
                }
                if (keyPress.Key == RLKey.Down)
                {
                    inventoryConsole.Print(0, indexInventory + 2, " ", Colors.Text);
                    indexInventory++;
                    if (indexInventory == Player.Items.Count || indexInventory + 2 == inventoryConsole.Height)
                    {
                        indexInventory = 0;
                    }
                    inventoryConsole.Print(0, indexInventory + 2, ">", Colors.Text);
                }
                if (keyPress.Key == RLKey.Enter)
                {
                    indexInventory = 0;
                    Player.Items[indexInventory].Use(Player);
                    CommandSystem.EndPlayerTurn();
                    inventoryConsole.Clear();
                    Player.DrawItems(inventoryConsole);
                    rootConsole.Update -= OnItemUpdate;
                    rootConsole.Update += OnGameUpdate;
                }
                if (keyPress.Key == RLKey.I || keyPress.Key == RLKey.Escape)
                {
                    indexInventory = 0;
                    rootConsole.Update -= OnItemUpdate;
                    rootConsole.Update += OnGameUpdate;
                    inventoryConsole.Clear();
                    Player.DrawItems(inventoryConsole);
                }
            }
        }
        private static void OnGameRender(object sender, UpdateEventArgs e)
        {
            if (renderRequired)
            {
                mapConsole.Clear();
                statConsole.Clear();
                messageConsole.Clear();

                MessageLog.Draw(messageConsole);
                DungeonMap.Draw(mapConsole, statConsole);
                Player.Draw(mapConsole, DungeonMap);
                Player.DrawStats(statConsole);
                Player.DrawItems(inventoryConsole);

                RLConsole.Blit(mapConsole, 0, 0, mapWidth, mapHeight, rootConsole, 0, inventoryHeight);
                RLConsole.Blit(statConsole, 0, 0, statWidth, statHeight, rootConsole, mapWidth, 0);
                RLConsole.Blit(messageConsole, 0, 0, messageWidth, messageHeight, rootConsole, 0, screenHeight - messageHeight);
                RLConsole.Blit(inventoryConsole, 0, 0, inventoryWidth, inventoryHeight, rootConsole, 0, 0);

                rootConsole.Draw();
            }
            renderRequired = false;
        }
        private static void OnGameUpdate(object sender, UpdateEventArgs e)
        {
            bool didPlayerAct = false;
            RLKeyPress keyPress = rootConsole.Keyboard.GetKeyPress();
            if (CommandSystem.IsPlayerTurn)
            {
                if (CommandSystem.PlayerIsDead)
                {
                    time.Stop();
                    ts = time.Elapsed;
                    DeathMenu();
                }
                if (CommandSystem.IsGameEnded)
                {
                    time.Stop();
                    ts = time.Elapsed;
                    WinMenu();
                }
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
                    else if (keyPress.Key == RLKey.I)
                    {
                        if (Player.Items.Count != 0)
                        {
                            inventoryConsole.Print(0,2, ">", Colors.Text);
                            renderRequired = true;
                            rootConsole.Update -= OnGameUpdate;
                            rootConsole.Update += OnItemUpdate;
                        }
                    }
                    else if (keyPress.Key == RLKey.Escape)
                    {
                        rootConsole.Title = "MagiCave";
                        menuConsole.Clear();
                        for (int i = 0; i < 4; i++)
                        {
                            menuConsole.Print((menuConsole.Width - pauseOptions[i].Length) / 2, 40 + i * 5, pauseOptions[i], RLColor.White);
                        }
                        menuConsole.Print((menuConsole.Width - pauseOptions[0].Length) / 2, 40, pauseOptions[0], RLColor.LightRed);
                        Pause();
                    }
                    else if (keyPress.Key == RLKey.Period)
                    {
                        if (DungeonMap.CanMoveDownToNextLevel())
                        {
                            if (CurrentLevel.Next == null)
                            {
                                DungeonMap.SetIsWalkable(Player.X, Player.Y, true);
                                // If this is going to be a final level
                                if (mapLevel == 4)
                                {
                                    SchedulingSystem A_SchedulingSystem = new SchedulingSystem();
                                    CurrentSchedulingSystem = CurrentSchedulingSystem.Next;
                                    CurrentSchedulingSystem = SchedulingSystems.AddLast(A_SchedulingSystem);
                                    SchedulingSystem = A_SchedulingSystem;

                                    DungeonGenerator mapGenerator = new DungeonGenerator(mapWidth, mapHeight, 20, 15, 7, ++mapLevel);
                                    DungeonMap = mapGenerator.CreateFinalLevel(SchedulingSystem);
                                    
                                    rootConsole.Title = $"MagiCave - Level {mapLevel}";
                                    DungeonMap.UpdatePlayerFieldOfView();
                                    didPlayerAct = true;
                                }
                                // Creating generic level
                                else
                                {
                                    SchedulingSystem A_SchedulingSystem = new SchedulingSystem();
                                    CurrentSchedulingSystem = CurrentSchedulingSystem.Next;
                                    CurrentSchedulingSystem = SchedulingSystems.AddLast(A_SchedulingSystem);
                                    SchedulingSystem = A_SchedulingSystem;

                                    DungeonGenerator mapGenerator = new DungeonGenerator(mapWidth, mapHeight, 20, 15, 7, ++mapLevel);
                                    DungeonMap = mapGenerator.CreateMap(SchedulingSystem);
                                    rootConsole.Title = $"MagiCave - Level {mapLevel}";
                                    didPlayerAct = true;
                                    DungeonMap.UpdatePlayerFieldOfView();

                                }
                                CurrentLevel = CurrentLevel.Next;
                                CurrentLevel = Levels.AddLast(DungeonMap);
                            }
                            // Moving to already created level
                            else
                            {
                                DungeonMap.SetIsWalkable(Player.X, Player.Y, true);
                                DungeonMap = CurrentLevel.Next.Value;

                                Player.X = DungeonMap.Rooms.First()[DungeonMap.Rooms.First().Count / 2].X;
                                Player.Y = DungeonMap.Rooms.First()[DungeonMap.Rooms.First().Count / 2].Y;

                                SchedulingSystem = CurrentSchedulingSystem.Next.Value;
                                CurrentSchedulingSystem = CurrentSchedulingSystem.Next;
                                
                                rootConsole.Title = $"MagiCave - Level {++mapLevel}";
                                didPlayerAct = true;
                                CurrentLevel = CurrentLevel.Next;
                                DungeonMap.UpdatePlayerFieldOfView();
                            }
                        }
                        // Moving to the previous level
                        if (DungeonMap.CanMoveUpToPreviousLevel() && !didPlayerAct)
                        {
                            if (CurrentLevel.Previous != null)
                            {
                                DungeonMap.SetIsWalkable(Player.X, Player.Y, true);
                                DungeonMap = CurrentLevel.Previous.Value;

                                SchedulingSystem = CurrentSchedulingSystem.Previous.Value;
                                CurrentSchedulingSystem = CurrentSchedulingSystem.Previous;
                                
                                Player.X = DungeonMap.Rooms.Last()[DungeonMap.Rooms.Last().Count / 2].X;
                                Player.Y = DungeonMap.Rooms.Last()[DungeonMap.Rooms.Last().Count / 2].Y;

                                rootConsole.Title = $"MagiCave - Level {--mapLevel}";
                                didPlayerAct = true;
                                CurrentLevel = CurrentLevel.Previous;
                                DungeonMap.UpdatePlayerFieldOfView();
                            }
                        }
                    }
                }
                if (didPlayerAct)
                {
                    steps++;
                    renderRequired = true;
                    CommandSystem.EndPlayerTurn();
                }
            }
            else
            {
                CommandSystem.ActivateMonsters(SchedulingSystem);
                renderRequired = true;
            }
        }
        #endregion
    }
}