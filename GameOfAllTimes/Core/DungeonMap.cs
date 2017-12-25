using RogueSharp;
using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameOfAllTimes.Core
{
    public class DungeonMap : Map
    {
        public readonly List<Monster> _monsters;

        public List<Door> Doors { get; set; }
        public Stairs StairsUp { get; set; }
        public Stairs StairsDown { get; set; }

        public List<Rectangle> Rooms;
        public DungeonMap()
        {
            Game.SchedulingSystem.Clear();
            Rooms = new List<Rectangle>();
            _monsters = new List<Monster>();
            Doors = new List<Door>();
        }

        public void Draw(RLConsole mapConsole, RLConsole statConsole)
        {
            mapConsole.Clear();
            foreach(Cell cell in GetAllCells())
            {
                SetConsoleSymbolForCell(mapConsole, cell);
            }
            int i = 0;
            foreach(Monster monster in _monsters)
            {
                if (IsInFov(monster.X, monster.Y))
                {
                    monster.Draw(mapConsole, this);
                    monster.DrawStats(statConsole, i);
                    i++;
                }
                else
                {
                    if (GetCell(monster.X, monster.Y).IsExplored)
                        mapConsole.Set(monster.X, monster.Y, Colors.Floor, Colors.FloorBackground, '.');
                }
            }
            foreach (Door door in Doors)
            {
                door.Draw(mapConsole, this);
            }
            StairsUp.Draw(mapConsole, this);
            StairsDown.Draw(mapConsole, this);
        }

        public Door GetDoor(int x, int y)
        {
            return Doors.SingleOrDefault(d => d.X == x && d.Y == y);
        }

        private void OpenDoor(Actor actor, int x, int y)
        {
            Door door = GetDoor(x, y);
            if (door != null && !door.IsOpen)
            {
                door.IsOpen = true;
                var cell = GetCell(x, y);
                SetCellProperties(x, y, true, cell.IsWalkable, cell.IsExplored);
                Game.MessageLog.Add($"{actor.Name} opened a door");
            }
        }

        public bool CanMoveDownToNextLevel()
        {
            Player player = Game.Player;
            return StairsDown.X == player.X && StairsDown.Y == player.Y;
        }

        public bool CanMoveUpToPreviousLevel()
        {
            Player player = Game.Player;
            return StairsUp.X == player.X && StairsUp.Y == player.Y;
        }

        private void SetConsoleSymbolForCell(RLConsole mapConsole, Cell cell)
        {
            if (!cell.IsExplored)
                return;
            if (IsInFov(cell.X, cell.Y))
            {
                if (cell.IsWalkable)
                    mapConsole.Set(cell.X, cell.Y, Colors.FloorFov, Colors.FloorBackgroundFov, '.');
                else
                    mapConsole.Set(cell.X, cell.Y, Colors.WallFov, Colors.WallBackgroundFov, '#');
            }
            else
            {
                if (cell.IsWalkable)
                    mapConsole.Set(cell.X, cell.Y, Colors.Floor, Colors.FloorBackground, '.');
                else
                    mapConsole.Set(cell.X, cell.Y, Colors.Wall, Colors.WallBackground, '#');
            }
        }

        public void AddMonster(Monster monster)
        {
            _monsters.Add(monster);
            SetIsWalkable(monster.X, monster.Y, false);
            Game.SchedulingSystem.Add(monster);
        }

        public Point GetRandomWalkableLocationInRoom (Rectangle room)
        {
            if (DoesRoomHaveWalkableSpace(room))
            {
                for (int i = 0; i < 100; i++)
                {
                    int x = Game.Random.Next(1, room.Width - 2) + room.X;
                    int y = Game.Random.Next(1, room.Height - 2) + room.Y;
                    if (IsWalkable(x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }
            return null;
        }

        public bool DoesRoomHaveWalkableSpace(Rectangle room)
        {
            for (int x = 1; x <= room.Width - 2; x++)
            {
                for (int y = 1; y <= room.Height - 2; y++)
                {
                    if (IsWalkable(x, y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void UpdatePlayerFieldOfView()
        {
            Player player = Game.Player;
            ComputeFov(player.X, player.Y, player.Awareness, true);
            foreach (Cell cell in GetAllCells())
            {
                if (IsInFov(cell.X, cell.Y))
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
            }
        }

        public bool SetActorPosition(Actor actor, int x, int y)
        {
            if (GetCell(x, y).IsWalkable)
            {
                SetIsWalkable(actor.X, actor.Y, true);
                actor.X = x;
                actor.Y = y;
                SetIsWalkable(actor.X, actor.Y, false);
                OpenDoor(actor, x, y);
                if (actor is Player)
                    UpdatePlayerFieldOfView();
                return true;
            }
            return false;
        }

        public void AddPlayer(Player player)
        {
            Game.Player = player;
            SetIsWalkable(player.X, player.Y, false);
            UpdatePlayerFieldOfView();
            Game.SchedulingSystem.Add(player);
        }

        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            Cell cell = GetCell(x, y);
            SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
        }

        public void RemoveMonster(Monster monster)
        {
            _monsters.Remove(monster);
            SetIsWalkable(monster.X, monster.Y, true);
            Game.SchedulingSystem.Remove(monster);
        }

        public Monster GetMonsterAt(int x, int y)
        {
            return _monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }
    }
}
