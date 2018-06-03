using System;
using RLNET;
using MagiCave.Behaviours;

namespace MagiCave.Core
{
    public class Monster : Actor
    {
        public int? TurnsAlerted { get; set; }
        public virtual void PerformAction()
        {
            if (AreaControlled.Count == 1)
            {
                var behaviour = new StandardMoveAndAttack();
                behaviour.Act(this);
            }
            else
            {
                var behaviour = new BigActorMoveAndAttack();
                behaviour.Act(this);
            }
        }
        public void DrawStats(RLConsole statConsole, int position)
        {
            int yPos = 13 + (position * 2);
            statConsole.Print(1, yPos, Symbol.ToString(), Color);
            int width = Convert.ToInt32(((double) Health/(double) MaxHealth) * 16.0);
            int remainWidth = 16 - width;
            statConsole.SetBackColor(3 , yPos, width, 1, Swatch.Primary);
            statConsole.SetBackColor(3 + width, yPos, remainWidth , 1, Swatch.PrimaryDarkest);
            statConsole.Print(2, yPos, $": {Name}", Swatch.DbLight);
        }
    }
}
