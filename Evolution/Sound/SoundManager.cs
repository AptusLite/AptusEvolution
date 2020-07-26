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

using System.Media;

namespace Evolution.Sound
{
    public class SoundManager
    {
        private static readonly SoundPlayer s_player = new SoundPlayer(EvolutionCore.Properties.Resources.Theme);

        public static void PlayTheme()
        {
            s_player.PlayLooping(); 
        }

        public static void StopTheme()
        {
            s_player.Stop();
        }
    }
}