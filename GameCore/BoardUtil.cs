﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    public class BoardUtil
    {
        private static Dictionary<char, int> playerRowTranslationMap;
        private static Dictionary<char, int> playerColTranslationMap;
        private static Random random;

        static BoardUtil()
        {
            playerRowTranslationMap = new Dictionary<char, int>();
            playerRowTranslationMap.Add('1', 16);
            playerRowTranslationMap.Add('2', 14);
            playerRowTranslationMap.Add('3', 12);
            playerRowTranslationMap.Add('4', 10);
            playerRowTranslationMap.Add('5', 8);
            playerRowTranslationMap.Add('6', 6);
            playerRowTranslationMap.Add('7', 4);
            playerRowTranslationMap.Add('8', 2);
            playerRowTranslationMap.Add('9', 0);
            
            playerColTranslationMap = new Dictionary<char, int>();
            playerColTranslationMap.Add('a', 0);
            playerColTranslationMap.Add('b', 2);
            playerColTranslationMap.Add('c', 4);
            playerColTranslationMap.Add('d', 6);
            playerColTranslationMap.Add('e', 8);
            playerColTranslationMap.Add('f', 10);
            playerColTranslationMap.Add('g', 12);
            playerColTranslationMap.Add('h', 14);
            playerColTranslationMap.Add('i', 16);

            random = new Random();
        }

        public static int GetInteralPlayerRow(char c)
        {
            return playerRowTranslationMap[c];
        }

        public static int GetInternalPlayerCol(char c)
        {
            return playerColTranslationMap[c];
        }

        // BFS to find if indicated player can reach their target row 
        public static bool CanReachGoal(char[,] gameBoard, int goalRow, int startX, int startY)
        {
            HashSet<Tuple<int, int>> markedSet = new HashSet<Tuple<int, int>>();
            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();

            Tuple<int, int> startNode = new Tuple<int, int>(startX, startY);
            markedSet.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                Tuple<int, int> current = queue.Dequeue();
                if (current.Item1 == goalRow)
                {
                    return true;
                }

                if (current.Item2 + 2 < GameBoard.TOTAL_COLS
                    && gameBoard[current.Item1, current.Item2 + 1] != GameBoard.WALL) // Can move East
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1, current.Item2 + 2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                if (current.Item2 - 2 >= 0
                    && gameBoard[current.Item1, current.Item2 - 1] != GameBoard.WALL) // Can move West 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1, current.Item2 - 2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                if (current.Item1 - 2  >= 0
                    && gameBoard[current.Item1 - 1, current.Item2] != GameBoard.WALL) // Can move North 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1 - 2, current.Item2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                if (current.Item1 + 2 < GameBoard.TOTAL_COLS
                    && gameBoard[current.Item1 + 1, current.Item2] != GameBoard.WALL) // Can move South 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1 + 2, current.Item2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return false;
        }


        // BitArray version of BFS to find if indicated player can reach their target row 
        public static bool CanReachGoalBitArray(List<BitArray> gameBoard, int goalRow, int startX, int startY)
        {
            HashSet<Tuple<int, int>> markedSet = new HashSet<Tuple<int, int>>();
            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();

            Tuple<int, int> startNode = new Tuple<int, int>(startX, startY);
            markedSet.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                Tuple<int, int> current = queue.Dequeue();
                if (current.Item1 == goalRow)
                {
                    return true;
                }

                if (current.Item2 + 2 < GameBoard.TOTAL_COLS
                    && gameBoard[current.Item1].Get(current.Item2 + 1) != true) // Can move East
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1, current.Item2 + 2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                if (current.Item2 - 2 >= 0
                    && gameBoard[current.Item1].Get(current.Item2 - 1) != true) // Can move West 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1, current.Item2 - 2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                if (current.Item1 - 2 >= 0
                    && gameBoard[current.Item1 - 1].Get(current.Item2) != true) // Can move North 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1 - 2, current.Item2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                if (current.Item1 + 2 < GameBoard.TOTAL_COLS
                    && gameBoard[current.Item1 + 1].Get(current.Item2) != true) // Can move South 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1 + 2, current.Item2);
                    if (!markedSet.Contains(neighbor))
                    {
                        markedSet.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return false;
        }

        public static string GetRandomPlayerPieceMove()
        {
            StringBuilder sb = new StringBuilder();
            int col = random.Next(97, 106);
            int row = random.Next(1, 10);
            sb.Append(Convert.ToChar(col));
            sb.Append(row);
            return sb.ToString();
        }

        public static string GetRandomNearbyPlayerPieceMove(PlayerCoordinate player)
        {
            StringBuilder sb = new StringBuilder();
            int col = random.Next( (player.Col == 0 ? 97 : ((97 + (player.Col / 2) - 2)) < 97 ? 97 : 97 + (player.Col / 2) - 2), (player.Col == 16 ? 106 : 97 + (player.Col / 2) + 2) > 105 ? 106 : 97 + (player.Col / 2) + 3);
            sb.Append(Convert.ToChar(col));
            int row = random.Next(player.Row == 16 ? 1 : (9 - (player.Row / 2) - 2 < 1 ? 1 : 9 - (player.Row / 2) - 2), player.Row == 0 ? 10 : (9 - (player.Row / 2) + 2 > 9 ? 9 : 9 - (player.Row / 2) + 3));
            sb.Append(row);
            return sb.ToString();
        }

        public static string GetRandomWallPlacementMove()
        {
            StringBuilder sb = new StringBuilder();
            Random rand = new Random();
            int col = random.Next(97, 105);
            int row = random.Next(1, 9);
            char orientation;
            if (random.Next() % 2 == 0)
            {
                orientation = 'h';
            }
            else
            {
                orientation = 'v';
            }
            sb.Append(Convert.ToChar(col));
            sb.Append(row);
            sb.Append(orientation);
            return sb.ToString();

        }

    }
}
