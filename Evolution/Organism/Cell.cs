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
    /// <summary>
    /// All 'Cells' are the same size 1x1x1x1 'square', but a Cell can also have a propulsion system fitted on 1 of their sides, 
    /// that side referred to as LEFT, UP, DOWN, RIGHT or NONE.
    /// </summary>
    public class Cell
    {
        //Unique Identifier for the cell
        private Guid m_identifier = Guid.NewGuid();
        public Propulsion Propulsion { get; set; } = null;
        public Position PrevPosition { get; set; }
        private Position m_position;

        public Cell(Position position, Propulsion propulsion)
        {
            Position = position;
            Propulsion = propulsion;
            if(Propulsion.PropulsionPositionOnCell == Propulsion.PositionOnCell.RANDOM)
            {
                Propulsion.CurrentPositionOnCell = Propulsion.PositionOnCell.RIGHT;
            }
        }

        public Cell(Cell cell)
        {
            Position = new Position(cell.Position.X, cell.Position.Y);
            Propulsion = new Propulsion(cell.Propulsion.PropulsionPositionOnCell);
        }

        public static Propulsion.PositionOnCell GeneratePropulsionPosOnCellToBeDifferent(Cell cell)
        {
            Propulsion.PositionOnCell posOnCell = Propulsion.GenerateRandomPropulsionPosOnCell();
            while (posOnCell == cell.Propulsion.PropulsionPositionOnCell || posOnCell == Propulsion.PositionOnCell.NONE || posOnCell == Propulsion.PositionOnCell.RANDOM)
            {
                posOnCell = Propulsion.GenerateRandomPropulsionPosOnCell();
            }
            return posOnCell;
        }

        public Position Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
                PrevPosition = new Position(m_position.X, m_position.Y); // only get's set once at first run
            }
        }

        /// <summary>
        /// Unique identifier of cell
        /// </summary>
        public Guid Identifier
        {
            get
            {
                return m_identifier;
            }
        }

    }
}
