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
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using Evolution.World;
using System.Configuration;
using System.Windows.Input;
using Evolution.Commands;
using System.Collections.Concurrent;
using ClockTimer = System.Timers;
using System.Diagnostics;
using Evolution.Sound;

namespace Evolution
{
    public class EnvironmentViewModel : INotifyPropertyChanged
    {
        public enum STATE { RESET, RUNNING, PAUSED, EXTINCT, FASTFORWARD};

        public int PetriDishBorderThickness { get; } = Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]);

        public event PropertyChangedEventHandler PropertyChanged;

        private World.Environment SimulationEnvironment { get; set; }
        private ClockTimer.Timer m_stopWatch;
        private ConcurrentDictionary<Guid, ItemView> m_itemViewMap = new ConcurrentDictionary<Guid, ItemView>();
        private IList<Guid> m_toBeRemovedOrganismsAndFoodContainer = new List<Guid>();

        public static  int RunningFPSCounter { get; set; } //This is current running caculated frames per second, esentially equivalent to steps per second
        public static EvolutionCanvas SetCanvas { get; set; } = null;
        public static Action SetUpdateUIThread { get; set; } = null;
        public static Action ClearCanvasUIThread { get; set; } = null;
        public static Action<double, double, ItemView.TYPE, Guid, ItemView.STATE> SetAddToCanvasThread { get; set; } = null;
        public static Action<double, double> SetPetriDishSize { get; set; } = null;

        private readonly object m_lockStateObject = new object();


        //constructor
        public EnvironmentViewModel() { Sound = true; }

        //Actions
        private ICommand _startSimulator = null;
        public ICommand StartSimulatorCommand
        {
            get
            {
                if (_startSimulator == null)
                {
                    _startSimulator = new RelayCommand(
                        p => this.StartRun(),
                        p => this.CanRun());
                }
                return _startSimulator;
            }
        }

        private ICommand _resetSimulator = null;
        public ICommand ResetSimulatorCommand
        {
            get
            {
                if (_resetSimulator == null)
                {
                    _resetSimulator = new RelayCommand(
                        p => this.Reset(),
                        p => this.CanReset());
                }
                return _resetSimulator;
            }
        }

        private ICommand _about = null;
        public ICommand AboutCommand
        {
            get
            {
                if (_about == null)
                {
                    _about = new RelayCommand(
                        p => this.About(),
                        p => this.ReturnTrue());
                }
                return _about;
            }
        }

        private ICommand _organismViewer = null;
        public ICommand OrganismViewerCommand
        {
            get
            {
                if (_organismViewer == null)
                {
                    _organismViewer = new RelayCommand(
                        p => this.OrganismViewer(),
                        p => this.ReturnTrue());
                }
                return _organismViewer;
            }
        }
        

        private ICommand _fastForwardSimulator = null;
        public ICommand FastForwardSimulatorCommand
        {
            get
            {
                if (_fastForwardSimulator == null)
                {
                    _fastForwardSimulator = new RelayCommand(
                        p => this.FastForward(),
                        p => this.CanFastForward());
                }
                return _fastForwardSimulator;
            }
        }

        private ICommand _pausedSimulator = null;
        public ICommand PauseSimulatorCommand
        {
            get
            {
                if (_pausedSimulator == null)
                {
                    _pausedSimulator = new RelayCommand(
                        p => this.PauseResumeRun(),
                        p => this.CanPauseResume());
                }
                return _pausedSimulator;
            }
        }

        public bool CanRun()
        {
            if(State == STATE.RESET || State == STATE.EXTINCT)
            {
                return true;
            }
            return false;
        }

        public bool CanReset()
        {
                return true;
        }

        public bool ReturnTrue()
        {
            return true;
        }

        public bool CanFastForward()
        {
            if (State == STATE.RUNNING || State == STATE.FASTFORWARD)
            {
                return true;
            }
            return false;
        }

        public bool CanPauseResume()
        {
            return true;
        }

        private void FastForward()
        {
            if (State == STATE.RUNNING)
            {
                ShowDisplay = false;
                State = STATE.FASTFORWARD;
                StopTheme();
                FastForwardContent = "Stop";
            } 
            else if(State == STATE.FASTFORWARD)
            {
                ShowDisplay = true;
                State = STATE.RUNNING;
                PlayTheme();
                FastForwardContent = "Fast Forward >>";
            }
            EnableDisableBtnsBasedOnState(State);
        }

        private void Reset()
        {
            PauseContent = "Pause";
            State = STATE.RESET;
            IsExtinct = false;
            SimulationEnvironment.AllowFoodDrop = false;
            EnableDisableBtnsBasedOnState(State);
            StopTheme();
            System.GC.Collect(); //Since reset, good opportunity to force garbage collection
        }

        private void PauseResumeRun()
        {
            if(State == STATE.RUNNING)
            {
                State = STATE.PAUSED;
                SimulationEnvironment.AllowFoodDrop = false;
                PauseContent = "Resume";
                StopTheme();
                System.GC.Collect(); //Since paused, good opportunity to force garbage collection
            } 
            else if(State == STATE.PAUSED)
            {
                PauseContent = "Pause";
                SimulationEnvironment.AllowFoodDrop = true;
                State = STATE.RUNNING;
                PlayTheme();
            }
            EnableDisableBtnsBasedOnState(State);
        }

        private void StartRun()
        {
            PauseContent = "Pause";
            State = STATE.RUNNING;
            PlayTheme();
            Task.Run(() => Run());
            EnableDisableBtnsBasedOnState(State);
        }

        private void EnableDisableBtnsBasedOnState(STATE theState)
        {
            switch (theState)
            {
                case STATE.EXTINCT:
                    {
                        //just start
                        StartEnabled = true;
                        FastForwardEnabled = PauseEnabled = ResetEnabled = false;
                        break;
                    }
                case STATE.FASTFORWARD:
                    {
                        //just fastfoward
                        FastForwardEnabled = true;
                        StartEnabled = PauseEnabled = ResetEnabled = false;
                        break;
                    }
                case STATE.PAUSED:
                    {
                        //just puase/resume and reset
                        PauseEnabled = ResetEnabled = true;
                        FastForwardEnabled = StartEnabled = false;
                        break;
                    }
                case STATE.RUNNING:
                    {
                        //everything except start
                        StartEnabled = false;
                        FastForwardEnabled = PauseEnabled = ResetEnabled = true;
                        break;
                    }
                case STATE.RESET:
                    {
                        //just start
                        StartEnabled = true;
                        FastForwardEnabled = PauseEnabled = ResetEnabled = false;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private void About()
        {
            About about = new About();
            about.Show();
        }

        private void OrganismViewer()
        {
            State = STATE.RESET;
            IsExtinct = false; //Close the popup, if it is open, as this is required is extinction occured and the user opened the OrganismViewer via the popup
            OrganismViewer organismViewer = new OrganismViewer();
            organismViewer.Show();
        }

        public async void Run()
        { 
            try
            {
                SimulationEnvironment = new World.Environment();
                SimulationEnvironment.PopulateDefaultNumbersOfOrganismsAndFoodAtStart();
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                _ = Task.Run(() => SimulationEnvironment.FreeFallFood(cancellationToken)); //Discard the await with the _ = syntax
                DateTime dt;
                SetupStopWatchForFPSCount();

                State = STATE.RUNNING;
                bool ContinueSimulation = true;
                while (ContinueSimulation)
                {
                    lock (m_lockStateObject)
                    {
                        if (State == STATE.PAUSED)
                        {
                            continue; //Stay in loop, but do no processing
                        }
                        ContinueSimulation = State == STATE.RUNNING || State == STATE.FASTFORWARD;
                    }

                    dt = DateTime.Now;              //Time start of this frame

                    SimulationEnvironment.Step();   //All Organisms perform their next step.

                    if (ShowDisplay)
                    {
                        OrganismsItemViewCreate();      //Create Organism View Data Helper
                        FoodsItemViewCreate();          //Create Food View Data Helper
                        await Task.Run(() => SendMessagesToUpdateDisplay()); //Invoke Action methods to update display with organisms and view items if we are showing the display
                    }

                    SetUpdateUIThread.Invoke();     //Impacts Food and Organism views only

                    //Set properties to be updated with value from SimulationEnvironment
                    OrganismCount = SimulationEnvironment.OrganismCount;
                    FoodCount = SimulationEnvironment.FoodCount;
                    LargestOrganismSize = SimulationEnvironment.LargestOrganismSize;
                    HighestGenerationCount = SimulationEnvironment.HighestGenerationCount;

                    if (HasExtinctionOccured()) //If extinction has occured, set to Extinct
                    {
                        SetExtinct();
                    }
                    else
                    {
                        IsExtinct = false;
                    }

                    //If showing display, must limit to no more than 60 frames per second - e.g. throttle
                    if(ShowDisplay)
                    {
                        LimitSixtyFPS(dt);
                    }

                    RunningFPSCounter++; //Increment FPS value
                }
                ResetOrExtinctionHasOccured(cancellationToken);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message, ex.StackTrace);
            }
        }

        private void SetupStopWatchForFPSCount()
        {
            m_stopWatch = new ClockTimer.Timer
            {
                Interval = 1000
            };
            m_stopWatch.Elapsed += CalculateFPSTimedEvent; // Hook up the Elapsed event for the timer; after time interval, call OnTimedEvent                 
            m_stopWatch.Enabled = true; // Start the timer
        }

        /// <summary>
        /// When an extinction or stopped 'reset' event has occured; then tidy up in order to allow for a new 'start' event to occur. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void ResetOrExtinctionHasOccured(CancellationTokenSource cancellationToken)
        {
            cancellationToken.Cancel();
            SimulationEnvironment.ClearEverything();
            m_itemViewMap.Clear();
            ClearCanvasUIThread.Invoke();
            m_stopWatch.Stop();
            SimulationEnvironment.HighestGenerationCount = 0;
            FramesPerSecond = "0";
        }

        /// <summary>
        /// Limit to 60FPS if framerate is higher.
        /// </summary>
        /// <param name="dt">Is subtracted from Now time to dtermine how fast a frame occured </param>
        private void LimitSixtyFPS(DateTime dt)
        {
            double timeRunning = DateTime.Now.Subtract(dt).TotalMilliseconds;
            if (timeRunning < 15) //should be 16.6666
            {
                Thread.Sleep(15 - (int)timeRunning);
            }
        }

        private void SetExtinct()
        {
            State = STATE.EXTINCT;
            ShowDisplay = true;
            FastForwardContent = "Fast Forward >>";
            IsExtinct = true;
            EnableDisableBtnsBasedOnState(State);
        }

        private bool m_isExtinct = false;
        public bool IsExtinct
        {
            get
            {
                return m_isExtinct;
            }
            set
            {
                if(m_isExtinct != value)
                {
                    m_isExtinct = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool HasExtinctionOccured()
        {
            return OrganismCount == 0;
        }

        private void SendMessagesToUpdateDisplay()
        {
            Parallel.ForEach(m_toBeRemovedOrganismsAndFoodContainer, guid =>
            {
                m_itemViewMap.TryRemove(guid, out ItemView iv);
                if(iv != null)
                {
                    SetAddToCanvasThread.Invoke(-1, -1, ItemView.TYPE.CELL, guid, ItemView.STATE.FINISHED);
                }
            });
            m_toBeRemovedOrganismsAndFoodContainer.Clear();
            Parallel.ForEach(m_itemViewMap.Values.ToList(), iv =>
            {
                SetAddToCanvasThread.Invoke(iv.X, iv.Y, iv.Type, iv.Identifier, iv.State);
            });
        }

        private void FoodsItemViewCreate()
        {
            lock (World.Environment.FoodLock2)
            {
                foreach (Guid foodGuid in SimulationEnvironment.FoodRemoved)
                {
                    m_toBeRemovedOrganismsAndFoodContainer.Add(foodGuid);
                }
                SimulationEnvironment.FoodRemoved.Clear();

                foreach (Food food in SimulationEnvironment.Foods)
                {
                    if (m_itemViewMap.TryGetValue(food.Identifier, out ItemView ov))
                    {
                        ov.State = ItemView.STATE.EXISTS;
                        ov.X = food.Position.X;
                        ov.Y = food.Position.Y;
                    }
                    else
                    {
                        ov = new ItemView
                        {
                            State = ItemView.STATE.CREATED,
                            Identifier = food.Identifier,
                            X = food.Position.X,
                            Y = food.Position.Y,
                            Type = ItemView.TYPE.FOOD
                        };
                        m_itemViewMap.TryAdd(food.Identifier, ov);
                    }
                }
            }
        }

        private void OrganismsItemViewCreate()
        {
            lock (World.Environment.OrganismLock)
            {
                foreach (Guid organismGuid in SimulationEnvironment.OrganismRemoved)
                {
                    m_toBeRemovedOrganismsAndFoodContainer.Add(organismGuid);
                }
                SimulationEnvironment.OrganismRemoved.Clear();

                foreach (Cell cell in SimulationEnvironment.Cells)
                {
                    if (m_itemViewMap.TryGetValue(cell.Identifier, out ItemView ov))
                    {
                        ov.State = ItemView.STATE.EXISTS;
                        ov.X = cell.Position.X;
                        ov.Y = cell.Position.Y;
                    }
                    else
                    {
                        ov = new ItemView
                        {
                            State = ItemView.STATE.CREATED,
                            Identifier = cell.Identifier,
                            X = cell.Position.X,
                            Y = cell.Position.Y,
                            Type = ItemView.TYPE.CELL
                        };
                        m_itemViewMap.TryAdd(cell.Identifier, ov);
                    }
                }
            }
        }

        private void CalculateFPSTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            FramesPerSecond = Convert.ToString(RunningFPSCounter);
        }

        private int m_organismCount = 0;
        public int OrganismCount
        {
            get
            {
                return m_organismCount; 
            }
            set
            {
                m_organismCount = value;
                NotifyPropertyChanged();
            }
        }

        private int m_foodCount = 0;
        public int FoodCount
        {
            get
            {
                return m_foodCount;
            }
            set
            {
                m_foodCount = value;
                NotifyPropertyChanged();
            }
        }

        private int m_foodRate;
        public int FoodRate
        {
            get
            {
                return m_foodRate;
            }
            set
            {
                if (m_foodRate != value)
                {
                    lock(World.Environment.FoodLock2)
                    {
                        m_foodRate = World.Environment.FoodRate = value;
                    }
                    
                }
            }
        }


        private volatile int m_defaultFoodAmtAtStart;
        public int DefaultFoodAmtAtStart
        {
            get
            {
                return m_defaultFoodAmtAtStart;
            }
            set
            {
                if(m_defaultFoodAmtAtStart != value)
                {
                    m_defaultFoodAmtAtStart = World.Environment.DefaultFoodAmtAtStart = value;
                }
            }
        }
        private volatile int s_defaultOrganismAmtAtStart;
        public int DefaultOrganismAmtAtStart
        {
            get
            {
                return s_defaultOrganismAmtAtStart;
            }
            set
            {
                if (s_defaultOrganismAmtAtStart != value)
                {
                    s_defaultOrganismAmtAtStart = World.Environment.DefaultOrganismAmtAtStart = value;
                }
            }
        }
        

        private static int m_foodExpiryTime;
        public int FoodExpiryTime
        {
            get
            {
                return m_foodExpiryTime;
            }
            set
            {
                if (m_foodExpiryTime != value)
                {
                    m_foodExpiryTime = World.Environment.FoodExpiryTime = value;
                }
            }
        }

        private volatile int m_fps = 0;
        public string FramesPerSecond
        {
            get
            {
               return "FPS - " + m_fps;
            }
            set
            {
                m_fps = Convert.ToInt32(value);
                if (ShowDisplay && m_fps > 60)
                {
                    m_fps = 60;
                }
                RunningFPSCounter = 0;
                NotifyPropertyChanged();
            }
        }

        private volatile int m_largestOrganismSize = 0;
        public int LargestOrganismSize
        {
            get
            {
                return m_largestOrganismSize;
            }
            set
            {
                m_largestOrganismSize = SimulationEnvironment.LargestOrganismSize;
                NotifyPropertyChanged();
            }
        }
        private volatile int m_highestGenerationCount = 0;
        public int HighestGenerationCount
        {
            get
            {
                return m_highestGenerationCount;
            }
            set
            {
                m_highestGenerationCount = SimulationEnvironment.HighestGenerationCount;
                NotifyPropertyChanged();
            }
        }

        private static volatile bool s_showDisplay = true;
        public static bool ShowDisplay
        {
            get
            {
                return s_showDisplay;
            }
            set
            {
                s_showDisplay = value;
            }
        }

        private static volatile bool s_allowMutation = true;
        public static bool AllowMutation
        {
            get
            {
                return s_allowMutation;
            }
            set
            {
                s_allowMutation = World.Environment.AllowMutation = value;
            }
        }

        private static volatile bool m_sound = true;
        public bool Sound
        {
            get
            {
                return m_sound;
            }
            set
            {
                if(m_sound != value)
                {
                    m_sound = value;
                    if (m_sound && State == STATE.RUNNING)
                    {
                        PlayTheme();
                    }
                    else
                    {
                        StopTheme();
                    }
                    NotifyPropertyChanged();
                }
            }
        }

        public void PlayTheme()
        {
            if(Sound)
            {
                Task.Run(() =>
                {
                    SoundManager.PlayTheme();
                });
            }
        }

        private void StopTheme()
        {
            Task.Run(() =>
            {
                SoundManager.StopTheme();
            });
        }

        private volatile STATE SimulationState = STATE.RESET;
        public STATE State
        {
            get
            {
                lock (m_lockStateObject)
                {
                    return SimulationState;
                }
            }
            set
            {
                lock (m_lockStateObject)
                {
                    SimulationState = value;
                }
                NotifyPropertyChanged();
            }
        }

        private string m_pauseContent = "Pause";
        public string PauseContent
        {
            get
            {
                return m_pauseContent;
            }
            set
            {
                m_pauseContent = value;
                NotifyPropertyChanged();
            }
        }

        private string m_fastForwardContent = "Fast Forward >>";
        public string FastForwardContent
        {
            get
            {
                return m_fastForwardContent;
            }
            set
            {
                m_fastForwardContent = value;
                NotifyPropertyChanged();
            }
        }
        
        public double WorldHeight
        {
            get
            {
                return World.Environment.WorldHeight - PetriDishBorderThickness * 2;
            }
            set
            {
                if(State == STATE.RESET || State == STATE.EXTINCT)
                {
                    World.Environment.WorldHeight = Convert.ToInt32(value);
                    SetPetriDishSize.Invoke(World.Environment.WorldWidth, World.Environment.WorldHeight);
                }
                
            }
        }

        public double WorldWidth
        {
            get
            {
                return World.Environment.WorldWidth - PetriDishBorderThickness * 2; 
            }
            set
            {
                if (State == STATE.RESET || State == STATE.EXTINCT)
                {
                    World.Environment.WorldWidth = Convert.ToInt32(value);
                    SetPetriDishSize.Invoke(World.Environment.WorldWidth, World.Environment.WorldHeight);
                }
            }
        }

        private bool m_startEnabled = true;
        public bool StartEnabled
        {
            get
            {
                return m_startEnabled;
            }
            set
            {
                if(m_startEnabled != value)
                {
                    m_startEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool m_pauseEnabled = false;
        public bool PauseEnabled
        {
            get
            {
                return m_pauseEnabled;
            }
            set
            {
                if(m_pauseEnabled != value)
                {
                    m_pauseEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool m_fastForwardEnabled = false;
        public bool FastForwardEnabled
        {
            get
            {
                return m_fastForwardEnabled;
            }
            set
            {
                if(m_fastForwardEnabled != value)
                {
                    m_fastForwardEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool m_resetEnabled = false;
        public bool ResetEnabled
        {
            get
            {
                return m_resetEnabled;
            }
            set
            {
                if(m_resetEnabled != value)
                {
                    m_resetEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}