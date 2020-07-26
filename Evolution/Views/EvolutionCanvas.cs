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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Evolution
{
    public class EvolutionCanvas : Canvas
    {
        public EvolutionCanvas(){}

        public class MyRect
        {
            public Guid Guid;
            public Rect Rect;
            public Brush Brush;
        }

        public IDictionary<Guid, MyRect> rects = new Dictionary<Guid, MyRect>();

        protected override void OnRender(DrawingContext dc)
        {
            //how often is this getting called?
            if (EnvironmentViewModel.ShowDisplay)
            {
                base.OnRender(dc);
                MyRect[] myrects = rects.Values.ToArray();
                for (int i = 0; i < myrects.Length; i++)
                {

                    dc.DrawRectangle(myrects[i].Brush, null, myrects[i].Rect);
                }
            }
        }
    }
}
