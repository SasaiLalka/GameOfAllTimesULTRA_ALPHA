using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RogueSharp;
using GameOfAllTimes.Core;
using RogueSharp.DiceNotation;

namespace GameOfAllTimes.Monsters
{
    class Dragon : Monster
    {
        public static Dragon Create(int level)
        {
            int health = Dice.Roll("4D10");
            return new Dragon
            {
                AreaControlled = new List<Cell>(),
                Attack = Dice.Roll("1D3") + level / 3,
                AttackChance = Dice.Roll("30D3"),
                Awareness = 15,
                Color = Colors.DragonColor,
                Defense = Dice.Roll("1D3") + level / 3,
                DefenseChance = Dice.Roll("15D4"),
                Gold = Dice.Roll("50D5"),
                Health = health,
                MaxHealth = health,
                Name = "Dragon",
                Speed = 11,
                Symbol = 'D',
                Size = 3
            };
        }
    }
}
