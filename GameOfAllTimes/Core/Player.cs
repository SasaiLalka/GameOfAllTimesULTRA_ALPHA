using RLNET;
using RogueSharp;
using System.Collections.Generic;
using MagiCave.Interfaces;

namespace MagiCave.Core
{
    public class Player : Actor
    {
        public Player()
        {
            Items = new List<IItem>();
            AreaControlled = new List<ICell>();
            Attack = 2;
            AttackChance = 50;
            Awareness = 8;
            Color = Colors.Player;
            Defense = 2;
            DefenseChance = 40;
            Gold = 0;
            Health = 100;
            MaxHealth = 100;
            Name = "Rogue";
            Speed = 10;
            Symbol = '@';
            Size = 1;
            Kills = 0;
        }

        public int Kills { get; set; }
        
        public void DrawStats(RLConsole statConsole)
        {
            statConsole.Print(1, 1, $"Name:    {Name}", Colors.Text);
            statConsole.Print(1, 3, $"Health:  {Health}/{MaxHealth}", Colors.Text);
            statConsole.Print(1, 5, $"Attack:  {Attack} ({AttackChance}%)", Colors.Text);
            statConsole.Print(1, 7, $"Defense: {Defense} ({DefenseChance}%)", Colors.Text);
            statConsole.Print(1, 9, $"Gold:    {Gold}", Colors.Gold);
        }
        public void DrawItems(RLConsole inventoryConsole)
        {
            inventoryConsole.Print(1, 1, "Inventory", Colors.Text);
            for (int i = 0; i < Items.Count; i++)
            {
                if (i < inventoryConsole.Height)
                {
                    inventoryConsole.Print(1, i + 2, Items[i].ToString(), Colors.Text);
                }
            }
        }
    }
}
