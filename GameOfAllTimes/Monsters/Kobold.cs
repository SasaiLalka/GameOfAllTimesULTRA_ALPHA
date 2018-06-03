using System.Collections.Generic;
using MagiCave.Core;
using RogueSharp;
using RogueSharp.DiceNotation;

namespace MagiCave.Monsters
{
    class Kobold : Monster
    {
        public Kobold(Monster monster)
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

        public Kobold()
        {

        }

        public static Kobold Create(int level)
        {
            int health = Dice.Roll("2D5");
            return new Kobold
            {
                Items = new List<Interfaces.IItem>(),
                AreaControlled = new List<ICell>(),
                Attack = Dice.Roll("1D3") + level / 3,
                AttackChance = Dice.Roll("25D3"),
                Awareness = 10,
                Color = Colors.KoboldColor,
                Defense = Dice.Roll("1D3") + level / 3,
                DefenseChance = Dice.Roll("10D4"),
                Gold = Dice.Roll("5D5"),
                Health = health,
                MaxHealth = health,
                Name = "Kobold",
                Speed = 14,
                Symbol = 'k',
                Size = 1
            };
        }
    }
}
