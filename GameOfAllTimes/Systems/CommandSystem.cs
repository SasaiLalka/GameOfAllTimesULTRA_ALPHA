using GameOfAllTimes.Core;
using System.Collections.Generic;
using System.Linq;
using RLNET;
using RogueSharp;
using System.Text;
using RogueSharp.DiceNotation;
using GameOfAllTimes.Behaviours;
using GameOfAllTimes.Interfaces;

namespace GameOfAllTimes.Systems
{
    public class CommandSystem
    {
        public static bool PlayerIsDead { get; set; }
        public static string KilledBy { get; set; }
        public bool IsPlayerTurn { get; set; }

        //public CommandSystem()
        //{
        //    PlayerIsDead = false;
        //}
        public bool MovePlayer(Direction direction)
        {
            int x = Game.Player.X;
            int y = Game.Player.Y;
            switch (direction)
            {
                case Direction.Up:
                    {
                        y = Game.Player.Y - 1;
                        break;
                    }
                case Direction.Down:
                    {
                        y = Game.Player.Y + 1;
                        break;
                    }
                case Direction.Left:
                    {
                        x = Game.Player.X - 1;
                        break;
                    }
                case Direction.Right:
                    {
                        x = Game.Player.X + 1;
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }
            if (Game.DungeonMap.SetActorPosition(Game.Player, x, y))
                return true;
            Monster monster = Game.DungeonMap.GetMonsterAt(x, y);
            if (monster != null)
            {
                Attack(Game.Player, monster);
                return true;
            }
            return false;
        }
        public void Attack(Actor attacker, Actor defender)
        {
            StringBuilder attackMessage = new StringBuilder();
            StringBuilder defenseMessage = new StringBuilder();
            int hits = ResolveAttack(attacker, defender, attackMessage);
            int blocks = ResolveDefense(attacker, hits, defender, defenseMessage, attackMessage);
            Game.MessageLog.Add(attackMessage.ToString());
            if (!string.IsNullOrWhiteSpace(defenseMessage.ToString()))
            {
                Game.MessageLog.Add(defenseMessage.ToString());
            }
            int damage = hits - blocks;
            ResolveDamage(attacker, defender, damage);
        }
        private static void ResolveDamage(Actor attacker, Actor defender, int damage)
        {
            if (damage > 0)
            {
                defender.Health -= damage;
                Game.MessageLog.Add($" {defender.Name} was hit for {damage} damage");
                if (defender.Health <= 0)
                {
                    ResolveDeath(attacker, defender);
                }
            }
            else
            {
                Game.MessageLog.Add($"{defender.Name} blocks all damage.");
            }
        }
        private static void ResolveDeath(Actor attacker, Actor defender)
        {
            if (defender is Player)
            {
                PlayerIsDead = true;
                KilledBy = attacker.Name;
            }
            else if (defender is Monster)
            {
                Game.Player.Gold += defender.Gold;
                Game.Player.Kills++;
                Game.DungeonMap.RemoveMonster((Monster)defender);
                Game.MessageLog.Add($"  {defender.Name} died and dropped {defender.Gold} gold");
            }
        }
        private static int ResolveAttack(Actor attacker, Actor defender, StringBuilder attackMessage)
        {
            int hits = 0;
            attackMessage.AppendFormat("{0} attacks {1} and rolls: ", attacker.Name, defender.Name);
            DiceExpression attackDice = new DiceExpression().Dice(attacker.Attack, 100);
            DiceResult attackResult = attackDice.Roll();
            foreach (TermResult termResult in attackResult.Results)
            {
                attackMessage.Append(termResult.Value + ", ");
                if (termResult.Value >= 100 - attacker.AttackChance)
                {
                    hits++;
                }
            }
            return hits;
        }
        private static int ResolveDefense(Actor attacker, int hits, Actor defender, StringBuilder defenseMessage, StringBuilder attackMessage)
        {
            int blocks = 0;
            if (hits > 0)
            {
                attackMessage.AppendFormat("scoring {0} hits", hits);
                defenseMessage.AppendFormat("{0} defends {1} and rolls: ", defender.Name, attacker.Name);
                DiceExpression defenseDice = new DiceExpression().Dice(defender.Defense, 100);
                DiceResult defenseResult = defenseDice.Roll();
                foreach (TermResult termResult in defenseResult.Results)
                {
                    defenseMessage.Append(termResult.Value + ", ");
                    if (termResult.Value >= 100 - defender.DefenseChance)
                    {
                        blocks++;
                    }
                }
                defenseMessage.AppendFormat("resulting in {0} blocks", blocks);
            }
            else
            {
                attackMessage.Append("and misses completely.");
            }
            return blocks;
        }

        public void EndPlayerTurn()
        {
            IsPlayerTurn = false;
        }
        public void ActivateMonsters()
        {
            IScheduleable scheduleable = Game.SchedulingSystem.Get();
            if (scheduleable is Player)
            {
                IsPlayerTurn = true;
                Game.SchedulingSystem.Add(Game.Player);
            }
            else
            {
                Monster monster = scheduleable as Monster;

                if (monster != null)
                {
                    monster.PerformAction(this);
                    Game.SchedulingSystem.Add(monster);
                }

                ActivateMonsters();
            }
        }

        public void MoveMonster(Monster monster, ICell cell)
        {
            if (monster.Size == 1)
            {
                // If something crossed the monster's path
                if (!Game.DungeonMap.SetActorPosition(monster, cell.X, cell.Y))
                {
                    // If it is player
                    if (Game.Player.X == cell.X && Game.Player.Y == cell.Y)
                    {
                        Attack(monster, Game.Player);
                    }
                }
            }
            else
            {
                // Clearing controlled area for calculating next path
                foreach (ICell tile in monster.AreaControlled)
                {
                    Game.DungeonMap.SetIsWalkable(tile.X, tile.Y, true);
                }
                // Defining the area, which must be controlled by monster
                List<ICell> DesiredArea = Game.DungeonMap.GetCellsInSquare(cell.X, cell.Y, 1).ToList();
                // If there are no obstacles
                if (DesiredArea.All(c => c.IsWalkable))
                {
                    monster.X = DesiredArea[0].X;
                    monster.Y = DesiredArea[0].Y;
                    foreach (ICell tile in monster.AreaControlled.ToArray())
                    {
                        monster.AreaControlled.RemoveAll(c => c.X == tile.X && c.Y == tile.Y);
                    }
                    foreach (ICell tile in DesiredArea)
                    {
                        monster.AreaControlled.Add(tile);
                    }
                }
                // If player is next to the monster
                else if (DesiredArea.Any(c => Game.Player.X == c.X && Game.Player.Y == c.Y))
                {
                    Attack(monster, Game.Player);
                }
                // Setting cells controlled by monster not walkable
                foreach (ICell tile in monster.AreaControlled)
                {
                    Game.DungeonMap.SetIsWalkable(tile.X, tile.Y, false);
                }
            }
        }
    }
}