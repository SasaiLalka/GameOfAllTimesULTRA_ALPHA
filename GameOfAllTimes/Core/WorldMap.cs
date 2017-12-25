using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RogueSharp;
using RLNET;

namespace GameOfAllTimes.Core
{
    class WorldMap : Map
    {
        public readonly List<Monster> _monsters;
        public readonly List<Stairs> DungeonEntrances;

        public WorldMap()
        {
            Game.SchedulingSystem.Clear();
            _monsters = new List<Monster>();
            DungeonEntrances = new List<Stairs>();
        }

        public void Draw(RLConsole mapConsole, RLConsole statConsole)
        {
            
        }
    }
}
