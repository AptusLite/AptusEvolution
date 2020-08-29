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

using Evolution.World;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace Evolution.Replicator
{
    //TODO will need a special organism to be the first 'life form' a special organism case -> single-cell'd organism.
    public class Organism
    {
        private static int s_worldWidth = ((Convert.ToInt16(ConfigurationManager.AppSettings["WorldWidth"]) - (Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]) * 2)));
        private static int s_worldHeight = ((Convert.ToInt16(ConfigurationManager.AppSettings["WorldHeight"]) - (Convert.ToInt16(ConfigurationManager.AppSettings["PetriDishBorderThickness"]) * 2)));
        private static bool s_allowMutation = Convert.ToBoolean(ConfigurationManager.AppSettings["MutationAllowed"]);

        //Minimum speed that an organism can travel
        public const short MIN_SPEED = 0;

        //Maxium speed that an organism can travel
        public const short MAX_SPEED = 3;

        //Used for Random number between 1 and 100
        private static Random s_rand = new Random(Guid.NewGuid().GetHashCode());

        //An organism is comprised of cell groups, factoring in symmetry
        private IList<CellGroup> m_cellGroups = new List<CellGroup>();

        //Chance of the organism mutating - this percentage itself can mutate (but never 0)
        private short m_mutation_change_percent = 2;

        //Unique Identifier for the organism
        private Guid m_identifier = Guid.NewGuid();

        //Speed of organism's movement, The higher, the faster.
        private int OrganismSpeed { get; set; } = 1;

        //Amount of energy the organism has
        private float m_energy = 5.0f;

        //The organisms State
        public enum State { ALIVE, DEAD, REPLICATING };

        //Organims's Parent
        public Organism Parent { get; set; }

        //Organism's Children
        private IList<Organism> m_children = new List<Organism>();

        //Has the organism been killed
        public bool HasBeenKilled { get; set; } = false;

        //The highest generation count
        public static int s_highestGenerationCount = 0; //default when the first organism is created

        private volatile int m_stepsBeforeChangeOfDirection = 20; //This only comes into affect if 'random' is used as movement.

        private volatile int m_stepsTaken = 0;

        public GeneticCode m_geneticCode = null;

        public int GenerationCount { get; set; } = 0;

        public int MutationCount { get; set; } = 0;

        private int m_force = 0;

        /// <summary>
        /// This constructor is used only for use in history to show the evolution of the organism.
        /// </summary>
        /// <param name="geneticCode"></param>
        /// <param name="generationCount"></param>
        /// <param name="organismSize"></param>
        /// <param name="mutationCount"></param>
        public Organism(string geneticCode, int generationCount, int mutationCount)
        {
            m_geneticCode = new GeneticCode(new Propulsion(Propulsion.PositionOnCell.NONE), 0, 0)
            {
                GeneticCodeStr = geneticCode
            };
            GenerationCount = generationCount;
            MutationCount = mutationCount;
            int cellCount = m_geneticCode.GeneticCodeStr.Count(c => c == 'c');
            IList<Cell> cells = new List<Cell>();
            for(int i = 0; i < cellCount; i++)
            {
                cells.Add(new Cell(new Position(0, 0), new Propulsion(Propulsion.PositionOnCell.NONE)));
            }
            CellGroups.Add(new CellGroup(cells));
        }

        /// <summary>
        /// This can create very complex multi-cellular organism inheriting from a parent
        /// </summary>
        /// <param name="parent"></param>
        public Organism(Organism parent)
        {
            //clone from parent (inherit genetic code)
            GenerationCount = parent.GenerationCount + 1;
            MutationCount = parent.MutationCount;
            if (s_highestGenerationCount < GenerationCount)
            {
                HighestGenerationCount = GenerationCount;
            }

            Energy = parent.Energy / 2; //halve energy for parent and child organism
            parent.Energy = Energy;
            parent.AddChild(this);      //Make surew we had this organism to a child of the parent.

            //Now determine contents of the child 'this' organism.
            OrganismSpeed = parent.OrganismSpeed;
            MutationChangePercent = parent.MutationChangePercent;
            m_geneticCode = new GeneticCode(parent);
            Parent = parent;

            foreach (CellGroup cg in parent.CellGroups)
            {
                IList<Cell> cells = new List<Cell>();
                foreach (Cell cell in cg.Cells)
                {
                    cells.Add(new Cell(cell));
                }
                CellGroups.Add(new CellGroup(cells));
            }

            //Check if single-cell'd organism
            Propulsion.PositionOnCell posOnCell; //only change the direction if it was not equals to 1, otherwise stay the same.
            if (Cells.First<Cell>().Propulsion.PropulsionPositionOnCell != Propulsion.PositionOnCell.NONE && Cells.First<Cell>().Propulsion.PropulsionPositionOnCell != Propulsion.PositionOnCell.RANDOM)
            {
                posOnCell = Cell.GeneratePropulsionPosOnCellToBeDifferent(Cells.First<Cell>());//since not a mutation, it should not be squiggle and not moving.
            }
            else
            {
                posOnCell = Cells.First<Cell>().Propulsion.PropulsionPositionOnCell;
            }

            foreach (Cell cell in Cells)
            {
                cell.Propulsion.PropulsionPositionOnCell = posOnCell;
            }

            //TODO Determine if mutation will occur
            if (s_allowMutation)
            {
                if (ShouldItMutate())
                {
                    MutationCount++;

                    string geneticCode = String.Empty;
                    try
                    {
                        geneticCode = m_geneticCode.GeneticCodeStr = GeneticCode.MutateGeneticCode(parent).ToString();
                    }
                    catch(Exception ex_)
                    {
                        Debug.WriteLine(ex_.Message, ex_.StackTrace);
                    }
                    
                    string[] parts = geneticCode.Split('|');
                    IList<Position> positions = TranslateGeneticCodeIntoCellPositions(geneticCode, parent);
                    foreach (CellGroup cg in CellGroups)
                    {
                        cg.Cells.Clear();
                    }
                    Propulsion.PositionOnCell newPosOnCell = (Propulsion.PositionOnCell)Enum.Parse(typeof(Propulsion.PositionOnCell), parts[3]);
                    int maxStepsBeforeDirectionChange = Convert.ToInt32(parts[4]);
                    if (maxStepsBeforeDirectionChange != -1)
                    {
                        m_stepsBeforeChangeOfDirection = maxStepsBeforeDirectionChange;
                    }

                    IDictionary<string, bool> tmpUnique = new Dictionary<string, bool>();
                    foreach (CellGroup cg in CellGroups)
                    {
                        foreach (Position position in positions)
                        {
                            if (!tmpUnique.ContainsKey(position.X + "-" + position.Y))
                            {
                                cg.Cells.Add(new Cell(position, new Propulsion(newPosOnCell)));
                                tmpUnique.Add(position.X + "-" + position.Y, true);
                            }
                        }
                    }
                }
            }
            Force = CalculateOrganismForce();
        }

        public int CalculateOrganismForce()
        {
            int force = 1;
            //foreach (CellGroup cg in CellGroups)
            //{
            //    if (cg.Cells.Count > 1) //organism bigger than 1
            //    {
            //        Position FirstCellPosition = cg.Cells.First<Cell>().Position;
            //        Position LastCellPosition = cg.Cells.Last<Cell>().Position;
            //        int yDist = FirstCellPosition.Y - LastCellPosition.Y;
            //        int xDist = FirstCellPosition.X - LastCellPosition.X;
            //        if (FirstCellPosition.X == LastCellPosition.X && (yDist == -1 || yDist == 1))
            //        {
            //            force = cg.Cells.Count;
            //        }
            //        else if (FirstCellPosition.Y == LastCellPosition.Y && (xDist == -1 || xDist == 1))
            //        {
            //            force = cg.Cells.Count;
            //        }
            //        else if ((yDist == -1 || yDist == 1) && (xDist == -1 || xDist == 1))
            //        {
            //            force = cg.Cells.Count;
            //        }
            //    }
            //}
            //force *= OrganismSpeed == 0 ? 1 : OrganismSpeed;
            
            
            force = CellGroups.First().Cells.Count;
            force *= OrganismSpeed == 0 ? 1 : OrganismSpeed;
            return force;
        }

        /// <summary>
        /// This can create basic single-cell'd organisms.
        /// </summary>
        public Organism(Position postion, Propulsion propulsion, int speed /* this can be thought of as for the entire organism as this moment */)
        {
            IList<Cell> cells = new List<Cell>();
            cells.Add(new Cell(postion, propulsion));
            m_cellGroups.Add(new CellGroup(cells));
            OrganismSpeed = speed;
            m_geneticCode = new GeneticCode(propulsion, speed, MutationChangePercent);
            Parent = null;
        }

        //Random is not thread safe, thus the requirement for this method
        private static int NextRandomNumber(int min, int max)
        {
            lock (s_rand)
            {
                return s_rand.Next(min, max);
            }
        }

        /// <summary>
        /// Determine if a mutation should occur
        /// </summary>
        /// <returns></returns>
        private bool ShouldItMutate()
        {
            //TODO SHOULD IS MUTATE
            int randomNumber = NextRandomNumber(0, 200);
            for (short i = 0; i < MutationChangePercent; i++)
            {
                if (randomNumber == NextRandomNumber(0, 200))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Collection of all the Cells that make up the organism
        /// </summary>
        public IList<Cell> Cells
        {
            get
            {
                IList<Cell> cells = new List<Cell>();
                foreach (CellGroup cg in m_cellGroups)
                {
                    foreach (Cell cell in cg.Cells)
                    {
                        cells.Add(cell);
                    }
                }
                return cells;
            }
        }

        /// <summary>
        /// Unique identifier of organism
        /// </summary>
        public Guid Identifier
        {
            get
            {
                return m_identifier;
            }
        }

        /// <summary>
        /// Organism to Move
        ///     Note - This is very basic for single-cell organisms
        ///     Note - if end of the environment, but bound opposite direction as was travelling (effectively it's hit a boundary, so take action to go in opposite direction).
        /// </summary>
        public State Move()
        {
            if (HasBeenKilled)
            {
                return State.DEAD;
            }

            m_stepsTaken++;
            bool hasBoundaryCollision = true;
            bool isRandomMovement = false;
            Propulsion.PositionOnCell val = Propulsion.GenerateRandomPropulsionPosOnCell();
            while (hasBoundaryCollision)
            {
                val = Propulsion.GenerateRandomPropulsionPosOnCell();
                
                hasBoundaryCollision = false;
                foreach (Cell cell in Cells)
                {
                    if(cell.Propulsion.PropulsionPositionOnCell != Propulsion.PositionOnCell.RANDOM)
                    {
                        val = cell.Propulsion.PropulsionPositionOnCell;
                        m_stepsTaken = 0;
                    }
                    else
                    {
                        val = cell.Propulsion.CurrentPositionOnCell;
                        isRandomMovement = true;
                    }
                    switch (val)
                    {
                        case Propulsion.PositionOnCell.LEFT:
                            {
                                cell.PrevPosition.X = cell.Position.X;
                                cell.Position.X -= OrganismSpeed;
                                if (!hasBoundaryCollision)
                                {
                                    hasBoundaryCollision = HasBoundaryCollision(cell);
                                }
                                break;
                            }
                        case Propulsion.PositionOnCell.UP:
                            {
                                cell.PrevPosition.Y = cell.Position.Y;
                                cell.Position.Y += OrganismSpeed;
                                if (!hasBoundaryCollision)
                                {
                                    hasBoundaryCollision = HasBoundaryCollision(cell);
                                }
                                break;
                            }
                        case Propulsion.PositionOnCell.RIGHT:
                            {
                                cell.PrevPosition.X = cell.Position.X;
                                cell.Position.X += OrganismSpeed;
                                if (!hasBoundaryCollision)
                                {
                                    hasBoundaryCollision = HasBoundaryCollision(cell);
                                }
                                break;
                            }
                        case Propulsion.PositionOnCell.DOWN:
                            {
                                cell.PrevPosition.Y = cell.Position.Y;
                                cell.Position.Y -= OrganismSpeed;
                                if (!hasBoundaryCollision)
                                {
                                    hasBoundaryCollision = HasBoundaryCollision(cell);
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                }
                if (hasBoundaryCollision)
                {
                    ReverseMovement(val, isRandomMovement); //You have hit the boundary, need to reverse movement
                }
                else if(isRandomMovement)//This can only apply to Random Movement Cells
                {
                    if (m_stepsTaken >= m_stepsBeforeChangeOfDirection)
                    {
                        val = Propulsion.GenerateRandomPropulsionPosOnCell();
                        while (val == Propulsion.PositionOnCell.RANDOM)
                        {
                            val = Propulsion.GenerateRandomPropulsionPosOnCell();
                        }
                        foreach(Cell cell in Cells)
                        {
                           cell.Propulsion.CurrentPositionOnCell = val;
                        }
                        m_stepsTaken = 0;
                    }
                }
            }
            Energy -= CalculateEnergyUsageForStep();
            return OrganismsState();
        }


        private State OrganismsState()
        {
            if (HasRunOutOfEnergy())
            {
                return State.DEAD;
            }
            else if (CanReplicate())
            {
                return State.REPLICATING;
            }
            else
            {
                return State.ALIVE;
            }
        }

        private float CalculateEnergyUsageForStep()
        {
            return ((0.001f * OrganismSpeed) + 0.001f) * Cells.Count; /* The second last addition is to handle the fact that some organisms are completely stationary */
        }

        private bool HasRunOutOfEnergy()
        {
            return Energy <= 0.0f ? true : false;
        }

        private bool CanReplicate()
        {
            return Energy >= CalculateEnergyRequiredForReplication();
        }

        private float CalculateEnergyRequiredForReplication()
        {
            return Cells.Count * 10;
        }

        public static bool AllowMutation
        {
            get
            {
                return s_allowMutation;
            }
            set
            {
                s_allowMutation = value;
            }
        }

        /// <summary>
        /// Determine if cell has boundary collision
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static bool HasBoundaryCollision(Cell cell)
        {
            if (cell.Position.X <= 0 || cell.Position.X >= s_worldWidth || cell.Position.Y >= s_worldHeight || cell.Position.Y <= 0)
            {
                return true;
            }
            return false;
        }

        public static bool HasBoundaryCollision(Position pos)
        {
            //Console
            if (pos.X <= 0 || pos.X >= s_worldWidth || pos.Y >= s_worldHeight || pos.Y <= 0)
            {
                return true;
            }
            return false;
        }


        public void ReverseMovement(Propulsion.PositionOnCell tmp, bool flag=false)
        {
            foreach (Cell cell in Cells)
            {
                //if (flag)
                //{
                //    tmp = cell.Propulsion.PropulsionPositionOnCell;
                //}
                switch (tmp)
                {
                    case Propulsion.PositionOnCell.LEFT:
                        {
                            if(flag)
                                cell.Propulsion.CurrentPositionOnCell = Propulsion.PositionOnCell.RIGHT;
                            else
                                cell.Propulsion.PropulsionPositionOnCell = Propulsion.PositionOnCell.RIGHT;
                            break;
                        }
                    case Propulsion.PositionOnCell.UP:
                        {
                            if (flag)
                                cell.Propulsion.CurrentPositionOnCell = Propulsion.PositionOnCell.DOWN;
                            else
                                cell.Propulsion.PropulsionPositionOnCell = Propulsion.PositionOnCell.DOWN;
                            break;
                        }
                    case Propulsion.PositionOnCell.RIGHT:
                        {
                            if (flag)
                                cell.Propulsion.CurrentPositionOnCell = Propulsion.PositionOnCell.LEFT;
                            else
                                cell.Propulsion.PropulsionPositionOnCell = Propulsion.PositionOnCell.LEFT;
                            break;
                        }
                    case Propulsion.PositionOnCell.DOWN:
                        {
                            if (flag)
                                cell.Propulsion.CurrentPositionOnCell = Propulsion.PositionOnCell.UP;
                            else
                                cell.Propulsion.PropulsionPositionOnCell = Propulsion.PositionOnCell.UP;
                            break;
                        }
                    default: //if random? how to go back (find last position before random position to go back e.g. look at val)
                        {
                            break;
                        }
                }
            }
        }

        public float Energy
        {
            get
            {
                return m_energy;
            }
            set
            {
                m_energy = value;
            }
        }

        public void Consume(Food food)
        {
            Energy += food.Use();
        }

        public int Force
        {
            get
            {
                return m_force;
            }
            set
            {
                m_force = value;
            }
        }

        private short MutationChangePercent
        {
            get
            {
                return m_mutation_change_percent;
            }
            set
            {
                m_mutation_change_percent = value;
            }
        }

        private IList<CellGroup> CellGroups
        {
            get
            {
                return m_cellGroups;
            }
            set
            {
                m_cellGroups = value;
            }
        }

        public IList<Position> GetCellPositions()
        {
            IList<Position> positions = new List<Position>(1);
            foreach (Cell cell in Cells)
            {
                positions.Add(cell.Position);
            }
            return positions;
        }

        /// <summary>
        /// Return back Generated Genetic Code for this organism
        /// </summary>
        /// <returns></returns>
        private string GenerateGeneticCode(Propulsion propulsion)
        {
            string geneticCode = "c|" + Convert.ToString(OrganismSpeed) + "|" + Convert.ToString(MutationChangePercent) + "|" + Convert.ToString(propulsion.PropulsionPositionOnCell);
            return geneticCode;
        }

        //Imporant Mutation characters
        //c = cell
        //< = to the left
        //> = to the right
        //v = below
        //^ = above
        //| equals separation between cell descriptions, and speed and mutation rate

        //Types of mutations
        //Code duplicate
        //reverse  all/some < v > ^ signs
        //snip and replace
        //rearrange in parts (switch)
        /// <summary>
        /// If parent is null, that means build the cell positions around a position that is 0,0, since there is not parent to guide position.
        /// </summary>
        /// <param name="geneticCode"></param>
        /// <param name="parent"></param>
        /// <param name="flagNoParent"></param>
        /// <returns></returns>
        public static IList<Position> TranslateGeneticCodeIntoCellPositions(string geneticCode, Organism parent)
        {
            IList<Position> newPositions = new List<Position>();

            //The below width and height are used to calculate the new position of the mutated child organism so that it is not on top of the parent.
            int startX = 0, startY = 0, minX = 0, minY = 0, widthOfParentOrganism = 0, heightOfParentOrganism = 0;

            if (parent != null)
            {
                widthOfParentOrganism = WidthOfOrganism(parent.GetCellPositions());
                minX = parent.GetCellPositions()[0].X;
                heightOfParentOrganism = HeightOfOrganism(parent.GetCellPositions());
                minY = parent.GetCellPositions()[0].Y;

                startX = minX - widthOfParentOrganism;
                startY = minY - heightOfParentOrganism;
            }

            IList<Position> prunedPositions = new List<Position>(); //All the new positions of organism, pruned if 1 or more existed outside the bounds by removing those ones.
            bool makeSureNewOrganismExistsInBoundary = true;
            while(makeSureNewOrganismExistsInBoundary)
            {
                //for each new cell, start from minx and minus width, same with height
                foreach(char character in geneticCode.Substring(0, geneticCode.IndexOf('|', 0)))
                {
                    if(character == '<')
                    {
                        startX -= 1;
                        continue;
                    }
                    else if (character == '>')
                    {
                        startX += 1;
                        continue;
                    }
                    else if (character == '^')
                    {
                        startY -= 1;
                        continue;
                    }
                    else if(character == 'v')
                    {
                        startY += 1;
                        continue;
                    }
                    newPositions.Add(new Position(startX, startY));
                }

                foreach (Position newPos in newPositions)
                {
                    if (parent != null)
                    {
                        if (!HasBoundaryCollision(newPos))
                        {
                            prunedPositions.Add(newPos); //TODO This will make the genetic code and the actual organism out of sync...(so maybe don't allow organism to be made if falls out of bounds?)
                        }
                    } 
                    else
                    {
                        prunedPositions.Add(newPos);
                    }
                }

                if(prunedPositions.Count == 0)
                {
                    startX = minX + (widthOfParentOrganism * 2); //try the other direction to get it in bounds
                    startY = minY + (heightOfParentOrganism * 2); //try the other direction to get it in bounds
                } else
                {
                    makeSureNewOrganismExistsInBoundary = false;
                }
            }
            
            return prunedPositions;
        }

        /// <summary>
        /// Return x-width of the organism
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static int WidthOfOrganism(IList<Position> positions)
        {
            OrderBy(ref positions, true, true);
            return positions.Last<Position>().X - positions.First<Position>().X + 1;
        }

        /// <summary>
        /// Return y-Height of the organism
        /// </summary>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static int HeightOfOrganism(IList<Position> positions)
        {
            OrderBy(ref positions, false, true);
            return positions.Last<Position>().Y - positions.First<Position>().Y + 1;
        }

        /// <summary>
        /// if XorY true, order by x, if false, order by y
        /// Small or largest suggest which direction to order, true is ascending, false descending
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        private static void OrderBy(ref IList<Position> positions, bool XorY, bool SmallOrLargest)
        {
            if (XorY)
            {
                if (SmallOrLargest)
                {
                    positions = positions.OrderBy(x => x.X).ToList();
                }
                else
                {
                    positions = positions.OrderBy(x => -x.X).ToList();
                }
            }
            else
            {
                if (SmallOrLargest)
                {
                    positions = positions.OrderBy(y => y.Y).ToList();
                }
                else
                {
                    positions = positions.OrderBy(y => -y.Y).ToList();
                }
            }
        }

        public static int WorldHeight
        {
            get
            {
                return s_worldHeight;
            }
            set
            {
                s_worldHeight = value;
            }
        }

        public static int WorldWidth
        {
            get
            {
                return s_worldWidth;
            }
            set
            {
                s_worldWidth = value;
            }
        }

        public void AddChild(Organism child)
        {
            m_children.Add(child);
        }

        public IList<Organism> getChildren()
        {
            return m_children;
        }

        public static int HighestGenerationCount
        {
            get
            {
                return s_highestGenerationCount;
            }
            set
            {
                s_highestGenerationCount = value;
            }
        }
    }
}