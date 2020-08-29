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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Evolution.Commands;
using Evolution.Replicator;
using Evolution.ViewModels;

namespace Evolution
{
    class OrganismViewerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum STATE { STOP, PLAY };

        public int Index { get; set; }
        public Organism OriginalAncestor { get; set; }
        public static IList<Organism> Organisms { get; set; }
        private IList<string> OrganismsGeneticCodes { get; set; }

        public static Action SetUpdateUIThread { get; set; } = null;
        public static Action ClearCanvasUIThread { get; set; } = null;
        public static Action<double, double, ItemView.TYPE, ItemView.STATE> SetAddToCanvasThread { get; set; } = null;

        private static STATE s_state = STATE.STOP;

        public OrganismViewerViewModel()
        {
            s_state = STATE.STOP;
           Task.Run(() => PopulateWithData());
        }

        public void PopulateWithData()
        {
            Organisms = new List<Organism>();
            OrganismsGeneticCodes = new List<string>();

            //Find the last surviving organism
            Organism longestLastingOrganism = World.Environment.LastOrganismToDie;
            Organisms.Add(longestLastingOrganism);
            while (longestLastingOrganism?.Parent != null) //Find the longest surviving organism's ancestors
            {
                Organisms.Add(longestLastingOrganism.Parent);
                OrganismsGeneticCodes.Add(longestLastingOrganism.m_geneticCode.GeneticCodeStr);
                longestLastingOrganism = longestLastingOrganism.Parent;
            }            
            Index = Organisms.Count - 1; //The first organism was added last in the collection
            NextOrganism();
        }

        private ICommand _next = null;
        public ICommand Next
        {
            get
            {
                if (_next == null)
                {
                    _next = new RelayCommand(
                        p => this.NextOrganism(),
                        p => this.CanStep());
                }
                return _next;
            }
        }

        private ICommand _end = null;
        public ICommand End
        {
            get
            {
                if (_end == null)
                {
                    _end = new RelayCommand(
                        p => this.EndOrganism(),
                        p => this.CanStep());
                }
                return _end;
            }
        }

        private ICommand _start = null;
        public ICommand Start
        {
            get
            {
                if (_start == null)
                {
                    _start = new RelayCommand(
                        p => this.StartOrganism(),
                        p => this.CanStep());
                }
                return _start;
            }
        }

        private ICommand _prev = null;
        public ICommand Prev
        {
            get
            {
                if (_prev == null)
                {
                    _prev = new RelayCommand(
                        p => this.PrevOrganism(),
                        p => this.CanStep());
                }
                return _prev;
            }
        }

        private ICommand _play = null;
        public ICommand Play
        {
            get
            {
                if (_play == null)
                {
                    _play = new RelayCommand(
                        p => this.PlaySteps(),
                        p => this.CanPlay());
                }
                return _play;
            }
        }

        private bool CanStep()
        {
            return s_state != STATE.PLAY;
        }

        private bool CanPlay()
        {
            if (s_state != STATE.PLAY)
            {
                return true;
            }
            return false;
        }

        private void PlaySteps()
        {
            Action playAction = () =>
            {
                s_state = STATE.PLAY;
                Index = Organisms.Count - 1; //start from scratch
                bool stillMoreOrganisms = true;
                while (s_state != STATE.STOP && stillMoreOrganisms)
                {
                    stillMoreOrganisms = NextOrganism();
                    Thread.Sleep(200);
                }
                s_state = STATE.STOP;
            };
            Task.Run(() => playAction());
        }

        private int m_generationCount = 0;
        public int GenerationCount
        {
            get
            {
                return m_generationCount;
            }
            set
            {
                m_generationCount = value;
                NotifyPropertyChanged();
            }
        }
        
        private int m_organismSize = 0;
        public int OrganismSize
        {
            get
            {
                return m_organismSize;
            }
            set
            {
                m_organismSize = value;
                NotifyPropertyChanged();
            }
        }

        private int m_mutationCount = 0;
        public int MutationCount
        {
            get
            {
                return m_mutationCount;
            }
            set
            {
                m_mutationCount = value;
                NotifyPropertyChanged();
            }
        }

        private string m_GeneticCode;
        public string GeneticCode
        {
            get
            {
                return m_GeneticCode;
            }
            set
            {
                m_GeneticCode = "Genetic Code: " + value;
                NotifyPropertyChanged();
            }
        }

        private bool NextOrganism()
        {
            if (Index > 0)
            {
                Index--;
                IList<Position> positions = Organism.TranslateGeneticCodeIntoCellPositions(Organisms[Index].m_geneticCode.GeneticCodeStr, null);
                ClearCanvasUIThread.Invoke();
                foreach (Position position in positions)
                {
                    SetAddToCanvasThread.Invoke(position.X, position.Y, ItemView.TYPE.CELL, ItemView.STATE.EXISTS);
                }
                SetUpdateUIThread.Invoke();
                GenerationCount = Organisms[Index].GenerationCount;
                MutationCount = Organisms[Index].MutationCount;
                OrganismSize = Organisms[Index].Cells.Count;
                GeneticCode = Organisms[Index].m_geneticCode.GeneticCodeStr;
                return true;
            }
            return false;
        }

        private void EndOrganism()
        {
            Index = 1;
            NextOrganism();
        }

        private void StartOrganism()
        {
            Index = Organisms.Count - 2;
            PrevOrganism();
        }

        private void PrevOrganism()
        {
            if (Index < Organisms.Count - 1)
            {
                ++Index;
                IList<Position> positions = Organism.TranslateGeneticCodeIntoCellPositions(Organisms[Index].m_geneticCode.GeneticCodeStr, null);
                ClearCanvasUIThread.Invoke();
                foreach (Position position in positions)
                {
                    SetAddToCanvasThread.Invoke(position.X, position.Y, ItemView.TYPE.CELL, ItemView.STATE.EXISTS);
                }
                SetUpdateUIThread.Invoke();
                GenerationCount = Organisms[Index].GenerationCount;
                MutationCount = Organisms[Index].MutationCount;
                OrganismSize = Organisms[Index].Cells.Count;
                GeneticCode = Organisms[Index].m_geneticCode.GeneticCodeStr;
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}