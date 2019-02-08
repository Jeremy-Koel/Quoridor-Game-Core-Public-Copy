#define DEBUG 
using System;
using System.Collections.Generic;
using System.Text;


namespace GameCore
{

    /*
     * MonteCarloNode is 
     */

    /// <summary>
    /// Class Name: MonteCarloNode
    /// Description: MonteCarloNode is a node to be used in the building of a Monte Carlo Search Tree. The constructor for a Monte Carlo node
    /// accepts a Gameboard.
    /// </summary>
    class MonteCarloNode
    {
        private List<MonteCarloNode> children;
        private MonteCarloNode parent;
        private int wins;
        private int timesVisited;
        // private List<string> invalidMoves;
        public GameBoard boardState;
        private string thisMove;

        public MonteCarloNode(GameBoard newState)
        {
            boardState = new GameBoard(newState);
            children = new List<MonteCarloNode>();
//            invalidMoves = new List<string>();
            parent = null;
        }

        public MonteCarloNode(MonteCarloNode childParent, GameBoard newState, string move)
        {
            parent = childParent;
            boardState = newState;
            thisMove = move;
//            invalidMoves = new List<string>();
            if (!move.Contains("v") && !move.Contains("h"))
            {
                boardState.MovePiece(boardState.GetWhoseTurn() == 1 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO, new PlayerCoordinate(move));
            }
            boardState.PrintBoard();
        }

        public List<MonteCarloNode> GetChildrenNodes()
        {
            return children;
        }

        public MonteCarloNode GetParentNode()
        {
            return parent;
        }
        
        private bool ValidWallMove(string move)
        {
            return boardState.IsValidWallPlacement(new WallCoordinate(move));
        }

        private bool ValidPlayerMove(string move)
        {
            return boardState.IsValidPlayerMove(boardState.GetWhoseTurn() == 1 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO, 
                                                boardState.GetPlayerCoordinate(boardState.GetWhoseTurn()),
                                                new PlayerCoordinate(move));
        }

        /// <summary>
        /// The <c>ExpandOptions</c> method calls the <c>RandomMove</c> method to generate a move to expand the current options from the current <c>MonteCarloNode</c>
        /// </summary>

        public void ExpandOptions()
        {
            string move;
#if DEBUG 
            move = "e2";
#else
            move = RandomMove();
#endif
            while (!InsertChild(move))
            { }
        } 

        /// <summary>
        /// The <c>InsertChild</c> method inserts a new <c>MonteCarloNode</c> child into the current <c>children</c> List. If the move is valid it will return true signifying success. 
        /// If the move was an invalid move the method will return false
        /// </summary>
        /// <param name="move">specified move - either place a wall or move a pawn</param>
        
        public bool InsertChild(string move)
        {
            bool successfulInsert = false;

            if (ValidPlayerMove(move) || ValidWallMove(move) )
            {
                successfulInsert = true;

                if (move.Contains("v") || move.Contains("h"))
                {
                    GameBoard newState = new GameBoard(boardState);
                    newState.PlaceWall(newState.GetWhoseTurn() == 1 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO, new WallCoordinate(move));
                    children.Add(new MonteCarloNode(this, newState, move));
                }
                else
                {
                    children.Add(new MonteCarloNode(this, boardState, move));
                }
            }

            return successfulInsert;
        }

    }
class MonteCarlo
    {
        // new GameBoard(GameBoard.PlayerEnum.ONE, "e1", "e9")
        MonteCarloNode TreeSearch;

        public static void Main()
        {
            MonteCarlo WeakAI = new MonteCarlo();
        }
        public MonteCarlo()
        {
            MonteCarloNode TreeSearch = new MonteCarloNode(new GameBoard(GameBoard.PlayerEnum.ONE, "e1", "e9"));
        }

        public MonteCarlo(GameBoard boardState)
        {
            MonteCarloNode TreeSearch = new MonteCarloNode(boardState);
            TreeSearch.ExpandOptions();
        }


    }
}