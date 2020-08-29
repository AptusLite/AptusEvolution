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
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Evolution
{
    public class GeneticCode
    {
        //Minimum speed that an organism can travel
        public const short MIN_SPEED = 0;

        //Maxium speed that an organism can travel
        public const short MAX_SPEED = 3;

        //Used for Random number
        private static readonly Random s_rand = new Random(Guid.NewGuid().GetHashCode());

        //Contains the GeneticCode 
        public string GeneticCodeStr { get; set; } = String.Empty;

        private const string LEFT = "<";
        private const string RIGHT = ">";
        private const string UP = "^";
        private const string DOWN = "v";
        private const string CELL = "c";
        private const string DELIMITER = "|";

        public GeneticCode(Propulsion propulsion, int speed, int mutationChancePercent) 
        {
            GeneticCodeStr = CELL + DELIMITER + Convert.ToString(speed) + DELIMITER + Convert.ToString(mutationChancePercent) + DELIMITER + Convert.ToString(propulsion.PropulsionPositionOnCell);
        }
        public GeneticCode(Organism parent)
        {
            GeneticCodeStr = parent.m_geneticCode.GeneticCodeStr;
        }

        //Random is not thread safe, thus the requirement for this method
        private static int NextRandomNumber(int min, int max)
        {
            lock (s_rand)
            {
                return s_rand.Next(min, max);
            }
        }

        public static string MutateGeneticCode(Organism parent)
        {
            //Start with parent's genetic code
            string mutatedGeneticCode = parent.m_geneticCode.GeneticCodeStr.Substring(0, parent.m_geneticCode.GeneticCodeStr.IndexOf('|', 0));

            bool shouldDouble = NextRandomNumber(0, 2) == 1; 

            bool shouldReverse = NextRandomNumber(0, 2) == 1; 

            bool shouldSnipAndReplace = NextRandomNumber(0, 2) == 1; 

            bool switchMiddleAndReverseOneHalf = NextRandomNumber(0, 2) == 1;

            bool shouldHalve = NextRandomNumber(0, 2) == 1; 

            //Movement PosOnCell change
            bool shouldMovementChange = NextRandomNumber(1, 3) == 2 ? true : false;
            Propulsion.PositionOnCell posOnCell;
            int stepsBeforeDirectionChange = -1; //-1 not applicable, unless has random mode.
            if (shouldMovementChange)
            {
                posOnCell = Propulsion.GenerateRandomPropulsionPosOnCell();
                if(posOnCell == Propulsion.PositionOnCell.RANDOM)
                {
                    stepsBeforeDirectionChange = NextRandomNumber(1, 500); 
                }
            }
            else
            {
                posOnCell = parent.Cells.First<Cell>().Propulsion.PropulsionPositionOnCell;
            }

            if (shouldDouble)
            {
                mutatedGeneticCode = DoubleGeneticCode(mutatedGeneticCode);
            }

            if (shouldSnipAndReplace)
            {
                mutatedGeneticCode = SnipAndReplaceGeneticCode(mutatedGeneticCode);
            }

            if (switchMiddleAndReverseOneHalf)
            {
                bool coinFlip1 = NextRandomNumber(0, 2) == 1;
                bool coinFlip2 = NextRandomNumber(0, 2) == 1;
                mutatedGeneticCode = ReverseGeneticCode(mutatedGeneticCode, coinFlip1, coinFlip2, 1, 1);
            }

            if (!shouldDouble && shouldHalve)
            {
                mutatedGeneticCode = HalveGeneticCode(mutatedGeneticCode);
            }

            if (!switchMiddleAndReverseOneHalf && shouldReverse)
            {
                bool upAndDown = NextRandomNumber(0, 2) == 1; //20% up and down replacement
                bool leftAndRight = NextRandomNumber(0, 2) == 1; //20% left and right replacement
                mutatedGeneticCode = ReverseGeneticCode(mutatedGeneticCode, true, true);
            }

            mutatedGeneticCode = CleanGeneticCode(mutatedGeneticCode);

            //speed part possibly changed here
            mutatedGeneticCode += DELIMITER + Convert.ToString(NextRandomNumber(MIN_SPEED, MAX_SPEED));

            //Mutation chance probably changed here (keep mutation chance percent between 1% and 3%)
            mutatedGeneticCode += DELIMITER + Convert.ToString(NextRandomNumber(1, 3));

            //Fourth part is the posOnCell (e.g. direction)
            mutatedGeneticCode += DELIMITER + posOnCell;

            //5th part represents when direction should change (only applicable for random mode).
            mutatedGeneticCode += DELIMITER + stepsBeforeDirectionChange;

            return mutatedGeneticCode;
        }

        /// <summary>
        /// Double the genetic code,witha random Cell direction between the doubled genetic code
        /// </summary>
        /// <param name="geneticCode"></param>
        /// <returns></returns>
        private static string DoubleGeneticCode(string geneticCode)
        {
            String mutatedCode = geneticCode + RandomCellDirection() + geneticCode;
            bool shouldReverseCoinFlip = NextRandomNumber(0, 2) == 1;
            if (shouldReverseCoinFlip)
            {
                bool upAndDown = NextRandomNumber(0, 2) == 1; 
                bool leftAndRight = NextRandomNumber(0, 2) == 1; 
                mutatedCode = ReverseGeneticCode(mutatedCode, upAndDown, leftAndRight, -1, -1);
            }

            if (mutatedCode.Length == 2)
            {
                return mutatedCode[0] + RandomCellDirection() + mutatedCode[1];
            }
            return mutatedCode;
        }

        private static string HalveGeneticCode(string geneticCode)
        {
            int length = geneticCode.Length;
            if (length == 1)
                return geneticCode;
            bool keepFirstHalf = NextRandomNumber(0, 2) == 1;
            int midWay = length / 2;
            if (geneticCode[midWay].Equals(Convert.ToChar(LEFT)) || geneticCode[midWay].Equals(Convert.ToChar(RIGHT)) || geneticCode[midWay].Equals(Convert.ToChar(DOWN)) || geneticCode[midWay].Equals(Convert.ToChar(UP)))
            {
                midWay++;
            }
            if (keepFirstHalf)
            {
                return geneticCode.Substring(0, midWay - 1);
            }
            else //keep second half
            {
                return geneticCode.Substring(midWay, length - midWay);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="geneticCode"></param>
        /// <returns></returns>
        private static string CleanGeneticCode(string geneticCode)
        {
            if (geneticCode.Length <= 3) //e.g. cvc nothing to do here, as there is no chance of opposite < > v ^ occuring as there is only one of them
                return geneticCode;

            StringBuilder cleanedGeneticCodeStrBuilder =  new StringBuilder(CELL);
            char lastCellBuildDirection = 'n'; //disregard this value, just denotes it's not applicable at this stage n/a
            for (int i = 1; i < geneticCode.Length; i += 2)
            {
                if (!IsCurrentCellBuildDirectionBuildingOverLastCellBuildDirection(geneticCode[i], lastCellBuildDirection))
                {
                    lastCellBuildDirection = geneticCode[i];
                    cleanedGeneticCodeStrBuilder.Append(geneticCode[i] + CELL);
                }
            }
            return  cleanedGeneticCodeStrBuilder.ToString();
        }

        private static string SnipAndReplaceGeneticCode(string geneticCode)
        {
            int length = geneticCode.Length;
            if (length <= 3)
                return geneticCode;

            int halfway = length / 2;
            string firstHalf = geneticCode.Substring(0, halfway);
            string secondHalf = geneticCode[halfway..^0];
            if (firstHalf[^1].Equals(Convert.ToChar(LEFT)) || firstHalf[^1].Equals(Convert.ToChar(RIGHT)) || firstHalf[^1].Equals(Convert.ToChar(DOWN)) || firstHalf[^1].Equals(Convert.ToChar(UP)))
            {
                firstHalf = firstHalf[0..^1]; //itself minus last characer to clean up this string, so it finished on a Cell
            }
            if (secondHalf[0].Equals(Convert.ToChar(LEFT)) || secondHalf[0].Equals(Convert.ToChar(RIGHT)) || secondHalf[0].Equals(Convert.ToChar(DOWN)) || secondHalf[0].Equals(Convert.ToChar(UP)))
            {
                secondHalf = secondHalf[1..^0]; //itself minus last characer to clean up this string, so it finished on a Cell
            }
            if (NextRandomNumber(0, 2) == 1) //only the first part is going to change.
            {
                string newFirstHalf = String.Empty;
                foreach (char c in firstHalf)
                {
                    string s = Convert.ToString(c);
                    if (s.Equals(LEFT) || s.Equals(RIGHT) || s.Equals(DOWN) || s.Equals(UP))
                    {
                        newFirstHalf += RandomCellDirection();
                    } 
                    else
                    {
                        newFirstHalf += CELL;
                    }
                }
                firstHalf = newFirstHalf;
            } 
            else //only the second part
            {
                string newSecondHalf = String.Empty;
                foreach (char c in secondHalf)
                {
                    string s = Convert.ToString(c);
                    if (s.Equals(LEFT) || s.Equals(RIGHT) || s.Equals(DOWN) || s.Equals(UP))
                    {
                        newSecondHalf += RandomCellDirection();
                    }
                    else
                    {
                        newSecondHalf += CELL;
                    }
                }
                secondHalf = newSecondHalf;
            }
            return firstHalf + RandomCellDirection() + secondHalf;
        }

        
        public static bool IsCurrentCellBuildDirectionBuildingOverLastCellBuildDirection(char cellCurrentBuildDirection, char cellPrevBuildDirection)
        {
            bool isOpposite = false;
            string cellCurrentBuildDirectionStr = Convert.ToString(cellCurrentBuildDirection);
            string cellPrevBuildDirectionStr = Convert.ToString(cellPrevBuildDirection);
            if (cellCurrentBuildDirectionStr == LEFT && cellPrevBuildDirectionStr == RIGHT)
            {
                isOpposite = true;
            }
            else if (cellCurrentBuildDirectionStr == RIGHT && cellPrevBuildDirectionStr == LEFT)
            {
                isOpposite = true;
            }
            else if (cellCurrentBuildDirectionStr == UP && cellPrevBuildDirectionStr == DOWN)
            {
                isOpposite = true;
            }
            else if (cellCurrentBuildDirectionStr == DOWN && cellPrevBuildDirectionStr == UP)
            {
                isOpposite = true;
            }
            return isOpposite;
        }

        /// <summary>
        /// Reverse the use of the < ^ > v, thus produces different forms.
        /// </summary>
        /// <param name="geneticCode"></param>
        /// <returns></returns>
        private static string ReverseGeneticCode(string geneticCode, bool upAndDown, bool leftAndRight, int start = -1, int finish = -1) //up and down, left and right, excetra.
        {
            if (geneticCode.Length == 1 || geneticCode.Length == 3)
                return geneticCode;

            if (!upAndDown && !leftAndRight) //if left and right AND up and down were false, set them both to true
                upAndDown = leftAndRight = true;

            string endPartOfGeneticCode;
            string firstPartOfGeneticCode;
            if (!(start == -1 && finish == -1))
            {
                bool reverseFirstHalf = NextRandomNumber(0, 2) == 1;
                int midWay = geneticCode.Length / 2;
                if (geneticCode[midWay].Equals(Convert.ToChar(LEFT)) || geneticCode[midWay].Equals(Convert.ToChar(RIGHT)) || geneticCode[midWay].Equals(Convert.ToChar(DOWN)) || geneticCode[midWay].Equals(Convert.ToChar(UP)))
                {
                    midWay++;
                }
                endPartOfGeneticCode = geneticCode.Substring(midWay, geneticCode.Length - midWay);
                firstPartOfGeneticCode = geneticCode.Substring(0, midWay - 1);
                if (reverseFirstHalf)
                {
                    return ReversePartOfGeneticCode(firstPartOfGeneticCode, upAndDown, leftAndRight) + RandomCellDirection() + endPartOfGeneticCode;
                }
                else
                {
                    return firstPartOfGeneticCode + RandomCellDirection() + ReversePartOfGeneticCode(endPartOfGeneticCode, upAndDown, leftAndRight);
                }
            }
            return ReversePartOfGeneticCode(geneticCode, upAndDown, leftAndRight);
        }

        private static string ReversePartOfGeneticCode(string geneticCodePart, bool upAndDown, bool leftAndRight)
        {
            StringBuilder reverseCode = new StringBuilder();
            foreach (char character in geneticCodePart)
            {
                switch (Convert.ToString(character))
                {
                    case DOWN:
                        {
                            if (upAndDown)
                                reverseCode.Append(UP);
                            else
                                reverseCode.Append(DOWN);
                            break;
                        }
                    case UP:
                        {
                            if (upAndDown)
                                reverseCode.Append(DOWN);
                            else
                                reverseCode.Append(UP);
                            break;
                        }
                    case RIGHT:
                        {
                            if (leftAndRight)
                                reverseCode.Append(LEFT);
                            else
                                reverseCode.Append(RIGHT);
                            break;
                        }
                    case LEFT:
                        {
                            if (leftAndRight)
                                reverseCode.Append(RIGHT);
                            else
                                reverseCode.Append(LEFT);
                            break;
                        }
                    default:
                        {
                            reverseCode.Append(character);
                            break;
                        }
                }
            }
            return reverseCode.ToString();
        }

        private static string RandomCellDirection()
        {
            switch (NextRandomNumber(0, 4))
            {
                case 0:
                    {
                        return LEFT;
                    }
                case 1:
                    {
                        return DOWN;
                    }
                case 2:
                    {
                        return RIGHT;
                    }
                case 3:
                    {
                        return UP;
                    }
                default: // this should never be used
                    {
                        return DOWN;
                    }
            }
        }
    }
}