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

using EvolutionCore;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Evolution
{
    /// <summary>
    /// Interaction logic for CompanySplashScreen.xaml
    /// </summary>
    public partial class CompanySplashScreen : Window
    {
        public CompanySplashScreen()
        {
            InitializeComponent();

            Sound.SoundManager.PlayTheme();

            MemoryStream companyLogoStream = new MemoryStream(AptusEvolution.Properties.Resources.CompanyLogo);
            MyImage.Source = BitmapFrame.Create(companyLogoStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            Task.Run(async () =>
            {
                await Sleep(3); //allow time to show company logo
                
                this.Dispatcher.Invoke(() =>
                {
                    MemoryStream gameLogoStream = new MemoryStream(AptusEvolution.Properties.Resources.GameLogo);
                    MyImage.Source = BitmapFrame.Create(gameLogoStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    MyImage.Stretch = Stretch.Fill;
                });
                
                await Sleep(3); //allow time to show game logo

                //Take to game screen
                this.Dispatcher.Invoke(() =>
                {
                    MainWindow evolutionWindow = new MainWindow();
                    evolutionWindow.Show();
                    this.Close();
                });
            });
        }
        private async Task Sleep(int seconds)
        {
            await Task.Delay(seconds * 1000);
        }
    }
}
