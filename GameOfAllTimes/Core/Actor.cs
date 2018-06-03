using MagiCave.Interfaces;
using RLNET;
using RogueSharp;
using System.Collections.Generic;
using System.Linq;

namespace MagiCave.Core
{
    public class Actor : IActor, IDrawable, IScheduleable
    {
        private List<IItem> items;
        private List<ICell> areaControlled;
        private int attack;
        private int attackChance;
        private int awareness;
        private int defense;
        private int defenseChance;
        private int gold;
        private int health;
        private int maxHealth;
        private int size;
        private string name;
        private int speed;
        public int Attack
        {
            get
            {
                return attack;
            }
            set
            {
                attack = value;
            }
        }
        public int AttackChance
        {
            get
            {
                return attackChance;
            }
            set
            {
                attackChance = value;
            }
        }
        public int Awareness
        {
            get
            {
                return awareness;
            }
            set
            {
                awareness = value;
            }
        }
        public int Defense
        {
            get
            {
                return defense;
            }
            set
            {
                defense = value;
            }
        }
        public int DefenseChance
        {
            get
            {
                return defenseChance;
            }
            set
            {
                defenseChance = value;
            }
        }
        public int Gold
        {
            get
            {
                return gold;
            }
            set
            {
                gold = value;
            }
        }

        public int Health
        {
            get
            {
                return health;
            }
            set
            {
                health = value;
            }
        }

        public int MaxHealth
        {
            get
            {
                return maxHealth;
            }
            set
            {
                maxHealth = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public int Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
            }
        }
        public int Time
        {
            get
            {
                return Speed;
            }
        }
        
        public int Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }
        public List<IItem> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
            }
        }

        public List<ICell> AreaControlled
        {
            get
            {
                return areaControlled;
            }
            set
            {
                areaControlled = value;
            }
        }

        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public void Draw(RLConsole console, IMap map)
        {
            if (Size == 1)
            {
                if (!map.GetCell(X, Y).IsExplored)
                {
                    return;
                }
                if (map.IsInFov(X, Y))
                {
                    console.Set(X, Y, Color, Colors.FloorBackgroundFov, Symbol);
                }
                else
                {
                    console.Set(X, Y, Colors.Floor, Colors.FloorBackground, '.');
                }
            }
            else
            {
                if (!map.GetCell(X, Y).IsExplored)
                {
                    return;
                }
                if (AreaControlled.Any(cell => map.IsInFov(cell.X, cell.Y)))
                {
                    console.Set(X, Y, Size, Size, Color, Colors.FloorBackgroundFov, Symbol);
                }
            }
        }
    }
}
