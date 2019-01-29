using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class AStar
    {
        private static double HeuristicCostEstimate(Tuple<int, int> start, int finishRow)
        {
            double heuristicCost = 0;

            for (int i = 0; i < GameBoard.TOTAL_ROWS; ++i)
            {
                heuristicCost += Math.Abs(i - start.Item1) + Math.Abs(finishRow - start.Item2);
            }

            return heuristicCost / (double) GameBoard.TOTAL_ROWS;
        }
        public static double FindPath(char[,] gameBoard, Tuple<int, int> start, int finishRow)
        {
            // The set of nodes already evaluated
            HashSet<Tuple<int, int>> evaluatedNodes = new HashSet<Tuple<int, int>>();

            // The set of currently discovered nodes that are not evaluated yet.
            // Initially, only the start node is known.
            HashSet<Tuple<int, int>> discoveredNodes = new HashSet<Tuple<int, int>>();
            discoveredNodes.Add(start);

            // For each node, which node it can most efficiently be reached from.
            // If a node can be reached from many nodes, cameFrom will eventually contain the
            // most efficient previous step.
            Dictionary<Tuple<int, int>, Tuple<int, int>> cameFrom = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

            // For each node, the cost of getting from the start node to that node.
            Dictionary<Tuple<int, int>, double> gScore = new Dictionary<Tuple<int, int>, double>();

            // The cost of going from start to start is zero.
            gScore[start] = 0;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            Dictionary<Tuple<int, int>, double> fScore = new Dictionary<Tuple<int, int>, double>();

            // For the first node, that value is completely heuristic.
            fScore[start] = HeuristicCostEstimate(start, finishRow);

            // Keep track of lowest and next lowest fscore values 
            Tuple<int, int> lowestIndex = start;
            double minScore = fScore[start];
            Tuple<int, int> nextLowestIndex = null;
            double nextMinScore = Double.PositiveInfinity;

            while (discoveredNodes.Count > 0)
            {
                Tuple<int, int> current = lowestIndex;
                if (current.Item2 == finishRow)
                {
                    return gScore[current];
                }

                evaluatedNodes.Add(current);
                discoveredNodes.Remove(current);
                lowestIndex = nextLowestIndex;
                minScore = nextMinScore;

                // For each neighbor of current node 
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        if (i == 0 ^ j == 0)
                        {
                            Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1 + i, current.Item2 + j);
                            if (!evaluatedNodes.Contains(neighbor))
                            {
                                // The distance from start to a neighbor
                                double tentativeScore = gScore[current] + 1; // Our distance will always be 1 

                                if (!discoveredNodes.Contains(neighbor))
                                {
                                    discoveredNodes.Add(neighbor);
                                }
                                else if (tentativeScore >= gScore[neighbor])
                                {
                                    continue;
                                }

                                // This path is the best until now. Record it!
                                cameFrom[neighbor] = current;
                                gScore[neighbor] = tentativeScore;
                                fScore[neighbor] = tentativeScore + HeuristicCostEstimate(neighbor, finishRow);

                                // Update lowest score 
                                if (fScore[neighbor] < minScore)
                                {
                                    minScore = fScore[neighbor];
                                    lowestIndex = neighbor;
                                }
                                else if (fScore[neighbor] < nextMinScore)
                                {
                                    nextMinScore = fScore[neighbor];
                                    nextLowestIndex = neighbor;
                                }
                            }
                        }
                    }
                }
            }
            return -1;
        }
        static AStar()
        {

        }
    }
}
