//#define DEBUG 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;


namespace GameCore
{

    /// <summary>
    /// Class Name: MonteCarloNode
    /// Description: MonteCarloNode is a node to be used in the building of a Monte Carlo Search Tree. The constructor for a Monte Carlo node
    /// either having it start at e9 or can be given coordinates of where to start along, and may also be passed a List of WallCoordinates.
    /// </summary>
    class MonteCarloNode
    {
        private List<MonteCarloNode> children;
        private List<WallCoordinate> walls;
        private static List<BitArray> board;
        private static Random randomPercentileChance;
        private static string MonteCarloPlayer;
        private MonteCarloNode parent;
        private List<PlayerCoordinate> playerLocations;
        private List<int> wallsRemaining;

        private int wins;
        private int timesVisited;
        private bool gameOver;
        private GameBoard.PlayerEnum turn;

        public static int TOTAL_ROWS = 17;
        public static int TOTAL_COLS = 17;
        // private List<string> invalidMoves;
        // public GameBoard boardState;
        private string thisMove;

        public List<BitArray> GetBoard()
        {
            return board;
        }

        public string GetMove()
        {
            return thisMove;
        }

        public MonteCarloNode(PlayerCoordinate playerOne, PlayerCoordinate playerTwo, int playerOneTotalWalls, int playerTwoTotalWalls, List<WallCoordinate> wallCoordinates, GameBoard.PlayerEnum currentTurn)
        {
            board = new List<BitArray>();
            for (int i = 0; i < TOTAL_ROWS; i++)
            {
                board.Add(new BitArray(131072));
            }

            if (MonteCarloPlayer == null)
            {
                MonteCarloPlayer = currentTurn.ToString();
            }

            playerLocations = new List<PlayerCoordinate>();
            playerLocations.Add(new PlayerCoordinate(playerOne.Row, playerOne.Col));
            playerLocations.Add(new PlayerCoordinate(playerTwo.Row, playerTwo.Col));


            wallsRemaining = new List<int>();
            wallsRemaining.Add(playerOneTotalWalls);
            wallsRemaining.Add(playerTwoTotalWalls);

            children = new List<MonteCarloNode>();
            walls = new List<WallCoordinate>(wallCoordinates);
            randomPercentileChance = new Random();
            turn = currentTurn;
            gameOver = false;
//            invalidMoves = new List<string>();
            parent = null;
        }

        public MonteCarloNode(string move, List<PlayerCoordinate> players, List<int> wallCounts, List<WallCoordinate> wallCoordinates, WallCoordinate newWallCoordinate, GameBoard.PlayerEnum currentTurn, MonteCarloNode childParent)
        {
            playerLocations = new List<PlayerCoordinate>(players);


            wallsRemaining = new List<int>(wallCounts);

            children = new List<MonteCarloNode>();
            walls = new List<WallCoordinate>(wallCoordinates);
            turn = currentTurn;
            parent = childParent;
            board = childParent.GetBoard();
            thisMove = move;

            walls.Add(newWallCoordinate);
            if (currentTurn == GameBoard.PlayerEnum.ONE)
            {
                wallsRemaining[0]--;
            }
            else if (currentTurn == GameBoard.PlayerEnum.TWO)
            {
                wallsRemaining[1]--;
            }
            // Mark that this player has taken their turn 
            turn = currentTurn == 0 ? GameBoard.PlayerEnum.TWO : GameBoard.PlayerEnum.ONE;
        }

        public MonteCarloNode(MonteCarloNode childParent, string move)
        {
            parent = childParent;
            board = childParent.GetBoard();
            thisMove = move;
            PlayerCoordinate destinationCoordinate;
            //            invalidMoves = new List<string>();
            destinationCoordinate = new PlayerCoordinate(move);

            switch (childParent.turn)
            {
                case GameBoard.PlayerEnum.ONE:
                    playerLocations[0].Row = destinationCoordinate.Row;
                    playerLocations[0].Col = destinationCoordinate.Col;
                    turn = GameBoard.PlayerEnum.TWO;
                    break;
                case GameBoard.PlayerEnum.TWO:
                    playerLocations[1].Row = destinationCoordinate.Row;
                    playerLocations[1].Col = destinationCoordinate.Col;
                    turn = GameBoard.PlayerEnum.ONE;
                    break;
            }
            // check for win 
            if (playerLocations[0].Row == 0)
            {
                gameOver = true;
            }
            if (playerLocations[1].Row == (TOTAL_ROWS - 1))
            {
                gameOver = true;
            }
        }

        private bool MovePiece(GameBoard.PlayerEnum player, PlayerCoordinate destinationCoordinate)
        {
            bool retValue = false;
            PlayerCoordinate startCoordinate = null;
            switch (player)
            {
                case GameBoard.PlayerEnum.ONE:
                    startCoordinate = playerLocations[0];
                    break;
                case GameBoard.PlayerEnum.TWO:
                    startCoordinate = playerLocations[1];
                    break;
            }

            if (ValidPlayerMove(startCoordinate, destinationCoordinate))
            {
                retValue = true;
            }


            return retValue;
        }

        public List<MonteCarloNode> GetChildrenNodes()
        {
            return children;
        }

        public MonteCarloNode GetParentNode()
        {
            return parent;
        }

        private bool PlaceWall(GameBoard.PlayerEnum currentTurn, WallCoordinate wallCoordinate)
        {
            if (gameOver || turn != currentTurn)
            {
                return false;
            }

            switch (currentTurn)
            {
                case GameBoard.PlayerEnum.ONE:
                    if (wallsRemaining[0] <= 0)
                    {
                        return false;
                    }
                    break;
                case GameBoard.PlayerEnum.TWO:
                    if (wallsRemaining[1] <= 0)
                    {
                        return false;
                    }
                    break;
            }


            if (CanPlayersReachGoal(wallCoordinate))
            {
                return true;
            }

            return false;
        }

        private bool IsValidWallPlacement(WallCoordinate wall)
        {
            bool onBoard = IsMoveInBounds(wall.StartRow, wall.StartCol)
                         && IsMoveInBounds(wall.EndRow, wall.EndCol);
            if (!onBoard)
            {
                return false;
            }

            bool onWallSpace = IsOddSpace(wall.StartRow, wall.StartCol, wall.Orientation)
                            && IsOddSpace(wall.EndRow, wall.EndCol, wall.Orientation);
            bool isEmpty = IsEmptyWallSpace(wall.StartRow, wall.StartCol)
                       && IsEmptyWallSpace(wall.EndRow, wall.EndCol);
            return onWallSpace
                && isEmpty;
        }

        private bool CanPlayersReachGoal(WallCoordinate wallCoordinate)
        {
            Populate();

            board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);
            board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);

            bool canPlayerOneReachGoal = BoardUtil.CanReachGoalBitArray(board, 0, playerLocations[0].Row, playerLocations[0].Col);
            bool canPlayerTwoReachGoal = BoardUtil.CanReachGoalBitArray(board, 16, playerLocations[1].Row, playerLocations[1].Col);
            
            board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);
            board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);

            Unpopulate();

            return canPlayerOneReachGoal && canPlayerTwoReachGoal;
        }

        private bool ValidPlayerMove(PlayerCoordinate start, PlayerCoordinate destination)
        {
            if (gameOver
                || !IsMoveInBounds(destination.Row, destination.Col))
            {
                return false;
            }

            bool onPlayerSpace = IsMoveOnOpenSpace(turn, destination);
            bool blocked = IsMoveBlocked(start, destination);
            bool canReach = IsDestinationAdjacent(start, destination);
            if (!canReach)
            {
                canReach = IsValidJump(turn, start, destination);
            }

            return onPlayerSpace
                && !blocked
                && canReach;
        }
        
        private bool ValidWallMove(string move)
        {
            bool validityOfWallPlacement = false;

            if (move.Length == 3 && (move[2] == 'v' || move[2] == 'h'))
            {
                WallCoordinate givenMove = new WallCoordinate(move);

                if (!walls.Contains(givenMove))
                {
                    bool onBoard = IsMoveInBounds(givenMove.StartRow, givenMove.StartCol)
                                && IsMoveInBounds(givenMove.EndRow, givenMove.EndCol);
                    if (!onBoard)
                    {
                        return false;
                    }

                    bool onWallSpace = IsOddSpace(givenMove.StartRow, givenMove.StartCol, givenMove.Orientation)
                                    && IsOddSpace(givenMove.EndRow, givenMove.EndCol, givenMove.Orientation);
                    bool isEmpty = IsEmptyWallSpace(givenMove.StartRow, givenMove.StartCol)
                               && IsEmptyWallSpace(givenMove.EndRow, givenMove.EndCol);
                    return onWallSpace
                        && isEmpty;
                }
            }

            return validityOfWallPlacement;
        }

        private bool IsEmptyWallSpace(int row, int col)
        {
            return board[row].Get(col) == false;
        }

        private bool IsOddSpace(int row, int col, WallCoordinate.WallOrientation orientation)
        {
            bool retValue = false;
            switch (orientation)
            {
                case WallCoordinate.WallOrientation.Horizontal:
                    retValue = row % 2 == 1;
                    break;
                case WallCoordinate.WallOrientation.Vertical:
                    retValue = col % 2 == 1;
                    break;
            }
            return retValue;
        }
         
        private bool IsDestinationAdjacent(PlayerCoordinate start, PlayerCoordinate destination)
        {
            bool verticalMove = (Math.Abs(destination.Row - start.Row) == 2) && (Math.Abs(destination.Col - start.Col) == 0);
            bool horizontalMove = (Math.Abs(destination.Col - start.Col) == 2) && (Math.Abs(destination.Row - start.Row) == 0);
            return verticalMove ^ horizontalMove; // Only north south east west are considered adjacent 
        }

        private bool IsMoveBlocked(PlayerCoordinate start, PlayerCoordinate destination)
        {
            bool blocked = false;
            if (start.Row == destination.Row)
            {
                if (start.Col < destination.Col)
                {
                    blocked = (board[start.Row].Get(start.Col + 1) == true) || (board[destination.Row].Get(destination.Col - 1) == true);
                }
                else
                {
                    blocked = (board[start.Row].Get(start.Col - 1) == true) || (board[destination.Row].Get(destination.Col + 1) == true);
                }
            }
            else if (start.Col == destination.Col)
            {
                if (start.Row < destination.Row)
                {
                    blocked = (board[start.Row + 1].Get(start.Col) == true) || (board[destination.Row - 1].Get(destination.Col) == true);
                }
                else
                {
                    blocked = (board[start.Row - 1].Get(start.Col) == true) || (board[destination.Row + 1].Get(destination.Col) == true);
                }
            }
            return blocked;
        }

        private bool IsMoveInBounds(int row, int col)
        {
            return row >= 0
                && row < TOTAL_ROWS
                && col >= 0
                && col < TOTAL_COLS;
        }   

        private bool IsMoveOnOpenSpace(GameBoard.PlayerEnum turn, PlayerCoordinate destination)
        {
            bool onPlayerSpace = destination.Row % 2 == 0  // odd rows are walls 
                  && destination.Col % 2 == 0; // odd cols are walls 

            bool isSpaceEmpty;
            if (turn == GameBoard.PlayerEnum.ONE)
            {
                isSpaceEmpty = !(destination.Row == playerLocations[1].Row && destination.Col == playerLocations[1].Col);
            }
            else
            {
                isSpaceEmpty = !(destination.Row == playerLocations[0].Row && destination.Col == playerLocations[0].Col);
            }

            return onPlayerSpace && isSpaceEmpty;
        }

        private bool IsValidJump(GameBoard.PlayerEnum turn, PlayerCoordinate start, PlayerCoordinate destination)
        { // Jumping over? 
            Tuple<int, int> midpoint = FindMidpoint(start, destination);
            int midRow = midpoint.Item1;
            int midCol = midpoint.Item2;
            int opponentRow, opponentCol;
            if (turn == GameBoard.PlayerEnum.ONE)
            {
                opponentRow = playerLocations[1].Row;
                opponentCol = playerLocations[1].Col;
            }
            else
            {
                opponentRow = playerLocations[0].Row;
                opponentCol = playerLocations[0].Col;
            }
            bool overJump = midRow == opponentRow
                && midCol == opponentCol
                && (Math.Abs(destination.Row - start.Row) == 4 || Math.Abs(destination.Col - start.Col) == 4);

            // Diagonal jump? 
            bool diagonalJump = false;
            PlayerCoordinate opponent;
            if (turn == GameBoard.PlayerEnum.ONE)
            {
                opponent = new PlayerCoordinate(playerLocations[1].Row, playerLocations[1].Col);
            }
            else
            {
                opponent = new PlayerCoordinate(playerLocations[0].Row, playerLocations[0].Col);
            }

            if (start.Row != destination.Row && start.Col != destination.Col)
            {
                int targetOppRow, targetOppoCol;
                if (destination.Row == start.Row - 2 && destination.Col == start.Col + 2) // NE
                {
                    targetOppRow = start.Row - 2;
                    targetOppoCol = start.Col + 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row - 3].Get(start.Col) == true || board[start.Row].Get(start.Col + 3) == true);
                }
                else if (destination.Row == start.Row - 2 && destination.Col == start.Col - 2) // NW
                {
                    targetOppRow = start.Row - 2;
                    targetOppoCol = start.Col - 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row - 3].Get(start.Col) == true || board[start.Row].Get(start.Col - 3) == true);
                }
                else if (destination.Row == start.Row + 2 && destination.Col == start.Col - 2) // SW
                {
                    targetOppRow = start.Row + 2;
                    targetOppoCol = start.Col - 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row + 3].Get(start.Col) == true || board[start.Row].Get(start.Col - 3) == true);
                }
                else if (destination.Row == start.Row + 2 && destination.Col == start.Col + 2) // SE 
                {
                    targetOppRow = start.Row + 2;
                    targetOppoCol = start.Col + 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row + 3].Get(start.Col) == true || board[start.Row].Get(start.Col + 3) == true);
                }
            }

            return overJump || diagonalJump;
        }

        private Tuple<int, int> FindMidpoint(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return new Tuple<int, int>((start.Row + destination.Row) / 2, (start.Col + destination.Col) / 2);
        }

        private string RandomMove()
        {
            return randomPercentileChance.Next(1, 100) >= 51 ? BoardUtil.GetRandomWallPlacementMove() : BoardUtil.GetRandomPlayerPieceMove();
        }


        private void Populate()
        {
            foreach (var wallCoordinate in walls)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);
                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);
            }
        }

        private void Unpopulate()
        {
            foreach (var wallCoordinate in walls)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);
                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);
            }
        }

        /// <summary>
        /// The <c>ExpandOptions</c> method calls the <c>RandomMove</c> method to generate a move to expand the current options from the current <c>MonteCarloNode</c>
        /// </summary>

        public void ExpandOptions()
        {
            string move;
            move = RandomMove();
            while (!InsertChild(move))
            {
                move = RandomMove();
            }
        }
        /// <summary>
        /// The <c>InsertChild</c> method inserts a new <c>MonteCarloNode</c> child into the current <c>children</c> List. If the move is valid it will return true signifying success. 
        /// If the move was an invalid move the method will return false
        /// </summary>
        /// <param name="move">specified move - either place a wall or move a pawn</param>

        private bool InsertChild(string move)
        {
            bool successfulInsert = false;

            if (ValidPlayerMove( turn == 0 ? playerLocations[0] : playerLocations[1], new PlayerCoordinate(move)) || ValidWallMove(move) )
            {

                if (move.Contains("v") || move.Contains("h"))
                {
                    if (PlaceWall(turn == 0 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO, new WallCoordinate(move)))
                    {
                        children.Add(new MonteCarloNode(move, playerLocations, wallsRemaining, walls, new WallCoordinate(move), turn, this));
                        successfulInsert = true;
                    }
                }
                else
                {
                    children.Add(new MonteCarloNode(this, move));
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
        }

        public MonteCarlo(GameBoard boardState)
        {
       //     MonteCarloNode TreeSearch = new MonteCarloNode(boardState);
            TreeSearch.ExpandOptions();
        }


    }
}