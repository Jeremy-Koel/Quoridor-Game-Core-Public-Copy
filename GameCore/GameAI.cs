using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{

    /*
     * MonteCarloNode is 
     */
    class MonteCarloNode
    {
        private static List<MonteCarloNode> children;
        private static MonteCarloNode parent;
        private static char[,] boardState;
        private static double winRate;
        private static PlayerCoordinate AI, Opponent;
        private static int timesNodeHasBeenVisited;
        private static string move;

        public static List<MonteCarloNode> GetChildrenNodes()
        {
            return children;
        }

        public static MonteCarloNode GetParentNode()
        {
            return parent;
        }

    }
    class MonteCarlo
    {
    }
}
