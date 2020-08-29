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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evolution.Replicator
{
    public class CellGroup
    {
        //A cell group is a collection of Cells.
        private IList<Cell> m_cells = new List<Cell>();

        public CellGroup(IList<Cell> cells)
        {
            m_cells = cells;
        }

        public IList<Cell> Cells
        {
            get
            {
                return m_cells;
            }
            set
            {
                m_cells = value;    
            }
        }
    }
}
