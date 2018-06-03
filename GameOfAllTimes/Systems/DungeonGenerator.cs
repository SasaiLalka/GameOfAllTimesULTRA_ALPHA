using MagiCave.Core;
using RogueSharp;
using System;
using System.Linq;
using RogueSharp.DiceNotation;
using MagiCave.Monsters;
using System.Collections.Generic;

namespace MagiCave.Systems
{
    class DungeonGenerator
    {
        private readonly int width;
        private readonly int height;
        private readonly int maxRooms;
        private readonly int maxRoomSize;
        private readonly int minRoomSize;

        private readonly DungeonMap map;

        public DungeonGenerator(int width, int height, int maxRooms, int maxRoomSize, int minRoomSize, int mapLevel)
        {
            this.width = width;
            this.height = height;
            this.maxRooms = maxRooms;
            this.maxRoomSize = maxRoomSize;
            this.minRoomSize = minRoomSize;
            map = new DungeonMap();
        }
        public DungeonGenerator(DungeonMap map)
        {
            this.map = map;
        }

        // Creating default level
        public DungeonMap CreateMap(SchedulingSystem schedule)
        {
            map.Initialize(width, height);
            // Creating rooms
            for (int r = maxRooms; r > 0; r--)
            {
                int roomWidth = Game.Random.Next(minRoomSize, maxRoomSize);
                int roomHeight = Game.Random.Next(minRoomSize, maxRoomSize);
                int xPosition = Game.Random.Next(0, width - roomWidth - 1);
                int yPosition = Game.Random.Next(0, height - roomHeight - 1);
                var newRoom = new Rectangle(xPosition, yPosition, roomWidth, roomHeight);
                // If created room isn't intersected by any other room which already exists, add it to the list
                if (!map.Rooms.Any(room => newRoom.Intersects(room)))
                {
                    map.Rooms.Add(newRoom);
                }
            }
            // Creating tunnels between rooms
            for (int r = 1; r < map.Rooms.Count; r++)
            {
                int previousRoomCenterX = map.Rooms[r - 1].Center.X;
                int previousRoomCenterY = map.Rooms[r - 1].Center.Y;
                int currentRoomCenterX = map.Rooms[r].Center.X;
                int currentRoomCenterY = map.Rooms[r].Center.Y;
                if (Game.Random.Next(1, 2) == 1)
                {
                    CreateHorizontalTunnel(previousRoomCenterX, currentRoomCenterX, previousRoomCenterY);
                    CreateVerticalTunnel(previousRoomCenterY, currentRoomCenterY, currentRoomCenterX);
                }
                else
                {
                    CreateVerticalTunnel(previousRoomCenterY, currentRoomCenterY, previousRoomCenterX);
                    CreateHorizontalTunnel(previousRoomCenterX, currentRoomCenterX, currentRoomCenterY);
                }
            }
            foreach (Rectangle room in map.Rooms)
            {
                CreateRoom(room);
                CreateDoors(room);
            }
            CreateStairs();
            PlacePlayer();
            PlaceMonsters();
            return map;
        }

        // Creating the final level of the game
        // Killing the final boss will end the game
        // So there is no need to create stairs down
        public DungeonMap CreateFinalLevel(SchedulingSystem schedule)
        {
            map.Initialize(width, height);
            Rectangle playerSpawn = new Rectangle(10, height / 2 - 5, 8, 8);
            Rectangle bossSpawn = new Rectangle(width / 2, 0, width / 2 - 1, height - 2);

            CreateRoom(bossSpawn);
            CreateRoom(playerSpawn);

            map.Rooms.Add(playerSpawn);
            map.Rooms.Add(bossSpawn);

            CreateHorizontalTunnel(playerSpawn.Center.X, bossSpawn.Center.X, playerSpawn.Center.Y);

            PlacePlayer();

            var dragon = Dragon.Create(1);
            dragon.X = bossSpawn.Center.X;
            dragon.Y = bossSpawn.Center.Y;

            map.AddMonster(dragon);
            CreateStairs();
            // Setting the second stairs at 0,0 to avoid exception
            map.StairsDown.X = map.StairsDown.Y = 0;
            return map;
        }

        // Creating room on the level
        private void CreateRoom(Rectangle room)
        {
            for (int x = room.Left + 1; x < room.Right; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom; y++)
                {
                    map.SetCellProperties(x, y, true, true, false);
                }
            }
        }

        // Placing monsters on the level
        private void PlaceMonsters()
        {
            while (map.Monsters.Count == 0)
            {
                foreach (var room in map.Rooms)
                {
                    if (!map.Rooms.First().Equals(room))
                    {
                        if (Dice.Roll("1D10") < 8)
                        {
                            var numberOfMonsters = Dice.Roll("1D4");
                            for (int i = 0; i < numberOfMonsters; i++)
                            {
                                Point randomRoomLocation = map.GetRandomWalkableLocationInRoom(room);
                                if (randomRoomLocation != null)
                                {
                                    var monster = Kobold.Create(1);
                                    monster.X = randomRoomLocation.X;
                                    monster.Y = randomRoomLocation.Y;
                                    map.AddMonster(monster);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Restore(SchedulingSystem schedule)
        {
            CreateStairs();
            if (Game.mapLevel == 5)
            {
                // Setting the second stairs at 0,0 to avoid exception
                map.StairsDown.X = map.StairsDown.Y = 0;
            }
            foreach (Monster monster in map.Monsters)
            {
                schedule.Add(monster);
            }
            schedule.Add(Game.Player);
        }

        // Placing player on the level
        private void PlacePlayer()
        {
            Player player = Game.Player;
            if (player == null)
            {
                player = new Player();
            }
            player.X = map.Rooms[0].Center.X;
            player.Y = map.Rooms[0].Center.Y;
            map.AddPlayer(player);
        }

        // Creating a horizontal path from one room to another
        private void CreateHorizontalTunnel(int xStart, int xEnd, int yPosition)
        {
            for (int x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); x++)
            {
                map.SetCellProperties(x, yPosition, true, true);
            }
        }

        // Creating a vertical path from one room to another
        private void CreateVerticalTunnel(int yStart, int yEnd, int xPosition)
        {
            for (int y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); y++)
            {
                map.SetCellProperties(xPosition, y, true, true);
            }
        }

        private void CreateDoors(Rectangle room)
        {
            int xMin = room.Left;
            int xMax = room.Right;
            int yMin = room.Top;
            int yMax = room.Bottom;

            List<ICell> borderCells = map.GetCellsAlongLine(xMin, yMin, xMax, yMin).ToList();
            borderCells.AddRange(map.GetCellsAlongLine(xMin, yMin, xMin, yMax));
            borderCells.AddRange(map.GetCellsAlongLine(xMin, yMax, xMax, yMax));
            borderCells.AddRange(map.GetCellsAlongLine(xMax, yMin, xMax, yMax));

            foreach (ICell cell in borderCells)
            {
                if (IsPotentialDoor(cell))
                {
                    // A door must block field-of-view when it is closed.
                    map.SetCellProperties(cell.X, cell.Y, false, true);
                    map.Doors.Add(new Door
                    {
                        X = cell.X,
                        Y = cell.Y,
                        IsOpen = false
                    });
                }
            }
        }

        private bool IsPotentialDoor(ICell cell)
        {
            // If the cell is not walkable
            // then it is a wall and not a good place for a door
            if (!cell.IsWalkable)
            {
                return false;
            }

            // Store references to all of the neighboring cells 
            ICell right = map.GetCell(cell.X + 1, cell.Y);
            ICell left = map.GetCell(cell.X - 1, cell.Y);
            ICell top = map.GetCell(cell.X, cell.Y - 1);
            ICell bottom = map.GetCell(cell.X, cell.Y + 1);

            // Make sure there is not already a door here
            if (map.GetDoor(cell.X, cell.Y) != null ||
                map.GetDoor(right.X, right.Y) != null ||
                map.GetDoor(left.X, left.Y) != null ||
                map.GetDoor(top.X, top.Y) != null ||
                map.GetDoor(bottom.X, bottom.Y) != null)
            {
                return false;
            }

            // This is a good place for a door on the left or right side of the room
            if (right.IsWalkable && left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable)
            {
                return true;
            }

            // This is a good place for a door on the top or bottom of the room
            if (!right.IsWalkable && !left.IsWalkable && top.IsWalkable && bottom.IsWalkable)
            {
                return true;
            }
            return false;
        }

        private void CreateStairs()
        {
            map.StairsUp = new Stairs
            {
                X = map.Rooms.First().Center.X + 1,
                Y = map.Rooms.First().Center.Y,
                IsUp = true
            };
            map.StairsDown = new Stairs
            {
                X = map.Rooms.Last().Center.X,
                Y = map.Rooms.Last().Center.Y,
                IsUp = false
            };
        }
    }
}
