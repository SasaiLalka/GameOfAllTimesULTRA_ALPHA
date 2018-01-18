using GameOfAllTimes.Core;
using RogueSharp;
using System;
using System.Linq;
using GameOfAllTimes.Systems;
using RogueSharp.DiceNotation;
using GameOfAllTimes.Monsters;
using System.Collections.Generic;

namespace GameOfAllTimes.Systems
{
    class DungeonGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _maxRooms;
        private readonly int _maxRoomSize;
        private readonly int _minRoomSize;

        private readonly DungeonMap _map;

        public DungeonGenerator(int width, int height, int maxRooms, int maxRoomSize, int minRoomSize, int mapLevel)
        {
            _width = width;
            _height = height;
            _maxRooms = maxRooms;
            _maxRoomSize = maxRoomSize;
            _minRoomSize = minRoomSize;
            _map = new DungeonMap();
        }

        // Creating default level
        public DungeonMap CreateMap()
        {
            _map.Initialize(_width, _height);
            // Creating rooms
            for (int r = _maxRooms; r > 0; r--)
            {
                int roomWidth = Game.Random.Next(_minRoomSize, _maxRoomSize);
                int roomHeight = Game.Random.Next(_minRoomSize, _maxRoomSize);
                int xPosition = Game.Random.Next(0, _width - roomWidth - 1);
                int yPosition = Game.Random.Next(0, _height - roomHeight - 1);
                var newRoom = new Rectangle(xPosition, yPosition, roomWidth, roomHeight);
                // If created room isn't intersected by any other room which already exists, add it to the list
                if (!_map.Rooms.Any(room => newRoom.Intersects(room)))
                {
                    _map.Rooms.Add(newRoom);
                }
            }
            // Creating tunnels between rooms
            for (int r = 1; r < _map.Rooms.Count; r++)
            {
                int previousRoomCenterX = _map.Rooms[r - 1].Center.X;
                int previousRoomCenterY = _map.Rooms[r - 1].Center.Y;
                int currentRoomCenterX = _map.Rooms[r].Center.X;
                int currentRoomCenterY = _map.Rooms[r].Center.Y;
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
            foreach (Rectangle room in _map.Rooms)
            {
                CreateRoom(room);
                CreateDoors(room);
            }
            CreateStairs();
            PlacePlayer();
            PlaceMonsters();
            return _map;
        }

        // Creating the final level of the game
        // Killing the final boss will end the game
        // So there is no need to create stairs down
        public DungeonMap CreateFinalLevel()
        {
            _map.Initialize(_width, _height);
            Rectangle playerSpawn = new Rectangle(10, _height / 2 - 5, 8, 8);
            Rectangle bossSpawn = new Rectangle(_width / 2, 0, _width / 2 - 1, _height - 2);

            CreateRoom(bossSpawn);
            CreateRoom(playerSpawn);

            _map.Rooms.Add(playerSpawn);
            _map.Rooms.Add(bossSpawn);

            CreateHorizontalTunnel(playerSpawn.Center.X, bossSpawn.Center.X, playerSpawn.Center.Y);

            PlacePlayer();

            var monster = Dragon.Create(1);
            monster.X = bossSpawn.Center.X;
            monster.Y = bossSpawn.Center.Y;

            _map.AddMonster(monster);

            CreateStairs();
            // Setting the second stairs at 0,0 to avoid exception
            _map.StairsDown.X = _map.StairsDown.Y = 0;
            return _map;
        }

        // Creating room on the level
        private void CreateRoom(Rectangle room)
        {
            for (int x = room.Left + 1; x < room.Right; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom; y++)
                {
                    _map.SetCellProperties(x, y, true, true, false);
                }
            }
        }

        // Placing monsters on the level
        private void PlaceMonsters()
        {
            foreach(var room in _map.Rooms)
            {
                if (Dice.Roll("1D10") < 8)
                {
                    var numberOfMonsters = Dice.Roll("1D4");
                    for (int i = 0; i < numberOfMonsters; i++)
                    {
                        Point randomRoomLocation = _map.GetRandomWalkableLocationInRoom(room);
                        if (randomRoomLocation != null)
                        {
                            var monster = Kobold.Create(1);
                            monster.X = randomRoomLocation.X;
                            monster.Y = randomRoomLocation.Y;
                            _map.AddMonster(monster);
                        }
                    }
                }
            }
        }

        // Placing player on the level
        private void PlacePlayer()
        {
            Player player = Game.Player;
            if (player == null)
            {
                player = new Player();
            }
            player.X = _map.Rooms[0].Center.X;
            player.Y = _map.Rooms[0].Center.Y;
            _map.AddPlayer(player);
        }

        // Creating a horizontal path from one room to another
        private void CreateHorizontalTunnel(int xStart, int xEnd, int yPosition)
        {
            for (int x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); x++)
            {
                _map.SetCellProperties(x, yPosition, true, true);
            }
        }

        // Creating a vertical path from one room to another
        private void CreateVerticalTunnel(int yStart, int yEnd, int xPosition)
        {
            for (int y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); y++)
            {
                _map.SetCellProperties(xPosition, y, true, true);
            }
        }

        private void CreateDoors(Rectangle room)
        {
            int xMin = room.Left;
            int xMax = room.Right;
            int yMin = room.Top;
            int yMax = room.Bottom;

            List<Cell> borderCells = _map.GetCellsAlongLine(xMin, yMin, xMax, yMin).ToList();
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMin, xMin, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMax, xMax, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMax, yMin, xMax, yMax));

            foreach (Cell cell in borderCells)
            {
                if (IsPotentialDoor(cell))
                {
                    // A door must block field-of-view when it is closed.
                    _map.SetCellProperties(cell.X, cell.Y, false, true);
                    _map.Doors.Add(new Door
                    {
                        X = cell.X,
                        Y = cell.Y,
                        IsOpen = false
                    });
                }
            }
        }

        private bool IsPotentialDoor(Cell cell)
        {
            // If the cell is not walkable
            // then it is a wall and not a good place for a door
            if (!cell.IsWalkable)
            {
                return false;
            }

            // Store references to all of the neighboring cells 
            Cell right = _map.GetCell(cell.X + 1, cell.Y);
            Cell left = _map.GetCell(cell.X - 1, cell.Y);
            Cell top = _map.GetCell(cell.X, cell.Y - 1);
            Cell bottom = _map.GetCell(cell.X, cell.Y + 1);

            // Make sure there is not already a door here
            if (_map.GetDoor(cell.X, cell.Y) != null ||
                _map.GetDoor(right.X, right.Y) != null ||
                _map.GetDoor(left.X, left.Y) != null ||
                _map.GetDoor(top.X, top.Y) != null ||
                _map.GetDoor(bottom.X, bottom.Y) != null)
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
            _map.StairsUp = new Stairs
            {
                X = _map.Rooms.First().Center.X + 1,
                Y = _map.Rooms.First().Center.Y,
                IsUp = true
            };
            _map.StairsDown = new Stairs
            {
                X = _map.Rooms.Last().Center.X,
                Y = _map.Rooms.Last().Center.Y,
                IsUp = false
            };
        }
    }
}
