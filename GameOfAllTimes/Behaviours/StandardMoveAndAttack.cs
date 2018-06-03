using System.Linq;
using MagiCave.Interfaces;
using MagiCave.Core;
using MagiCave.Systems;
using RogueSharp;

namespace MagiCave.Behaviours
{
    public class StandardMoveAndAttack : IBehaviour
    {
        public bool Act(Monster monster)
        {
            DungeonMap dungeonMap = Game.DungeonMap;
            Player player = Game.Player;
            FieldOfView monsterFov = new FieldOfView(dungeonMap);
            if (!monster.TurnsAlerted.HasValue)
            {
                monsterFov.ComputeFov(monster.X, monster.Y, monster.Awareness, true);
                if (monsterFov.IsInFov(player.X, player.Y))
                {
                    Game.MessageLog.Add($"{monster.Name} is eager to fight {player.Name}");
                    monster.TurnsAlerted = 1;
                }
            }
            if (monster.TurnsAlerted.HasValue)
            {
                dungeonMap.SetIsWalkable(monster.X, monster.Y, true);
                dungeonMap.SetIsWalkable(player.X, player.Y, true);
                PathFinder pathFinder = new PathFinder(dungeonMap);
                Path path = null;
                try
                {
                    path = pathFinder.ShortestPath(dungeonMap.GetCell(monster.X, monster.Y), dungeonMap.GetCell(player.X, player.Y));
                }
                catch
                {
                    if (dungeonMap.IsInFov(monster.X, monster.Y))
                    {
                        Game.MessageLog.Add($"{monster.Name} waits for a turn");
                    }
                }
                dungeonMap.SetIsWalkable(monster.X, monster.Y, false);
                dungeonMap.SetIsWalkable(player.X, player.Y, false);
                if (path != null)
                {
                    if (path.Steps.First() != null)
                    {
                        CommandSystem.MoveMonster(monster, path.StepForward());
                    }
                    else
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
