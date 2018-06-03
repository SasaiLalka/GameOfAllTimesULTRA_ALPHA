using System.Collections.Generic;
using RLNET;

namespace MagiCave.Systems
{
    public class MessageLog
    {
        private static readonly int maxLines = 9;
        private readonly Queue<string> lines;
        public MessageLog()
        {
            lines = new Queue<string>();
        }
        public void Add(string message)
        {
            lines.Enqueue(message);
            if (lines.Count > maxLines)
            {
                lines.Dequeue();
            }
            Game.renderRequired = true;
        }
        public void Draw(RLConsole console)
        {
            console.Clear();
            string[] lines = this.lines.ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                console.Print(1, i + 1, lines[i], RLColor.White);
            }
        }
    }
}
