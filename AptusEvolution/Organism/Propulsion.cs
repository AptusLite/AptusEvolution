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

using System;

namespace Evolution.Replicator
{
    public class Propulsion
    {
        public enum PositionOnCell { LEFT, UP, DOWN, RIGHT, RANDOM, NONE };

        public PositionOnCell m_position;
        public PositionOnCell m_currentPositionOnCell;

        //Used for random number generation
        private static Random s_rand = new Random(Guid.NewGuid().GetHashCode());

        public Propulsion(Propulsion propulsion)
        {
            PropulsionPositionOnCell = propulsion.PropulsionPositionOnCell;
        }

        public Propulsion(PositionOnCell position)
        {
            m_position = position;
        }

        //Random is not thread safe, thus the requirement for this method
        private static int NextRandomNumber(int min, int max)
        {
            lock (s_rand)
            {
                return s_rand.Next(min, max);
            }
        }

        public PositionOnCell PropulsionPositionOnCell
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
            }
        }

        public PositionOnCell CurrentPositionOnCell
        {
            get
            {
                return m_currentPositionOnCell;
            }
            set
            {
                m_currentPositionOnCell = value;
            }
        }

        public static PositionOnCell GenerateRandomPropulsionPosOnCell()
        {
            switch (NextRandomNumber(0, 6))
            {
                case 0: return Propulsion.PositionOnCell.DOWN;
                case 1: return Propulsion.PositionOnCell.LEFT;
                case 2: return Propulsion.PositionOnCell.RIGHT;
                case 3: return Propulsion.PositionOnCell.UP;
                case 4: return Propulsion.PositionOnCell.RANDOM;
                default: return Propulsion.PositionOnCell.NONE;
            }
        }
    }
}
