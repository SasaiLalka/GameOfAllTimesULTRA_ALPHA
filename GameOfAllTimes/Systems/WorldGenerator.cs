using GameOfAllTimes.Core;
using RogueSharp;
using System;
using System.Linq;
using GameOfAllTimes.Systems;
using RogueSharp.DiceNotation;
using GameOfAllTimes.Monsters;
using System.Collections.Generic;

namespace GameOfAllTimes.Systems
{
    class WorldGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _age;
        public WorldGenerator(int width, int height, int age)
        {
            _width = width;
            _height = height;
            _age = age;
        }

    }
}
