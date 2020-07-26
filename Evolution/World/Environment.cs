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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Evolution.MathGame;
using Evolution.Replicator;

namespace Evolution.World
{
    public class Environment
    {
        //Default values derived from app.config
        private static int s_worldWidth = ((Convert.ToInt16(ConfigurationManager.AppSettings["WorldWidth"])) - (Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]) * 2));
        private static int s_worldHeight = ((Convert.ToInt16(ConfigurationManager.AppSettings["WorldHeight"])) - (Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]) * 2));
        private readonly bool LIFE_FORM_COLLISION_DETECTION = Convert.ToBoolean(ConfigurationManager.AppSettings["LifeFormCollisionDetection"]);
        private static bool s_mutationAllowed = Convert.ToBoolean(ConfigurationManager.AppSettings["MutationAllowed"]);
        public static int FoodRate { get; set; } = Convert.ToInt16(ConfigurationManager.AppSettings["FoodRatePerStep"]);
        public static int DefaultOrganismAmtAtStart { get; set; } = Convert.ToInt16(ConfigurationManager.AppSettings["OrangismAmtWhenRun"]);
        public static int DefaultFoodAmtAtStart { get; set; } = Convert.ToInt16(ConfigurationManager.AppSettings["FoodAmtWhenRun"]);

        //Used for random number generation
        private static Random s_rand = new Random(Guid.NewGuid().GetHashCode());

        //organism
        private volatile IDictionary<Guid, Organism> m_organisms = new Dictionary<Guid, Organism>(1000);
        private volatile IList<Guid> m_organismRemoved = new List<Guid>();
        private volatile IDictionary<Guid, Func<Organism.State>> m_moveActions = new Dictionary<Guid, Func<Organism.State>>();
        public static Organism LastOrganismToDie {get; set;}

        //free-fall food
        private volatile ConcurrentDictionary<Guid, Food> m_foods = new ConcurrentDictionary<Guid, Food>();         //container of food
        private volatile ConcurrentDictionary<string, Guid> m_FoodsPos = new ConcurrentDictionary<string, Guid>();  //container of food position key, to food identifier
        private volatile IList<Guid> m_foodExpired = new List<Guid>();                                              //container of food expired identifiers
        public static readonly object FoodLock2 = new object();
        private volatile static bool m_AllowFoodDrop = true;
        private const int MAX_STEPS_COUNTER_BEFORE_FOOD_DROP = 100;
        private volatile int m_stepsCounterBeforeFoodDrop = 0;
        
        //Use for checkout collision between organisms
        private volatile IDictionary<string, IList<Guid>> cellPositionToOrganism = new Dictionary<string, IList<Guid>>();

        public Environment() {}

        public void ClearEverything()
        {
            Organisms.Clear();
            OrganismRemoved.Clear();
            m_moveActions.Clear();
            Foods.Clear();
            m_FoodsPos.Clear();
            FoodRemoved.Clear();
            cellPositionToOrganism.Clear();
            HighestGenerationCount = 0;
        }

        public static readonly object m_OrganismLock = new object();
        public static Object OrganismLock
        {
            get
            {
                return m_OrganismLock;
            }
        }

        public void PopulateDefaultNumbersOfOrganismsAndFoodAtStart()
        {
            for (int i = 0; i < DefaultOrganismAmtAtStart; i++)
            {
                //Populate default organisms - Obtain unique Start position not taken by another organism
                Position randomStartPosition;
                while(true)
                {
                    randomStartPosition = GenerateRandomStartPosition();
                    if(!cellPositionToOrganism.ContainsKey(Food.GenerateFoodPositionKey(randomStartPosition)))
                    {
                        break;
                    }
                }
                Organism organism = new Organism(randomStartPosition, new Propulsion(Propulsion.GenerateRandomPropulsionPosOnCell()), GenerateRandomSpeed());
                m_organisms.Add(organism.Identifier, organism);
                m_moveActions.Add(organism.Identifier, organism.Move);
                if(LIFE_FORM_COLLISION_DETECTION)
                {
                    IList<Guid> organismIdentList = new List<Guid>();
                    organismIdentList.Add(organism.Identifier);
                    cellPositionToOrganism.Add(Food.GenerateFoodPositionKey(organism.Cells[0].Position), organismIdentList);
                }
            }
            //Populate default food 
            for (int i = 0; i < DefaultFoodAmtAtStart; i++)
            {
                AddFood(new Food(GenerateRandomStartPosition(), new Action<Food>(FoodExpired)));
            }
        }

        /// <summary>
        /// Add food to FoodsPos container, and if successful (i.e. this food position has not already been taken by another food object), then add it to food container
        /// </summary>
        /// <param name="food"></param>
        /// <returns></returns>
        private bool AddFood(Food food)
        {
            bool success = false;
            if (m_FoodsPos.TryAdd(food.PosKey, food.Identifier))
            {
                success = m_foods.TryAdd(food.Identifier, food);
            }
            return success;
        }

        public void FreeFallFood(CancellationTokenSource cancellationToken)
        {
            AllowFoodDrop = true;
            void action()
            {
                Food food = new Food(GenerateRandomStartPosition(), new Action<Food>(FoodExpired));
                lock (FoodLock2)
                {
                    AddFood(food);
                }
            }
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    AllowFoodDrop = false;
                    return;
                }
                if (!m_AllowFoodDrop)
                {
                    Task.Delay(200);
                    continue;
                }
                    
                Task[] tasks = new Task[FoodRate];
                lock (FoodLock2)
                {
                    for (int foodNo = 0; foodNo < FoodRate; foodNo++)
                    {
                        tasks[foodNo] = Task.Run(() => action());
                    }
                }
                Task.WaitAll(tasks);

                //Only drop food after set amount of steps have occured (this enables fast forwarding to still drop food proportionally to steps taken
                while((m_stepsCounterBeforeFoodDrop < MAX_STEPS_COUNTER_BEFORE_FOOD_DROP) && !cancellationToken.IsCancellationRequested)
                {
                    continue;
                }
                m_stepsCounterBeforeFoodDrop = 0;
            }
        }

        /// <summary>
        /// Called when Food food has expired.
        /// </summary>
        /// <param name="food"></param>
        private void FoodExpired(Food food)
        {
            lock(FoodLock2)
            {
                m_foods.TryRemove(food.Identifier, out _);
                m_foodExpired.Add(food.Identifier);
                m_FoodsPos.TryRemove(food.PosKey, out _);
            }            
        }

        public bool AllowFoodDrop
        {
            get
            {
                return m_AllowFoodDrop;
            }
            set
            {
                m_AllowFoodDrop = value;
            }
        }

        public int AllOrganismsStepCompleted
        {
            get
            {
                return m_stepsCounterBeforeFoodDrop;
            }
            set
            {
                m_stepsCounterBeforeFoodDrop++;
            }
        }

        //Random is not thread safe, thus the requirement for this method
        private static int NextRandomNumber(int min, int max)
        {
            lock(s_rand)
            {
                return s_rand.Next(min, max);
            }
        }

        private Position GenerateRandomStartPosition()
        {
            return new Position(NextRandomNumber(5, s_worldWidth - 5), NextRandomNumber(5, s_worldHeight - 5));
        }

        private int GenerateRandomSpeed()
        {
            return NextRandomNumber(Organism.MIN_SPEED, Organism.MAX_SPEED);
        }

        private IList<Organism> Organisms
        {
            get
            {
                return m_organisms.Values.ToList<Organism>();
            }
        }

        public static bool AllowMutation
        {
            get
            {
                return s_mutationAllowed;
            }
            set
            {
                s_mutationAllowed = Organism.AllowMutation = value;
            }
        }

        public void Step()
        {
            void action(Organism organism)
            {
                Organism.State state;
                lock (OrganismLock)
                {
                    state = m_moveActions[organism.Identifier].Invoke();
                }

                if (Organism.State.DEAD == state)
                {

                    lock (FoodLock2)
                    {
                        foreach (Cell cell in organism.Cells)
                        {
                            AddFood(new Food(new Position(cell.Position.X, cell.Position.Y), new Action<Food>(FoodExpired)));
                        }
                    }
                    lock (OrganismLock)
                    {
                        foreach (Cell cell in organism.Cells)
                        {
                            m_organismRemoved.Add(cell.Identifier);
                        }
                        m_organisms.Remove(organism.Identifier);
                        m_moveActions.Remove(organism.Identifier);
                        if (OrganismCount == 0) //The last organism has died.
                        {
                            LastOrganismToDie = organism;
                        }
                    }
                }
                else if (Organism.State.REPLICATING == state)
                {
                    lock (OrganismLock)
                    {
                        Organism newOrganism = new Organism(organism);
                        m_organisms.Add(newOrganism.Identifier, newOrganism);
                        m_moveActions.Add(newOrganism.Identifier, newOrganism.Move);

                        if (LIFE_FORM_COLLISION_DETECTION)
                        {
                            foreach (Cell cell in newOrganism.Cells)
                            {
                                string cellPositionToOrganismKey = Food.GenerateFoodPositionKey(cell.Position);
                                cellPositionToOrganism.TryGetValue(cellPositionToOrganismKey, out IList<Guid> organismIdentList);
                                if (organismIdentList == null)
                                {
                                    organismIdentList = new List<Guid>
                                    {
                                        newOrganism.Identifier
                                    };
                                    cellPositionToOrganism.Add(cellPositionToOrganismKey, organismIdentList);
                                }
                                else
                                {
                                    organismIdentList.Add(newOrganism.Identifier);
                                }
                            }
                        }
                    }
                }
                else if (Organism.State.ALIVE == state)
                {
                    IList<Food> foods = GetEatenFood(organism);
                    foreach (Food food in foods)
                    {
                        organism.Consume(food);
                        FoodExpired(food);
                    }
                    lock (OrganismLock)
                    {
                        foreach (Cell cell in organism.Cells)
                        {
                            string cellPositionToOrganismKey = Food.GenerateFoodPositionKey(cell.Position);
                            cellPositionToOrganism.TryGetValue(cellPositionToOrganismKey, out IList<Guid> organismIdentList);
                            if (organismIdentList == null)
                            {
                                organismIdentList = new List<Guid>
                                {
                                    organism.Identifier
                                };
                                cellPositionToOrganism.Add(cellPositionToOrganismKey, organismIdentList);
                            }
                            else
                            {
                                organismIdentList.Add(organism.Identifier);
                            }
                        }
                    }
                }
            }

            void checkCollision(Organism organism)
            {
                // Determine if there is a collision (e.g.something was attacked, so to speak)
                foreach (Cell cell in organism.Cells)
                {
                    string cellPositionToOrganismKey = Food.GenerateFoodPositionKey(cell.Position);
                    IList<Guid> organismIdentList = new List<Guid>();
                    cellPositionToOrganism.TryGetValue(cellPositionToOrganismKey, out organismIdentList);
                    if (organismIdentList != null && organismIdentList.Count > 1)
                    {
                        lock (OrganismLock)
                        {
                            foreach (Guid guid in organismIdentList)
                            {
                                if (!guid.Equals(organism.Identifier))
                                {
                                    m_organisms.TryGetValue(guid, out Organism collidedWith);

                                    //Perhaps Don't kills 'consume' your child or parent..forget this
                                    if (collidedWith != null)
                                    {
                                        //this organism can consume the other organism -> actually, instead, it just kills the other organism, which natually gives it a chance to eat it
                                        if (organism.Force > collidedWith.Force)
                                        {
                                            collidedWith.HasBeenKilled = true;
                                        }
                                        else if (collidedWith.Force > organism.Force)
                                        {
                                            organism.HasBeenKilled = true;
                                        }
                                        //else they are equal interms of force, destroy both or leave alone? - Leave both alone
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Perform all the steps
            var tasks = new List<Task>(Organisms.Count);
            foreach (Organism organism in Organisms)
            {
                tasks.Add(Task.Run(() => action(organism)));
            }
            Task.WaitAll(tasks.ToArray());

            //Now check if collisions have occured.
            if(LIFE_FORM_COLLISION_DETECTION)
            {
                tasks = new List<Task>(Organisms.Count);
                foreach (Organism organism in Organisms)
                {
                    tasks.Add(Task.Run(() => checkCollision(organism)));
                }
                Task.WaitAll(tasks.ToArray());

                lock (OrganismLock)
                {
                    foreach(Guid guid in OrganismRemoved)
                    {
                        m_moveActions.Remove(guid);
                    }
                    cellPositionToOrganism.Clear(); //get rid of all cell positions and their associated organisms GUIDs
                }
            }
            AllOrganismsStepCompleted++;
        }

        /// <summary>
        /// Determine if the organism has eaten any food (i.e. a single cell's position of the organism has come into contact with a food's position, when position is equal)
        /// Return the collection of food that the organism has eaten 'come into contact with'
        /// </summary>
        /// <param name="org"></param>
        public IList<Food> GetEatenFood(Organism organism)
        {
            IList<Food> foods = new List<Food>(); 

            foreach(Cell cell in organism.Cells)
            {
                Food food;
                if (m_FoodsPos.TryGetValue(Food.GenerateFoodPositionKey(cell.Position), out Guid val)) //Cell's position as key may have a food guid associated
                { 
                    if(m_foods.TryGetValue(val, out food))    //Use the associated food guid
                    {
                        foods.Add(food);
                    }
                }
                else //If you cannot detect cell and food position are equal, it may be that the organism (and thus it's cells) have moved fast from prev to current position, so check between
                {
                    //IList<string> foodKeys = Food.GenerateFoodPositionKeys(cell.PrevPosition, cell.Propulsion.PropulsionPositionOnCell, Convert.ToInt32(MathSim.Distance(cell.PrevPosition, cell.Position)));
                    IList<string> foodKeys = Food.GenerateFoodPositionKeys(cell.PrevPosition, cell.Position);
                    foreach (string key in foodKeys)
                    {
                        if (m_FoodsPos.TryGetValue(key, out val))
                        {
                            if (m_foods.TryGetValue(val, out food))
                            {
                                foods.Add(food);
                            }
                        }
                    }
                }
            }
            return foods;
        }

        public IList<Cell> Cells
        {
            get
            {
                List<Cell> cells = new List<Cell>();
                foreach (Organism org in Organisms)
                {
                    cells.AddRange(org.Cells);
                }
                return cells;
            }
        }

        public IList<Food> Foods
        {
            get
            {
                return m_foods.Values.ToList<Food>();
            }
        }

        public int OrganismCount
        {
            get
            {
                return m_organisms.Count;
            }
        }

        public int FoodCount
        {
            get
            {
                return m_foods.Count;
            }
        }

        public static int FoodExpiryTime
        {
            get
            {
                return Food.FoodExpiryTime;
            }
            set
            {
                Food.FoodExpiryTime = value;
            }
        }

        public IList<Guid> FoodRemoved
        {
            get
            {
                return m_foodExpired;
            }
        }

        public static int WorldHeight
        {
            get
            {
                return s_worldHeight + (Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]) * 2);
            }
            set
            {
                Organism.WorldHeight = value;
                s_worldHeight = value;
            }
        }

        public static int WorldWidth
        {
            get
            {
                return s_worldWidth + (Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]) * 2);
            }
            set
            {
                Organism.WorldWidth = value;
                s_worldWidth = value;
            }
        }

        /// <summary>
        /// Contains collection of all cells of all organisms that have been removed
        /// </summary>
        public IList<Guid> OrganismRemoved
        {
            get
            {
                return m_organismRemoved;
            }
        }

        public int LargestOrganismSize
        {
            get
            {
                int largest = 0;
                foreach(Organism organism in m_organisms.Values.ToList())
                {
                    if(organism.Cells.Count > largest)
                    {
                        largest = organism.Cells.Count;
                    }
                }
                return largest;
            }
        }
        
        public int HighestGenerationCount
        {
            get
            {
                return Organism.HighestGenerationCount;
            }
            set
            {
                Organism.HighestGenerationCount = value;
            }
        }
    }
}