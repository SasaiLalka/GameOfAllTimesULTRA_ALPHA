using System.Collections.Generic;
using RogueSharp;

namespace MagiCave.Interfaces
{
    public interface IActor
    {
        List<IItem> Items { get; set; }
        List<ICell> AreaControlled { get; set; }
        int Attack { get; set; }
        int AttackChance { get; set; }
        int Awareness { get; set; }
        int Defense { get; set; }
        int DefenseChance { get; set; }
        int Gold { get; set; }
        int Health { get; set; }
        int MaxHealth { get; set; }
        string Name { get; set; }
        int Speed { get; set; }
        int Size { get; set; }
    }
}
