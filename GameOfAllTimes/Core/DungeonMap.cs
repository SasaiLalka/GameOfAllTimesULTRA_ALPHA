using RogueSharp;
using RLNET;
using System.Collections.Generic;
using System.Linq;
using MagiCave.Interfaces;

namespace MagiCave.Core
{
    public class DungeonMap : Map
    {
        public List<Monster> Monsters { get; set; }

        public List<IItem> Items { get; set; }
        public List<Door> Doors { get; set; }
        public Stairs StairsUp { get; set; }
        public Stairs StairsDown { get; set; }

        public List<List<ICell>> Rooms;

        public DungeonMap()
        {
            Rooms = new List<List<ICell>>();
            Monsters = new List<Monster>();
            Doors = new List<Door>();
            Items = new List<IItem>();

        }
        

        public void Draw(RLConsole mapConsole, RLConsole statConsole)
        {
            mapConsole.Clear();
            foreach(ICell cell in GetAllCells())
            {
                SetConsoleSymbolForCell(mapConsole, cell);
            }
            int i = 0;
            foreach(Monster monster in Monsters)
            {
                if (monster.Size == 1)
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
                        {
                            mapConsole.Set(monster.X, monster.Y, Colors.Floor, Colors.FloorBackground, '.');
                        }
                    }
                }
                else
                {
                    // If player can see at least one part of the big monster, draw it fully
                    if (monster.AreaControlled.Any(cell => IsInFov(cell.X,cell.Y)))
                    {
                        monster.Draw(mapConsole, this);
                        monster.DrawStats(statConsole, i);
                        i++;
                    }
                    // If big monster is fully out from FoV, draw dots on its place
                    else if (monster.AreaControlled.All(cell => !IsInFov(cell.X, cell.Y) && IsExplored(cell.X, cell.Y)))
                    {
                        foreach (ICell cell in monster.AreaControlled)
                        {
                            mapConsole.Set(cell.X, cell.Y, Colors.Floor, Colors.FloorBackground, '.');
                        }
                    }
                }
            }
            foreach (IItem item in Items)
            {
                item.Draw(mapConsole, this);
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

        // Checking if player is on the stairs and is able
        // to move to the next level
        public bool CanMoveDownToNextLevel()
        {
            Player player = Game.Player;
            return StairsDown.X == player.X && StairsDown.Y == player.Y;
        }

        // Checking if player is on the stairs and is able
        // to move to the previous level
        public bool CanMoveUpToPreviousLevel()
        {
            Player player = Game.Player;
            return StairsUp.X == player.X && StairsUp.Y == player.Y;
        }

        private void SetConsoleSymbolForCell(RLConsole mapConsole, ICell cell)
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

        // Adding the monster to the list of monsters and to the scheduling system
        // Also defines, where monster is situated and how many cells is controls
        public void AddMonster(Monster monster)
        {
            Monsters.Add(monster);
            if (monster.Size == 1)
            {
                SetIsWalkable(monster.X, monster.Y, false);
                monster.AreaControlled.Add(GetCell(monster.X, monster.Y));
            }
            else
            {
                foreach (ICell cell in GetCellsInSquare(monster.X + 1, monster.Y + 1, 1))
                {
                    SetIsWalkable(cell.X, cell.Y, false);
                    monster.AreaControlled.Add(GetCell(cell.X, cell.Y));
                }
            }
            Game.SchedulingSystem.Add(monster);
        }

        public Point GetRandomWalkableLocationInRoom (List<ICell> cave)
        {
            for (int i = 0; i < 100; i++)
            {
                ICell c = cave[Game.Random.Next(1, cave.Count - 1)];
                if (c.IsWalkable)
                {
                    return new Point(c.X, c.Y);
                }
            }
            return new Point(1 + cave[0].X, 1 + cave[0].Y);
        }

        private void GetFieldOfView()
        {
            ComputeFov(Game.Player.X, Game.Player.Y, (int)(Game.Player.Awareness * 0.6), true);
            List<ICell> circleFov = new List<ICell>();
            var fieldOfView = new FieldOfView(this);
            var cellsInFov = fieldOfView.ComputeFov(Game.Player.X, Game.Player.Y, (Game.Player.Awareness), true);
            var circle = GetCellsInCircle(Game.Player.X, Game.Player.Y, (int)(Game.Player.Awareness *0.6)).ToList();
            foreach (ICell cell in cellsInFov)
            {
                if (circle.Contains(cell))
                {
                    AppendFov(cell.X, cell.Y, 1, true);
                }
            }
        }

        // Calculates player's FoV
        public void UpdatePlayerFieldOfView()
        {
            Player player = Game.Player;
            GetFieldOfView();
            foreach (ICell cell in GetAllCells())
            {
                if (IsInFov(cell.X, cell.Y))
                {
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                }
            }
        }

        // Setting position for actor situated on one cell
        // Gets the actor himself and coordinates of desired cell
        // Returns whether the move was successful
        public bool SetActorPosition(Actor actor, int x, int y)
        { 
            if (GetCell(x, y).IsWalkable)
            {
                SetIsWalkable(actor.X, actor.Y, true);
                actor.AreaControlled.RemoveAll(cell => cell.X == actor.X && cell.Y == actor.Y);
                actor.X = x;
                actor.Y = y;
                SetIsWalkable(x, y, false);
                actor.AreaControlled.Add(GetCell(x, y));
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
            player.AreaControlled.Add(GetCell(player.X, player.Y));
            UpdatePlayerFieldOfView();
            Game.SchedulingSystem.Add(player);
        }

        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            ICell cell = GetCell(x, y);
            SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
        }

        public void RemoveMonster(Monster monster)
        {
            Monsters.Remove(monster);
            if (monster.Size == 1)
            {
                SetIsWalkable(monster.X, monster.Y, true);
                monster.AreaControlled.RemoveAll(cell => cell.X == monster.X && cell.Y == monster.Y);
            }
            else
            {
                foreach (ICell cell in monster.AreaControlled.ToArray())
                {
                    SetIsWalkable(cell.X, cell.Y, true);
                    monster.AreaControlled.RemoveAll(tile => tile.X == cell.X && tile.Y == cell.Y);
                }
            }
            Game.SchedulingSystem.Remove(monster);
        }

        public Monster GetMonsterAt(int x, int y)
        {
            return Monsters.FirstOrDefault(m => m.AreaControlled.Any(cell => cell.X == x && cell.Y == y));
        }
        public IItem GetItemAt(int x, int y)
        {
            return Items.FirstOrDefault(i => i.X == x && i.Y == y);
        }
    }
}
