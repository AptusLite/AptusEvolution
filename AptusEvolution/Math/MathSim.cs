//   AptusEvolution - An Evolution Simulation
//   Copyright(C) 2020 - Brendan Price 
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see<https://www.gnu.org/licenses/>.

using Evolution.Replicator;
using System;

namespace Evolution.MathGame
{
    public sealed class MathSim
    {
        public static bool DoesPositionExistOnStraighLineBetween2Position(Position between, Position first, Position second)
        {
            //first--between--------second
            if (Distance(first, between) + Distance(between, second) == Distance(first, second))
            {
                return true; // between is on the line.
            }
            return false;    // between is not on the line.
        }

        public static double Distance(Position pos1, Position pos2)
        {
            return Math.Sqrt((pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Y - pos2.Y) * (pos1.Y - pos2.Y));
        }
    }
}