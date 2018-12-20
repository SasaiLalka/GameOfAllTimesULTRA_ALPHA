using MagiCave.Core;
using RogueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp.DiceNotation;
using MagiCave.Monsters;

namespace MagiCave.Systems
{
    class DungeonGenerator
    {
        #region properties
        
        public int Neighbours { get; set; }
        public int CloseCellProb { get; set; } //55 tends to produce 1 cave, 40 few and small caves
        public int Iterations { get; set; }


        
        public int LowerLimit { get; set; }
        public int UpperLimit { get; set; }
        public int EmptyNeighbours { get; set; }
        public int EmptyCellNeighbours { get; set; }


        //corridor properties
        public int Corridor_Min { get; set; }
        public int Corridor_Max { get; set; }
        public int Corridor_MaxTurns { get; set; }
        public int CorridorSpace { get; set; }
        public int BreakOut { get; set; }
        
        public int CaveNumber { get { return Caves == null ? 0 : Caves.Count; } }


        #endregion

        #region map structures

        /// <summary>
        /// Caves within the map are stored here
        /// </summary>
        private List<List<Point>> Caves;

        /// <summary>
        /// Corridors within the map stored here
        /// </summary>
        private List<Point> Corridors;

        /// <summary>
        /// Contains the map
        /// </summary>
        public int[,] Map;

        #endregion

        #region lookups

        /// <summary>
        /// Generic list of points which contain 4 directions
        /// </summary>
        List<Point> Directions = new List<Point>()
        {
            new Point (0,-1)    //north
            , new Point(0,1)    //south
            , new Point (1,0)   //east
            , new Point (-1,0)  //west
        };

        List<Point> Directions1 = new List<Point>()
        {
            new Point (0,-1)    //north
            , new Point(0,1)    //south
            , new Point (1,0)   //east
            , new Point (-1,0)  //west
            , new Point (1,-1)  //northeast
            , new Point(-1,-1)  //northwest
            , new Point (-1,1)  //southwest
            , new Point (1,1)   //southeast
            , new Point(0,0)    //centre
        };

        #endregion


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

            Neighbours = 4;
            Iterations = 50000;
            CloseCellProb = 45;

            LowerLimit = 16;
            UpperLimit = 500;
            EmptyNeighbours = 3;
            EmptyCellNeighbours = 4;

            CorridorSpace = 2;
            Corridor_MaxTurns = 10;
            Corridor_Min = 2;
            Corridor_Max = 5;

            BreakOut = 100000;
            map = new DungeonMap();
        }
        #region cave related

        #region make caves

        /// <summary>
        /// Calling this method will build caves, smooth them off and fill in any holes
        /// </summary>
        private void BuildCaves()
        {

            Map = new int[width, height];


            //go through each map cell and randomly determine whether to close it
            //the +5 offsets are to leave an empty border round the edge of the map
            for (int x = 1; x < width - 1; x++)
                for (int y = 1; y < height - 1; y++)
                    if (Game.Random.Next(0, 100) < CloseCellProb)
                        Map[x, y] = 1;

            Point cell;

            //Pick cells at random
            for (int x = 0; x <= Iterations; x++)
            {
                cell = new Point(Game.Random.Next(1, width - 1), Game.Random.Next(1, height - 1));

                //if the randomly selected cell has more closed neighbours than the property Neighbours
                //set it closed, else open it
                if (Neighbours_Get1(cell).Where(n => Point_Get(n) == 1).Count() > Neighbours)
                    Point_Set(cell, 1);
                else
                    Point_Set(cell, 0);
            }



            //
            //  Smooth of the rough cave edges and any single blocks by making several 
            //  passes on the map and removing any cells with 3 or more empty neighbours
            //
            for (int ctr = 0; ctr < 5; ctr++)
            {
                //examine each cell individually
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        cell = new Point(x, y);

                        if (
                                Point_Get(cell) > 0
                                && Neighbours_Get(cell).Where(n => Point_Get(n) == 0).Count() >= EmptyNeighbours
                            )
                            Point_Set(cell, 0);
                    }
            }

            //
            //  fill in any empty cells that have 4 full neighbours
            //  to get rid of any holes in an cave
            //
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    cell = new Point(x, y);

                    if (
                            Point_Get(cell) == 0
                            && Neighbours_Get(cell).Where(n => Point_Get(n) == 1).Count() >= EmptyCellNeighbours
                        )
                        Point_Set(cell, 1);
                }
        }

        #endregion

        #region locate caves
        /// <summary>
        /// Locate the edge of the specified cave
        /// </summary>
        /// <param name="pCaveNumber">Cave to examine</param>
        /// <param name="pCavePoint">Point on the edge of the cave</param>
        /// <param name="pDirection">Direction to start formting the tunnel</param>
        /// <returns>Boolean indicating if an edge was found</returns>
        private void Cave_GetEdge(List<Point> pCave, ref Point pCavePoint, ref Point pDirection)
        {
            do
            {

                //random point in cave
                pCavePoint = pCave.ToList()[Game.Random.Next(0, pCave.Count() - 1)];

                pDirection = Direction_Get(pDirection);

                do
                {
                    pCavePoint.X += pDirection.X;
                    pCavePoint.Y += pDirection.Y;

                    if (!Point_Check(pCavePoint))
                        break;
                    else if (Point_Get(pCavePoint) == 0)
                        return;

                } while (true);



            } while (true);
        }

        /// <summary>
        /// Locate all the caves within the map and place each one into the generic list Caves
        /// </summary>
        private void GetCaves()
        {
            Caves = new List<List<Point>>();

            List<Point> Cave;
            Point cell;

            //examine each cell in the map...
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    cell = new Point(x, y);
                    //if the cell is closed, and that cell doesn't occur in the list of caves..
                    if (Point_Get(cell) > 0 && Caves.Count(s => s.Contains(cell)) == 0)
                    {
                        Cave = new List<Point>();

                        //launch the recursive
                        LocateCave(cell, Cave);

                        //check that cave falls with the specified property range size...
                        if (Cave.Count() <= LowerLimit | Cave.Count() > UpperLimit)
                        {
                            //it does, so bin it
                            foreach (Point p in Cave)
                                Point_Set(p, 0);
                        }
                        else
                            Caves.Add(Cave);
                    }
                }

        }

        /// <summary>
        /// Recursive method to locate the cells comprising a cave, 
        /// based on flood fill algorithm
        /// </summary>
        /// <param name="cell">Cell being examined</param>
        /// <param name="current">List containing all the cells in the cave</param>
        private void LocateCave(Point pCell, List<Point> pCave)
        {
            foreach (Point p in Neighbours_Get(pCell).Where(n => Point_Get(n) > 0))
            {
                if (!pCave.Contains(p))
                {
                    pCave.Add(p);
                    LocateCave(p, pCave);
                }
            }
        }

        #endregion

        #region connect caves

        /// <summary>
        /// Attempt to connect the caves together
        /// </summary>
        public bool ConnectCaves()
        {


            if (Caves.Count() == 0)
                return false;



            List<Point> currentcave;
            List<List<Point>> ConnectedCaves = new List<List<Point>>();
            Point cor_point = new Point();
            Point cor_direction = new Point();
            List<Point> potentialcorridor = new List<Point>();
            int breakoutctr = 0;

            Corridors = new List<Point>(); //corridors built stored here

            //get started by randomly selecting a cave..
            currentcave = Caves[Game.Random.Next(0, Caves.Count() - 1)];
            ConnectedCaves.Add(currentcave);
            Caves.Remove(currentcave);



            //starting builder
            do
            {

                //no corridors are present, sp build off a cave
                if (Corridors.Count() == 0)
                {
                    currentcave = ConnectedCaves[Game.Random.Next(0, ConnectedCaves.Count() - 1)];
                    Cave_GetEdge(currentcave, ref cor_point, ref cor_direction);
                }
                else
                    //corridors are presnt, so randomly chose whether a get a start
                    //point from a corridor or cave
                    if (Game.Random.Next(0, 100) > 50)
                {
                    currentcave = ConnectedCaves[Game.Random.Next(0, ConnectedCaves.Count() - 1)];
                    Cave_GetEdge(currentcave, ref cor_point, ref cor_direction);
                }
                else
                {
                    currentcave = null;
                    Corridor_GetEdge(ref cor_point, ref cor_direction);
                }



                //using the points we've determined above attempt to build a corridor off it
                potentialcorridor = Corridor_Attempt(cor_point
                                                , cor_direction
                                                , true);


                //if not null, a solid object has been hit
                if (potentialcorridor != null)
                {

                    //examine all the caves
                    for (int ctr = 0; ctr < Caves.Count(); ctr++)
                    {

                        //check if the last point in the corridor list is in a cave
                        if (Caves[ctr].Contains(potentialcorridor.Last()))
                        {
                            if (
                                    currentcave == null //we've built of a corridor
                                    | currentcave != Caves[ctr] //or built of a room
                                )
                            {
                                //the last corridor point intrudes on the room, so remove it
                                potentialcorridor.Remove(potentialcorridor.Last());
                                //add the corridor to the corridor collection
                                Corridors.AddRange(potentialcorridor);
                                //write it to the map
                                foreach (Point p in potentialcorridor)
                                    Point_Set(p, 1);


                                //the room reached is added to the connected list...
                                ConnectedCaves.Add(Caves[ctr]);
                                //...and removed from the Caves list
                                Caves.RemoveAt(ctr);

                                break;

                            }
                        }
                    }
                }

                //breakout
                if (breakoutctr++ > BreakOut)
                    return false;

            } while (Caves.Count() > 0);

            Caves.AddRange(ConnectedCaves);
            ConnectedCaves.Clear();
            return true;
        }

        #endregion

        #endregion

        #region corridor related

        /// <summary>
        /// Randomly get a point on an existing corridor
        /// </summary>
        /// <param name="Location">Out: location of point</param>
        /// <returns>Bool indicating success</returns>
        private void Corridor_GetEdge(ref Point pLocation, ref Point pDirection)
        {
            List<Point> validdirections = new List<Point>();

            do
            {
                //the modifiers below prevent the first of last point being chosen
                pLocation = Corridors[Game.Random.Next(1, Corridors.Count - 1)];

                //attempt to locate all the empy map points around the location
                //using the directions to offset the randomly chosen point
                foreach (Point p in Directions)
                    if (Point_Check(new Point(pLocation.X + p.X, pLocation.Y + p.Y)))
                        if (Point_Get(new Point(pLocation.X + p.X, pLocation.Y + p.Y)) == 0)
                            validdirections.Add(p);


            } while (validdirections.Count == 0);

            pDirection = validdirections[Game.Random.Next(0, validdirections.Count - 1)];
            pLocation.X += pDirection.X;
            pLocation.Y += pDirection.Y;

        }

        /// <summary>
        /// Attempt to build a corridor
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pDirection"></param>
        /// <param name="pPreventBackTracking"></param>
        /// <returns></returns>
        private List<Point> Corridor_Attempt(Point pStart, Point pDirection, bool pPreventBackTracking)
        {

            List<Point> lPotentialCorridor = new List<Point>();
            lPotentialCorridor.Add(pStart);

            int corridorlength;
            Point startdirection = new Point(pDirection.X, pDirection.Y);

            int pTurns = Corridor_MaxTurns;

            while (pTurns >= 0)
            {
                pTurns--;

                corridorlength = Game.Random.Next(Corridor_Min, Corridor_Max);
                //build corridor
                while (corridorlength > 0)
                {
                    corridorlength--;

                    //make a point and offset it
                    pStart.X += pDirection.X;
                    pStart.Y += pDirection.Y;

                    if (Point_Check(pStart) && Point_Get(pStart) == 1)
                    {
                        lPotentialCorridor.Add(pStart);
                        return lPotentialCorridor;
                    }

                    if (!Point_Check(pStart))
                        return null;
                    else if (!Corridor_PointTest(pStart, pDirection))
                        return null;

                    lPotentialCorridor.Add(pStart);

                }

                if (pTurns > 1)
                    if (!pPreventBackTracking)
                        pDirection = Direction_Get(pDirection);
                    else
                        pDirection = Direction_Get(pDirection, startdirection);
            }

            return null;
        }

        private bool Corridor_PointTest(Point pPoint, Point pDirection)
        {

            //using the property corridor space, check that number of cells on
            //either side of the point are empty
            foreach (int r in Enumerable.Range(-CorridorSpace, 2 * CorridorSpace + 1).ToList())
            {
                if (pDirection.X == 0)//north or south
                {
                    if (Point_Check(new Point(pPoint.X + r, pPoint.Y)))
                        if (Point_Get(new Point(pPoint.X + r, pPoint.Y)) != 0)
                            return false;
                }
                else if (pDirection.Y == 0)//east west
                {
                    if (Point_Check(new Point(pPoint.X, pPoint.Y + r)))
                        if (Point_Get(new Point(pPoint.X, pPoint.Y + r)) != 0)
                            return false;
                }

            }

            return true;
        }

        #endregion

        #region direction related

        /// <summary>
        /// Return a list of the valid neighbouring cells of the provided point
        /// using only north, south, east and west
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private List<Point> Neighbours_Get(Point p)
        {
            return Directions.Select(d => new Point(p.X + d.X, p.Y + d.Y))
                    .Where(d => Point_Check(d)).ToList();
        }

        /// <summary>
        /// Return a list of the valid neighbouring cells of the provided point
        /// using north, south, east, ne,nw,se,sw
        private List<Point> Neighbours_Get1(Point p)
        {
            return Directions1.Select(d => new Point(p.X + d.X, p.Y + d.Y))
                    .Where(d => Point_Check(d)).ToList();
        }

        /// <summary>
        /// Get a random direction, provided it isn't equal to the opposite one provided
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Point Direction_Get(Point p)
        {
            Point newdir;
            do
            {
                newdir = Directions[Game.Random.Next(0, Directions.Count() - 1)];

            } while (newdir.X != -p.X & newdir.Y != -p.Y);

            return newdir;
        }

        /// <summary>
        /// Get a random direction, excluding the provided directions and the opposite of 
        /// the provided direction to prevent a corridor going back on it's self.
        /// 
        /// The parameter pDirExclude is the first direction chosen for a corridor, and
        /// to prevent it from being used will prevent a corridor from going back on 
        /// it'self
        /// </summary>
        /// <param name="dir">Current direction</param>
        /// <param name="pDirectionList">Direction to exclude</param>
        /// <param name="pDirExclude">Direction to exclude</param>
        /// <returns></returns>
        private Point Direction_Get(Point pDir, Point pDirExclude)
        {
            Point NewDir;
            do
            {
                NewDir = Directions[Game.Random.Next(0, Directions.Count() - 1)];
            } while (
                        Direction_Reverse(NewDir) == pDir
                         | Direction_Reverse(NewDir) == pDirExclude
                    );


            return NewDir;
        }

        private Point Direction_Reverse(Point pDir)
        {
            return new Point(-pDir.X, -pDir.Y);
        }

        #endregion

        #region cell related

        /// <summary>
        /// Check if the provided point is valid
        /// </summary>
        /// <param name="p">Point to check</param>
        /// <returns></returns>
        private bool Point_Check(Point p)
        {
            return p.X >= 0 & p.X < width & p.Y >= 0 & p.Y < height;
        }

        /// <summary>
        /// Set the map cell to the specified value
        /// </summary>
        /// <param name="p"></param>
        /// <param name="val"></param>
        private void Point_Set(Point p, int val)
        {
            Map[p.X, p.Y] = val;
        }

        /// <summary>
        /// Get the value of the provided point
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private int Point_Get(Point p)
        {
            return Map[p.X, p.Y];
        }

        #endregion

public DungeonGenerator(DungeonMap map)
        {
            this.map = map;
        }

        // Creating default level
        public DungeonMap CreateMap(SchedulingSystem schedule)
        {
            map.Initialize(width, height);
            BuildCaves();
            GetCaves();
            ConnectCaves();
            foreach (List<Point> cave in Caves)
            {
                CreateRoom(cave);
            }
            CreateTunnel(Corridors);
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
            
            List<ICell> playerRoom = new List<ICell>();
            List<ICell> bossRoom = new List<ICell>();
            for (int i = playerSpawn.X; i < playerSpawn.X + playerSpawn.Width; i++)
            {
                for (int j = playerSpawn.Y; j < playerSpawn.Y + playerSpawn.Height; j++)
                {
                    map.SetCellProperties(i, j, true, true, false);
                    playerRoom.Add(map.GetCell(i, j));
                }
            }

            for (int i = bossSpawn.X; i < bossSpawn.X + bossSpawn.Width; i++)
            {
                for (int j = bossSpawn.Y; j < bossSpawn.Y + bossSpawn.Height; j++)
                {
                    map.SetCellProperties(i, j, true, true, false);
                    bossRoom.Add(map.GetCell(i, j));
                }
            }
            
            for (int i = playerSpawn.Center.X; i < bossSpawn.Center.X; i++)
            {
                map.SetCellProperties(i, playerSpawn.Center.Y, true, true, false);
            }
            map.Rooms.Add(playerRoom);
            map.Rooms.Add(bossRoom);
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
        private void CreateRoom(List<Point> Cave)
        {
            List<ICell> cave = new List<ICell>();
            foreach (Point p in Cave)
            {
                map.SetCellProperties(p.X, p.Y, true, true, false);
                cave.Add(map.GetCell(p.X, p.Y));
            }
            map.Rooms.Add(cave);
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
            player.X = map.Rooms.First()[map.Rooms.First().Count / 2].X;
            player.Y = map.Rooms.First()[map.Rooms.First().Count / 2].Y;
            map.AddPlayer(player);
        }


        private void CreateTunnel(List<Point> tunnel)
        {
            foreach (Point p in tunnel)
            {
                map.SetCellProperties(p.X, p.Y, true, true, false);
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
                X = map.Rooms.First()[map.Rooms.First().Count / 2].X,
                Y = map.Rooms.First()[map.Rooms.First().Count / 2].Y,
                IsUp = true
            };
            map.StairsDown = new Stairs
            {
                X = map.Rooms.Last()[map.Rooms.Last().Count / 2].X,
                Y = map.Rooms.Last()[map.Rooms.Last().Count / 2].Y,
                IsUp = false
            };
        }
    }
}
