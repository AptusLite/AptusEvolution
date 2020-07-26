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
using Evolution.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Evolution
{
    /// <summary>
    /// Interaction logic for OrganismViewer.xaml
    /// </summary>
    public partial class OrganismViewer : Window
    {
        private const string FILE_TYPE_EXTENSION = "evohistory";
        private const string DELIMITER = "$";
        private const float SCALE_VALUE = 1.1f;
        private ScaleTransform canvasScaleTransform;

        public OrganismViewer()
        {
            InitializeComponent();
            OrganismViewerViewModel.SetUpdateUIThread = new Action(UpdateCanvas);
            OrganismViewerViewModel.ClearCanvasUIThread = new Action(ClearCanvasUIThread);
            OrganismViewerViewModel.SetAddToCanvasThread = new Action<double, double, ItemView.TYPE, ItemView.STATE>(this.AddCellItemView);
            if(World.Environment.LastOrganismToDie == null)
            {
                this.save.IsEnabled = false;
            }
            canvasScaleTransform = new ScaleTransform();
            this.canvas.RenderTransform = canvasScaleTransform;
            canvasScaleTransform.ScaleX *= 2;
            canvasScaleTransform.ScaleY *= 2;
            this.canvas.Width = (this.canvas.ActualWidth / 2);
            this.canvas.Height = (this.canvas.ActualHeight / 2);
            this.MouseWheel += (sender, e) =>
            {
                if (e.Delta > 0)
                {
                    canvasScaleTransform.ScaleX *= SCALE_VALUE;
                    canvasScaleTransform.ScaleY *= SCALE_VALUE;
                    this.canvas.Width = (this.canvas.ActualWidth / 2);
                    this.canvas.Height = (this.canvas.ActualHeight / 2);
                }
                else
                {
                    canvasScaleTransform.ScaleX /= SCALE_VALUE;
                    canvasScaleTransform.ScaleY /= SCALE_VALUE;
                    this.canvas.Width = (this.canvas.ActualWidth / 2);
                    this.canvas.Height = (this.canvas.ActualHeight / 2);
                }
            };
        }

        public void UpdateUiThread()
        {
            canvas.InvalidateVisual();
        }

        private void UpdateCanvas()
        {
            this.Dispatcher.Invoke(UpdateUiThread);
        }

        public void ClearCanvasUIThread()
        {
            void ClearCanvasRects()
            {
                canvas.rects.Clear();
                UpdateUiThread();
            }
            this.Dispatcher.Invoke(ClearCanvasRects);
        }

        public void UpdateAddCanvas(double left, double top, ItemView.TYPE type, ItemView.STATE state)
        {
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double diffWidth = (canvasWidth  / 2)  + left;
            double diffHeight = (canvasHeight / 2) + top;
            SolidColorBrush myBrush = new SolidColorBrush(Colors.Cyan);
            canvas.rects.Add(Guid.NewGuid(), new EvolutionCanvas.MyRect() { Brush = myBrush, Rect = new Rect(left + diffWidth, top + diffHeight, 1.0f, 1.0f) });
        }

        private void AddCellItemView(double left, double top, ItemView.TYPE type, ItemView.STATE state)
        {
            this.Dispatcher.Invoke(new Action<double, double, ItemView.TYPE, ItemView.STATE>(UpdateAddCanvas), left, top, type, state);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            IList<Organism> organisms = new List<Organism>();

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open Evolutionary History File",
                Filter = $"Evolution History File |*.{FILE_TYPE_EXTENSION}"
            };
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != String.Empty)
            {
                using (StreamReader sr = new StreamReader(openFileDialog.FileName, Encoding.UTF8))
                {
                    string line = sr.ReadLine();
                    while (line?.Length > 0)
                    {
                        string[] organismSectionDetails = line.Split(DELIMITER);
                        string geneticCode = organismSectionDetails[0];
                        int generationCount = Convert.ToInt32(organismSectionDetails[1]);
                        int mutationCount = Convert.ToInt32(organismSectionDetails[2]);
                        organisms.Add(new Organism(geneticCode + "|", generationCount, mutationCount));
                        line = sr.ReadLine();
                    }
                }
                OrganismViewerViewModel.Organisms = organisms;
                this.save.IsEnabled = false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Title = "Save Evolutionary History File",
                Filter = $"Evolution History File |*.{FILE_TYPE_EXTENSION}",
                DefaultExt = FILE_TYPE_EXTENSION,
                AddExtension = true
            };
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != String.Empty)
            {
                using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName, true, Encoding.UTF8))
                {
                    foreach (Organism organism in OrganismViewerViewModel.Organisms)
                    {
                        int generationCount = organism.GenerationCount;
                        int mutationCount = organism.MutationCount;
                        string geneticCode = organism.m_geneticCode.GeneticCodeStr;
                        geneticCode = geneticCode.Substring(0, geneticCode.IndexOf('|', 0));
                        sw.WriteLine(geneticCode + DELIMITER + generationCount + DELIMITER + mutationCount);
                    }
                    sw.Flush();
                }
            }
        }
    }
}