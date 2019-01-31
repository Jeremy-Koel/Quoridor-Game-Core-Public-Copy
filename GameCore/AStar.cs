using System;
using System.Collections.Generic;
using Priority_Queue;

namespace GameCore
{
    class AStar
    {
        /*
        private char[,] gameBoard;
        private int finishRow;
        private Tuple<int, int> startNode;

        // The set of nodes already evaluated
        private HashSet<Tuple<int, int>> evaluatedNodes;

        // The set of currently discovered nodes that are not evaluated yet.
        private HashSet<Tuple<int, int>> discoveredNodes;

        // For each node, which node it can most efficiently be reached from.
        // If a node can be reached from many nodes, cameFrom will eventually contain the
        // most efficient previous step.
        //private Dictionary<Tuple<int, int>, Tuple<int, int>> cameFrom;

        // For each node, the cost of getting from the start node to that node.
        private Dictionary<Tuple<int, int>, double> gScore;

        // For each node, the total cost of getting from the start node to the goal
        // by passing by that node. That value is partly known, partly heuristic.
        private Dictionary<Tuple<int, int>, double> fScore;

        // Priority queue of accessible nodes 
        private SimplePriorityQueue<Tuple<int, int>, double> queue;


        public AStar(char[,] gameBoard, Tuple<int, int> startNode, int finishRow)
        {
            evaluatedNodes = new HashSet<Tuple<int, int>>();
            discoveredNodes = new HashSet<Tuple<int, int>>();
            gScore = new Dictionary<Tuple<int, int>, double>();
            fScore = new Dictionary<Tuple<int, int>, double>();
            queue = new SimplePriorityQueue<Tuple<int, int>, double>();
            this.gameBoard = gameBoard;
            this.startNode = startNode;
            this.finishRow = finishRow;
        }

        public double FindPath()
        {
            // Initially, only the start node is known.
            discoveredNodes.Add(startNode);
            
            // The cost of going from start to start is zero.
            gScore[startNode] = 0;
            
            // For the first node, that value is completely heuristic.
            fScore[startNode] = GetHeuristicCostEstimate(startNode);

            // Keep track of lowest and next lowest fscore values 
            queue.Enqueue(startNode, fScore[startNode]);

            while (discoveredNodes.Count > 0)
            {
                Tuple<int, int> current = queue.Dequeue();
                
                if (current.Item1 == finishRow)
                {
                    return gScore[current];
                }

                evaluatedNodes.Add(current);
                discoveredNodes.Remove(current);
                
                if (current.Item2 + 2 < GameBoard.TOTAL_COLS
                    && gameBoard[current.Item1,current.Item2+1] != GameBoard.WALL) // Can move East
                {
                    FindNeighbors(current, current.Item1 + 2, current.Item2);
                }
                if (current.Item2 - 2 >= 0
                    && gameBoard[current.Item1, current.Item2 -1] != GameBoard.WALL) // Can move West 
                {
                    FindNeighbors(current, current.Item1 - 2, current.Item2);
                }
                if (current.Item1 + 2 < GameBoard.TOTAL_COLS
                    && gameBoard[current.Item1 + 1, current.Item2] != GameBoard.WALL) // Can move North 
                {
                    FindNeighbors(current, current.Item1, current.Item2 + 2);
                }
                if (current.Item1 - 2 >= 0
                    && gameBoard[current.Item1 - 1, current.Item2] != GameBoard.WALL) // Can move South 
                {
                    FindNeighbors(current, current.Item1, current.Item2 - 2);
                }
            }
            return -1;
        }

        private void FindNeighbors(
            Tuple<int, int> current,
            int newRow,
            int newCol)
        {
            Tuple<int, int> neighbor = new Tuple<int, int>(newRow, newCol);
            if (!evaluatedNodes.Contains(neighbor))
            {
                // The distance from start to a neighbor
                double tentativeScore = gScore[current] + 1; // Our distance will always be 1 

                if (!discoveredNodes.Contains(neighbor))
                {
                    discoveredNodes.Add(neighbor);
                    queue.Enqueue(neighbor, tentativeScore);
                }
                else if (tentativeScore < gScore[neighbor])
                {
                    // Foudn new best path
                    gScore[neighbor] = tentativeScore;
                    fScore[neighbor] = tentativeScore + GetHeuristicCostEstimate(neighbor);

                }
            }
        }

        private double GetHeuristicCostEstimate(Tuple<int, int> refNode)
        {
            double heuristicCost = 0;

            for (int i = 0; i < GameBoard.TOTAL_ROWS; ++i)
            {
                heuristicCost += Math.Abs(i - refNode.Item1) + Math.Abs(finishRow - refNode.Item2);
            }

            return heuristicCost / (double)GameBoard.TOTAL_ROWS;
        }

    */
    }
}
