using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameOfAllTimes.Interfaces;
using GameOfAllTimes.Core;
using GameOfAllTimes.Systems;
using RogueSharp;

namespace GameOfAllTimes.Behaviours
{
    class BigActorMoveAndAttack : IBehaviour
    {
        public bool Act(Monster monster, CommandSystem commandSystem)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;
            FieldOfView monsterFov = new FieldOfView(dungeonMap);
            if (!monster.TurnsAlerted.HasValue)
            {
                foreach (Cell cell in monster.AreaControlled.ToArray())
                {
                    monsterFov.ComputeFov(cell.X, cell.Y, monster.Awareness, true);
                    if (monsterFov.IsInFov(player.X, player.Y))
                    {
                        Game.MessageLog.Add($"{monster.Name} is eager to fight {player.Name}");
                        monster.TurnsAlerted = 1;
                        break;
                    }
                }
            }
            if (monster.TurnsAlerted.HasValue)
            {
                foreach (Cell cell in monster.AreaControlled)
                {
                    dungeonMap.SetIsWalkable(cell.X, cell.Y, true);
                }
                dungeonMap.SetIsWalkable(player.X, player.Y, true);
                PathFinder pathFinder = new PathFinder(dungeonMap);
                Path path = null;
                // Calculating the path from the central part of the monster
                try
                {
                    path = pathFinder.ShortestPath(monster.AreaControlled[4], dungeonMap.GetCell(player.X, player.Y));
                }
                catch
                {
                    if (monster.AreaControlled.Any(c => c.IsInFov))
                    {
                        Game.MessageLog.Add($"{monster.Name} waits for a turn");
                    }
                }
                foreach (Cell cell in monster.AreaControlled)
                {
                    dungeonMap.SetIsWalkable(cell.X, cell.Y, false);
                }
                dungeonMap.SetIsWalkable(player.X, player.Y, false);
                if (path != null)
                {
                    if (path.Steps.First() != null)
                    {
                        commandSystem.MoveMonster(monster, path.StepForward());
                    }
                    else
                    {
                        Game.MessageLog.Add($"{monster.Name} growls in frustration");
                    }
                    monster.TurnsAlerted++;
                    if (monster.TurnsAlerted > 90)
                    {
                        monster.TurnsAlerted = null;
                    }
                }
            }
            return true;
        }
    }
}
