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

using Evolution.ViewModels;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Media;

namespace Evolution
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EnvironmentViewModel.SetUpdateUIThread = new Action(UpdateCanvas);
            EnvironmentViewModel.ClearCanvasUIThread = new Action(ClearCanvasUIThread);
            EnvironmentViewModel.SetAddToCanvasThread = new Action<double, double, ItemView.TYPE, Guid, ItemView.STATE>(this.AddCellItemView);
            EnvironmentViewModel.SetPetriDishSize = new Action<double, double>(this.UpdatePetriDish);
            EnvironmentViewModel.SetCanvas = this.canvas;
            petridish.Width = Convert.ToInt16(ConfigurationManager.AppSettings["WorldWidth"]);
            petridish.Height = Convert.ToInt16(ConfigurationManager.AppSettings["WorldHeight"]);
            petridish.BorderThickness = new Thickness(Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]));
            this.sliderFoodExpiry.Value = Convert.ToInt16(ConfigurationManager.AppSettings["FoodExpiryTimeSeconds"]);
            this.sliderFoodRate.Value = Convert.ToInt16(ConfigurationManager.AppSettings["FoodRatePerStep"]);
            this.txtFoodAmtAtStart.Text = Convert.ToString(ConfigurationManager.AppSettings["FoodAmtWhenRun"]);
            this.txtOrganismAmtAtStart.Text = Convert.ToString(ConfigurationManager.AppSettings["OrangismAmtWhenRun"]);
        }

        public void UpdateUiThread()
        {
            canvas.InvalidateVisual();
        }

        public void ClearCanvasUIThread()
        {
            void action()
            {
                canvas.rects.Clear();
                UpdateUiThread();
            }
            this.Dispatcher.Invoke(action);
        }

        public void UpdatePetriDishSize(double x, double y)
        {
            petridish.Width = x;
            petridish.Height = y;
            UpdateUiThread();
        }

        public void UpdateAddCanvas(double left, double top, ItemView.TYPE type, Guid identifier, ItemView.STATE state)
        {
            if (state == ItemView.STATE.FINISHED)
            {
                canvas.rects.Remove(identifier);
                return;
            }
            canvas.rects.TryGetValue(identifier, out EvolutionCanvas.MyRect myrect);
            if (myrect != null)
            {
                myrect.Rect.X = left;
                myrect.Rect.Y = top;
            }
            else
            {
                SolidColorBrush myBrush;
                if (type == ItemView.TYPE.FOOD)
                {
                    myBrush = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    myBrush = new SolidColorBrush(Colors.Cyan);
                }
                canvas.rects.Add(identifier, new EvolutionCanvas.MyRect() { Brush = myBrush, Rect = new Rect(left, top, 1.0f, 1.0f) });
            }
        }

        private void AddCellItemView(double left, double top, ItemView.TYPE type, Guid identifier, ItemView.STATE state)
        {
            this.Dispatcher.Invoke(new Action<double, double, ItemView.TYPE, Guid, ItemView.STATE>(UpdateAddCanvas), left, top, type, identifier, state);
        }

        private void UpdatePetriDish(double x, double y)
        {
            this.Dispatcher.Invoke(new Action<double, double>(UpdatePetriDishSize), x, y);
        }

        private void UpdateCanvas()
        {
            this.Dispatcher.Invoke(UpdateUiThread);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}