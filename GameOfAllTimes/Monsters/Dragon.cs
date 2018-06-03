using System.Collections.Generic;
using RogueSharp;
using MagiCave.Core;
using RogueSharp.DiceNotation;

namespace MagiCave.Monsters
{
    class Dragon : Monster
    {
        public Dragon(Monster monster)
        {
            AreaControlled = monster.AreaControlled;
            Attack = monster.Attack;
            AttackChance = monster.AttackChance;
            Awareness = monster.Awareness;
            Color = monster.Color;
            Defense = monster.Defense;
            DefenseChance = monster.Defense;
            Gold = monster.Gold;
            Health = monster.Health;
            MaxHealth = monster.MaxHealth;
            Name = monster.Name;
            Speed = monster.Speed;
            Symbol = monster.Symbol;
            Size = monster.Size;
            X = monster.X;
            Y = monster.Y;
        }
        public Dragon()
        {

        }

        public static Dragon Create(int level)
        {
            int health = Dice.Roll("4D10");
            return new Dragon
            {
                Items = new List<Interfaces.IItem>(),
                AreaControlled = new List<ICell>(),
                Attack = Dice.Roll("1D4") + level / 3,
                AttackChance = Dice.Roll("30D3"),
                Awareness = 15,
                Color = Colors.DragonColor,
                Defense = Dice.Roll("1D4") + level / 3,
                DefenseChance = Dice.Roll("15D4"),
                Gold = Dice.Roll("50D20"),
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
