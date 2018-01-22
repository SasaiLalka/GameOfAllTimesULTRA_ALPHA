using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using GameOfAllTimes;
using System.Runtime.InteropServices;

namespace GameOfAllTimes.Systems
{
    class Menu
    {
        //Меню Win32 API
        const int MF_BYCOMMAND = 0x00000000;
        const int SC_SIZE = 0xF000;
        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        // Музыка
        static SoundPlayer music = new SoundPlayer("menu.wav");
        // Контроль паузы 
        static int pause = 0;

        public static void PrintMenu()
        {
            // Запрет на изменения размера
            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);
            if (handle != IntPtr.Zero)
            {
                DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }


            while (true)
            {
                if (MainMenu() == true)
                {
                    Game.NewGameStart();
                    if (Game._rootConsole.IsWindowClosed())
                    {
                        switch (pause)
                        {
                            case 1:
                                Game.NewGameStart();
                                break;
                            case 2:
                                continue;
                            case 3:
                                return;
                        }
                    }
                }
            }
        }

        public static bool MainMenu()
        {
            // Вкл музыку
            music.PlayLooping();
            // Настройки консоли
            Console.SetWindowSize(80, 22);
            Console.SetBufferSize(80, 22);
            Console.Title = "Magicave";
            // Выбор
            string[] menuItems = { "New game", "Load game", "Help", "Exit" };
            // Переменная, для определение размера строки
            string temp = "███████████████████████████████████████";
            int counter = 0;
            bool flag = true;
            ConsoleKeyInfo key;
            while (flag == true)
            {
                Console.Clear();
                Console.CursorVisible = false;
                do
                {
                    Console.Clear();

                    DrawName();

                    Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop + 3);

                    for (int i = 0; i < menuItems.Length; i++)
                    {
                        if (counter == i)
                        {
                            Console.SetCursorPosition((Console.WindowWidth - menuItems[i].Length) / 2, Console.CursorTop);
                            Console.BackgroundColor = ConsoleColor.Cyan;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine(menuItems[i]);
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else
                        {
                            Console.SetCursorPosition((Console.WindowWidth - menuItems[i].Length) / 2, Console.CursorTop);
                            Console.WriteLine(menuItems[i]);
                        }
                    }
                    key = Console.ReadKey();
                    if (key.Key == ConsoleKey.UpArrow)
                    {
                        counter--;
                        if (counter == -1) counter = menuItems.Length - 1;
                    }
                    if (key.Key == ConsoleKey.DownArrow)
                    {
                        counter++;
                        if (counter == menuItems.Length) counter = 0;
                    }
                }
                while (key.Key != ConsoleKey.Enter);

                switch (counter)
                {
                    case 0:
                        // New game
                        music.Stop();
                        // Скрытие консольного окна
                        ShowWindow(GetConsoleWindow(), 0);
                        flag = false;
                        break;
                    case 1:
                        // Load game
                        Console.WriteLine("Still not made yet");
                        Console.ReadLine();
                        break;
                    case 2:
                        // Help
                        Help();
                        break;
                    case 3:
                        // Exit
                        Environment.Exit(0);
                        break;
                }
            }
            return true;
        }

        public static void Help()
        {
            Console.Clear();
            Console.CursorVisible = true;
            Console.CursorTop = Console.WindowHeight / 6;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            DrawOnCenter("Here you are, master! Didn't forget to take a sword?");
            Console.ReadLine();
            DrawOnCenter("Nice! Your target for today is big and strong");
            Console.ReadLine();
            DrawOnCenter("It is said, that no one haven't return from this cave yet");
            Console.ReadLine();
            DrawOnCenter("Hahahahahahahahahahahahaha!!!");
            Console.ReadLine();
            DrawOnCenter("But serious, be careful there...");
            Console.ReadLine();
            DrawOnCenter("Good luck and have fun :)");
            Console.ReadLine();
            Console.CursorTop += 4;

            DrawOnCenter("Press arrows to walk and attack");
            DrawOnCenter("Press . or > to use stairs");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadLine();

            Console.SetCursorPosition(0, 0);
        }

        public static void Pause()
        {
            // Показ консольного окна
            ShowWindow(GetConsoleWindow(), 1);
            // Переменная, для определение размера строки
            string[] pauseItems = { "Resume", "Restart", "To main menu", "Exit" };
            string temp = "███████████████████████████████████████";
            int counter = 0;
            ConsoleKeyInfo key;
            do
            {
                Console.Clear();

                DrawName();

                Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop + 3);

                for (int i = 0; i < pauseItems.Length; i++)
                {
                    if (counter == i)
                    {
                        Console.SetCursorPosition((Console.WindowWidth - pauseItems[i].Length) / 2, Console.CursorTop);
                        Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine(pauseItems[i]);
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.SetCursorPosition((Console.WindowWidth - pauseItems[i].Length) / 2, Console.CursorTop);
                        Console.WriteLine(pauseItems[i]);
                    }
                }
                key = Console.ReadKey();
                if (key.Key == ConsoleKey.UpArrow)
                {
                    counter--;
                    if (counter == -1) counter = pauseItems.Length - 1;
                }
                if (key.Key == ConsoleKey.DownArrow)
                {
                    counter++;
                    if (counter == pauseItems.Length) counter = 0;
                }
            }
            while (key.Key != ConsoleKey.Enter);

            switch (counter)
            {
                case 0:
                    // Скрытие консольного окна
                    ShowWindow(GetConsoleWindow(), 0);
                    break;
                case 1:
                    // Restart
                    pause = 1;
                    Game._rootConsole.Close();
                    ShowWindow(GetConsoleWindow(), 0);
                    Game.NewGameStart();
                    break;
                case 2:
                    // To main menu
                    pause = 2;
                    Game._rootConsole.Close();
                    break;
                case 3:
                    // Exit
                    pause = 3;
                    Game._rootConsole.Close();
                    break;
            }
        }
        public static void DeathMenu()
        {
            string temp = "███████████████████████████████████████";

            ShowWindow(GetConsoleWindow(), 1);

            Console.Clear();

            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.WindowHeight / 5);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            DrawOnCenter("██─██─████─█──█");
            DrawOnCenter("─███──█──█─█──█");
            DrawOnCenter("──█───█──█─█──█");
            DrawOnCenter("──█───█──█─█──█");
            DrawOnCenter("──█───████─████");
            DrawOnCenter("──█────────────");
            DrawOnCenter(" ");
            DrawOnCenter("████──███─███─████ ");
            DrawOnCenter("█──██──█──█───█──██");
            DrawOnCenter("█──██──█──███─█──██");
            DrawOnCenter("█──██──█──█───█──██");
            DrawOnCenter("████──███─███─████ ");

            Console.CursorTop += 4;

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;

            DrawOnCenter("Press any key to continue");
            Console.ReadKey();

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;

        }
        
        public static void DrawOnCenter(string str)
        {
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);
        }

        public static void DrawName()
        {
            string temp = "███████████████████████████████████████";
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.WindowHeight / 6);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("███████████████████████████████████████");
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("█─███─█────█────█───█────█────█─█─█───█");
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("█──█──█─██─█─█████─██─██─█─██─█─█─█─███");
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("█─█─█─█────█─█──██─██─████────█─█─█───█");
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("█─███─█─██─█─██─██─██─██─█─██─█───█─███");
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("█─███─█─██─█────█───█────█─██─██─██───█");
            Console.SetCursorPosition((Console.WindowWidth - temp.Length) / 2, Console.CursorTop);
            Console.WriteLine("███████████████████████████████████████");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}