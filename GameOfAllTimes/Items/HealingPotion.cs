using MagiCave.Interfaces;
using MagiCave.Core;
using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;

namespace MagiCave.Items
{
    public class HealingPotion: IItem
    {
        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public void Draw(RLConsole console, IMap map)
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
        public HealingPotion(int x, int y)
        {
            Color = Colors.HealingPotion;
            X = x;
            Y = y;
            Symbol = 'b';
        }
        
        public override string ToString()
        {
            return "Healing Potion";
        }

        public void Use(IActor actor)
        {
            int heal = Dice.Roll("2D20");
            actor.Health += heal;
            if (actor.Health > actor.MaxHealth)
            {
                actor.Health = actor.MaxHealth;
            }
            if (actor is Player)
            {
                Game.MessageLog.Add("As you drink, you feel, as your wounds heal instantly, refreshing you");
            }
            else
            {
                Game.MessageLog.Add("As "+ actor.Name+ " drinks, its fresh wounds are being quickly restored");
            }
            actor.Items.Remove(this);
        }
    }
}
