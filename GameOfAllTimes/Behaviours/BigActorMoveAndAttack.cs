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
                foreach(Cell cell in monster.AreaControlled.ToArray())
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
                Path shortestPath = null;
                foreach (Cell cell in monster.AreaControlled)
                {
                    try
                    {
                        path = pathFinder.ShortestPath(dungeonMap.GetCell(cell.X, cell.Y), dungeonMap.GetCell(player.X, player.Y));
                        if (shortestPath == null || path.Length < shortestPath.Length)
                        {
                            shortestPath = path;
                        }
                    }
                    catch
                    {

                    }
                }
                foreach (Cell cell in monster.AreaControlled)
                {
                    dungeonMap.SetIsWalkable(cell.X, cell.Y, false);
                }
                dungeonMap.SetIsWalkable(player.X, player.Y, false);
                if (shortestPath != null)
                {
                    try
                    {
                        commandSystem.MoveMonster(monster, path.Steps.First());
                    }
                    catch (NoMoreStepsException)
                    {
                        Game.MessageLog.Add($"{monster.Name} growls in frustration");
                    }
                    monster.TurnsAlerted++;
                    if (monster.TurnsAlerted > 15)
                    {
                        monster.TurnsAlerted = null;
                    }
                }
            }
            return true;
        }
    }
}
