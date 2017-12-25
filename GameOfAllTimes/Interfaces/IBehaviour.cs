using GameOfAllTimes.Core;
using GameOfAllTimes.Systems;

namespace GameOfAllTimes.Interfaces
{
    public interface IBehaviour
    {
        bool Act(Monster monster, CommandSystem commandSystem);
    }
}
