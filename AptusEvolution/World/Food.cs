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
using ClockTimer = System.Timers;
using System.Configuration;
using System.Collections.Generic;

namespace Evolution.World
{
    public class Food
    {
        //Expiry Timer
        private readonly ClockTimer.Timer m_stopWatch;

        //amount that a standard food item has
        private readonly float m_amount = float.Parse(ConfigurationManager.AppSettings["FoodValueWhenConsumed"]);
        private static int s_foodExpiryTime = Convert.ToInt32(ConfigurationManager.AppSettings["FoodExpiryTimeSeconds"]) * 1000;

        //Unique Identifier for the Food item
        public Guid m_identifier  = Guid.NewGuid();
        public Position Position { get; }
        public string PosKey { get; set; }

        //Hold the expired action to call when food timer runs up
        private readonly Action<Food> m_expired;

        /// <summary>
        /// Creates a new Food Item, with a position and an action to call when it has expired
        /// </summary>
        /// <param name="position"></param>
        /// <param name="expired_"></param>
        public Food(Position position, Action<Food> expired_)
        {
            Position = position;
            m_expired = expired_;
            PosKey = GenerateFoodPositionKey(position);

            m_stopWatch = new ClockTimer.Timer
            {
                Interval = s_foodExpiryTime
            };
            m_stopWatch.Elapsed += OnTimedEvent;

            // Have the timer fire repeated events (true is the default)
            m_stopWatch.AutoReset = false;

            // Start the timer
            m_stopWatch.Enabled = true;
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            m_expired.Invoke(this);
        }

        public Guid Identifier
        {
            get
            {
                return m_identifier;
            }
        }

        public static string GenerateFoodPositionKey(Position pos)
        {
            return pos.X + "-" + pos.Y;
        }

        public static IList<string> GenerateFoodPositionKeys(Position prevPos, Position pos)
        {
            IList<string> keys = new List<string>();
            int stepsX = prevPos.X - pos.X;
            int signFlag = Math.Sign(stepsX);
            if (signFlag == 1) //postive number
            {
                for (int i = 1; i <= stepsX; i++)
                {
                    keys.Add(prevPos.X + i + "-" + prevPos.Y);
                }
            } else if(signFlag == -1)
            {
                for (int i = -1; i >= stepsX; i--)
                {
                    keys.Add(prevPos.X - i + "-" + prevPos.Y);
                }
            } 

            int stepsY = pos.Y - prevPos.Y;
            signFlag = Math.Sign(stepsY);
            if (signFlag == 1) //postive number
            {
                for (int i = 1; i <= stepsY; i++)
                {
                    keys.Add(prevPos.X + "-" + (prevPos.Y + 1));
                }
            }
            else if (signFlag == -1)
            {
                for (int i = -1; i >= stepsY; i--)
                {
                    keys.Add(prevPos.X + "-" + (prevPos.Y - 1));
                }
            }
            
            return keys;

            //for (int i = 1; i <= dist; i++) //TODO should it be like this, or zero? 
            //{
            //    //LEFT, UP, DOWN, RIGHT, NONE
            //    switch (cellDirection)
            //    {
            //        case Propulsion.PositionOnCell.LEFT:
            //            {
            //                keys.Add(pos.X + i + "-" + pos.Y);
            //                break;
            //            }
            //        case Propulsion.PositionOnCell.UP:
            //            {
            //                keys.Add(pos.X + "-" + (pos.Y + i));
            //                break;
            //            }
            //        case Propulsion.PositionOnCell.RIGHT:
            //            {
            //                keys.Add(pos.X - i + "-" + pos.Y);
            //                break;
            //            }
            //        case Propulsion.PositionOnCell.DOWN:
            //            {
            //                keys.Add(pos.X + "-" + (pos.Y - i));
            //                break;
            //            }
            //        default:
            //            {
            //                keys.Add(GenerateFoodPositionKey(pos));
            //                return keys;
            //            }
            //    }
            //}
            //return keys;
        }

        public static IList<string> GenerateFoodPositionKeys(Position pos, Propulsion.PositionOnCell cellDirection, int dist)
        {
            IList<string> keys = new List<string>();
            for(int i = 1; i <= dist; i++) //TODO should it be like this, or zero? 
            {
                //LEFT, UP, DOWN, RIGHT, NONE
                switch (cellDirection)
                {
                    case Propulsion.PositionOnCell.LEFT :
                        {
                            keys.Add(pos.X + i + "-" + pos.Y);
                            break;
                        }
                    case Propulsion.PositionOnCell.UP:
                        {
                            keys.Add(pos.X + "-" + (pos.Y + i));
                            break;
                        }
                    case Propulsion.PositionOnCell.RIGHT:
                        {
                            keys.Add(pos.X - i + "-" + pos.Y);
                            break;
                        }
                    case Propulsion.PositionOnCell.DOWN:
                        {
                            keys.Add(pos.X + "-" + (pos.Y - i));
                            break;
                        }
                    default:
                        {
                            keys.Add(GenerateFoodPositionKey(pos));
                            return keys;
                        }
                }
            }
            return keys;
        }

        public static int FoodExpiryTime
        {
            get
            {
                return s_foodExpiryTime / 1000;
            }
            set
            {
                s_foodExpiryTime = value * 1000;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public float Use()
        {
            return m_amount;
        }
    }
}