﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RogueSharp;

namespace GameOfAllTimes.Core
{
    class Tile : ICell
    {
        public Tile(int x, int y, bool isTransparent, bool isWalkable, bool isInFov, bool isExplored)
        {
            X = x;
            Y = y;
            IsTransparent = isTransparent;
            IsWalkable = isWalkable;
            IsInFov = isInFov;
            IsExplored = isExplored;
        }
        public Tile(int x, int y, bool isTransparent, bool isWalkable, bool isInFov)
        {
            X = x;
            Y = y;
            IsTransparent = isTransparent;
            IsWalkable = isWalkable;
            IsInFov = isInFov;
            IsExplored = false;
        }
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool IsTransparent { get; private set; }
        public bool IsWalkable { get; private set; }
        public bool IsInFov { get; private set; }
        public bool IsExplored { get; private set; }
        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool useFov)
        {
            if (useFov && !IsInFov)
            {
                return "%";
            }
            if (IsWalkable)
            {
                if (IsTransparent)
                {
                    return ".";
                }
                else
                {
                    return "s";
                }
            }
            else
            {
                if (IsTransparent)
                {
                    return "o";
                }
                else
                {
                    return "#";
                }
            }
        }
        public bool Equals(ICell other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return X == other.X && Y == other.Y && IsTransparent == other.IsTransparent && IsWalkable == other.IsWalkable && IsInFov == other.IsInFov && IsExplored == other.IsExplored;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Tile)obj);
        }
        public static bool operator ==(Tile left, Tile right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(Tile left, Tile right)
        {
            return !Equals(left, right);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ IsTransparent.GetHashCode();
                hashCode = (hashCode * 397) ^ IsWalkable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsInFov.GetHashCode();
                hashCode = (hashCode * 397) ^ IsExplored.GetHashCode();
                return hashCode;
            }
        }
    }
}
