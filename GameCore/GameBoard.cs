using System;
using System.Collections.Generic;

namespace GameCore
{
    public class GameBoard
    {
        public static char PLAYER_SPACE = '#';
        public static char WALL = '*';
        public static char WALL_SPACE = ' ';
        public static char PLAYER_1 = '1';
        public static char PLAYER_2 = '2';
        public static int TOTAL_ROWS = 17;
        public static int TOTAL_COLS = 17;
        
        private PlayerCoordinate playerOneLocation;
        private PlayerCoordinate playerTwoLocation;
        private char[,] board;
        private bool gameOver;
        private bool playerOneWin;
        private bool playerTwoWin;
        private PlayerEnum whoseTurn;

        public bool GameOver { get => gameOver; }

        public enum PlayerEnum
        {
            ONE, TWO
        }

        public int GetWhoseTurn()
        {
            int currentPlayer = 0;
            if(whoseTurn == PlayerEnum.ONE)
            {
                currentPlayer = 1;
            }
            else
            {
                currentPlayer = 2;
            }
            return currentPlayer;
        }

        public void SetPlayerTurnRandom()
        {
            Random randomNumber = new Random();
            int oneOrTwo = randomNumber.Next(1, 3);
            if (oneOrTwo == 1)
                whoseTurn = PlayerEnum.ONE;
            else if (oneOrTwo == 2)
                whoseTurn = PlayerEnum.TWO;
        }

        private void InitializeBoard(string playerOneStart, string playerTwoStart)
        {
            gameOver = false;
            playerOneWin = false;
            playerTwoWin = false;

            playerOneLocation = new PlayerCoordinate(playerOneStart);
            playerTwoLocation = new PlayerCoordinate(playerTwoStart);

            // Init gameboard 
            board = new char[TOTAL_ROWS, TOTAL_COLS];
            for (int r = 0; r < TOTAL_ROWS; ++r)
            {
                for (int c = 0; c < TOTAL_COLS; ++c)
                {
                    if ((r % 2 == 0) && (c % 2 == 0))
                    {
                        board[r, c] = PLAYER_SPACE;
                    }
                    else
                    {
                        board[r, c] = WALL_SPACE;
                    }
                }
            }
        }

        public GameBoard(string playerOneStart, string playerTwoStart)
        {
            SetPlayerTurnRandom();
            InitializeBoard(playerOneStart, playerTwoStart);
        }


        public GameBoard(PlayerEnum startingPlayer, string playerOneStart, string playerTwoStart)
        {
            whoseTurn = startingPlayer;
            InitializeBoard(playerOneStart, playerTwoStart);
        }

        public void PrintBoard()
        {
            for (int r = 0; r < TOTAL_ROWS; ++r)
            {
                for (int c = 0; c < TOTAL_COLS; ++c)
                {
                    if (r == playerOneLocation.Row && c == playerOneLocation.Col)
                    {
                        Console.Write(PLAYER_1);
                    }
                    else if (r == playerTwoLocation.Row && c == playerTwoLocation.Col)
                    {
                        Console.Write(PLAYER_2);
                    }
                    else
                    {
                        Console.Write(board[r, c]);
                    }
                }
                Console.Write("\n");
            }
        }

        private void changeTurn()
        {
            if (whoseTurn == PlayerEnum.ONE)
            {
                whoseTurn = PlayerEnum.TWO;
            }
            else if (whoseTurn == PlayerEnum.TWO)
            {
                whoseTurn = PlayerEnum.ONE;
            }
        }

        public bool MovePiece(PlayerEnum player, PlayerCoordinate destinationCoordinate)
        {
            if (gameOver || player != whoseTurn)
            {
                return false;
            }

            bool retValue = false;
            PlayerCoordinate startCoordinate = null;
            switch (player)
            {
                case PlayerEnum.ONE:
                    startCoordinate = playerOneLocation;
                    break;
                case PlayerEnum.TWO:
                    startCoordinate = playerTwoLocation;
                    break;
            }

            if (IsValidPlayerMove(player, startCoordinate, destinationCoordinate))
            {
                board[startCoordinate.Row, startCoordinate.Col] = PLAYER_SPACE;
                switch (player)
                {
                    case PlayerEnum.ONE:
                        playerOneLocation.Row = destinationCoordinate.Row;
                        playerOneLocation.Col = destinationCoordinate.Col;
                        whoseTurn = PlayerEnum.TWO;
                        break;
                    case PlayerEnum.TWO:
                        playerTwoLocation.Row = destinationCoordinate.Row;
                        playerTwoLocation.Col = destinationCoordinate.Col;
                        whoseTurn = PlayerEnum.ONE;
                        break;
                }
                retValue = true;
            }

            // check for win 
            if (playerOneLocation.Row == 0)
            {
                playerOneWin = true;
            }
            if (playerTwoLocation.Row == TOTAL_ROWS)
            {
                playerTwoWin = true;
            }
            gameOver = playerOneWin || playerTwoWin;

            return retValue;
        }

        public bool PlaceWall(PlayerEnum player, WallCoordinate wallCoordinate)
        {
            if (gameOver || whoseTurn != player)
            {
                return false;
            }
            
            if (IsValidWallPlacement(wallCoordinate) && CanPlayersReachGoal(wallCoordinate))
            {
                board[wallCoordinate.StartRow, wallCoordinate.StartCol] = board[wallCoordinate.EndRow, wallCoordinate.EndCol] = WALL;
                // Mark that this player has taken their turn 
                changeTurn();
                return true;
            }
            
            return false;
        }

        private bool CanPlayersReachGoal(WallCoordinate wallCoordinate)
        {
            // Make a copy of the board, we don't want to change the original yet 
            char[,] copy = board.Clone() as char[,];
            copy[wallCoordinate.StartRow, wallCoordinate.StartCol] = copy[wallCoordinate.EndRow, wallCoordinate.EndCol] = WALL;

            bool canPlayerOneReachGoal = BoardUtil.CanReachGoal(copy, 0, playerOneLocation.Row, playerOneLocation.Col);
            bool canPlayerTwoReachGoal = BoardUtil.CanReachGoal(copy, 16, playerTwoLocation.Row, playerTwoLocation.Col);
            return canPlayerOneReachGoal && canPlayerTwoReachGoal;
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

        private bool IsEmptyWallSpace(int row, int col)
        {
            return board[row, col] == WALL_SPACE;
        }

        private bool IsValidPlayerMove(PlayerEnum player, PlayerCoordinate start, PlayerCoordinate destination)
        {
            if (gameOver 
                || !IsMoveInBounds(destination.Row, destination.Col))
            {
                return false;
            }

            bool onPlayerSpace = IsMoveOnOpenSpace(player, destination);
            bool blocked = IsMoveBlocked(start, destination);
            bool canReach = IsDestinationAdjacent(start, destination);
            if (!canReach)
            {
                canReach = IsValidJump(player, start, destination);
            }
            
            return onPlayerSpace
                && !blocked
                && canReach;
        }

        private bool IsMoveInBounds(int row, int col)
        {
            return row >= 0
                && row < TOTAL_ROWS
                && col >= 0
                && col < TOTAL_COLS;
        }

        private bool IsMoveOnOpenSpace(PlayerEnum player, PlayerCoordinate destination)
        {
            bool onPlayerSpace = destination.Row % 2 == 0  // odd rows are walls 
                && destination.Col % 2 == 0; // odd cols are walls 

            bool isSpaceEmpty;
            if (player == PlayerEnum.ONE)
            {
                isSpaceEmpty = !(destination.Row == playerTwoLocation.Row && destination.Col == playerTwoLocation.Col);
            }
            else
            {
                isSpaceEmpty = !(destination.Row == playerOneLocation.Row && destination.Col == playerOneLocation.Col);
            }

            return onPlayerSpace && isSpaceEmpty;
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
                    blocked = (board[start.Row,start.Col+1] == WALL) || (board[destination.Row,destination.Col-1] == WALL);
                }
                else
                {
                    blocked = (board[start.Row, start.Col - 1] == WALL) || (board[destination.Row, destination.Col + 1] == WALL);
                }
            }
            else if (start.Col == destination.Col)
            {
                if (start.Row < destination.Row)
                {
                    blocked = (board[start.Row + 1,start.Col] == WALL) || (board[destination.Row - 1,destination.Col] == WALL);
                }
                else
                {
                    blocked = (board[start.Row - 1,start.Col] == WALL) || (board[destination.Row + 1,destination.Col] == WALL);
                }
            }
            return blocked;
        }

        private bool IsValidJump(PlayerEnum player, PlayerCoordinate start, PlayerCoordinate destination)
        {
            // Jumping over? 
            Tuple<int,int> midpoint = FindMidpoint(start, destination);
            int midRow = midpoint.Item1;
            int midCol = midpoint.Item2;
            int opponentRow, opponentCol;
            if (player == PlayerEnum.ONE)
            {
                opponentRow = playerTwoLocation.Row;
                opponentCol = playerTwoLocation.Col;
            }
            else
            {
                opponentRow = playerOneLocation.Row;
                opponentCol = playerOneLocation.Col;
            }
            bool overJump = midRow == opponentRow
                && midCol == opponentCol
                && (Math.Abs(destination.Row - start.Row) == 4 || Math.Abs(destination.Col - start.Col) == 4);

            // Diagonal jump? 
            bool diagonalJump = false;
            PlayerCoordinate opponent;
            if (player == PlayerEnum.ONE)
            {
                opponent = new PlayerCoordinate(playerTwoLocation.Row, playerTwoLocation.Col);
            }
            else
            {
                opponent = new PlayerCoordinate(playerOneLocation.Row, playerTwoLocation.Col);
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
                        && (board[start.Row - 3, start.Col] == WALL || board[start.Row, start.Col + 3] == WALL);
                }
                else if (destination.Row == start.Row - 2 && destination.Col == start.Col - 2) // NW
                {
                    targetOppRow = start.Row - 2;
                    targetOppoCol = start.Col - 2;
                    diagonalJump = 
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row - 3, start.Col] == WALL || board[start.Row, start.Col - 3] == WALL);
                }
                else if (destination.Row == start.Row + 2 && destination.Col == start.Col - 2) // SW
                {
                    targetOppRow = start.Row + 2;
                    targetOppoCol = start.Col - 2;
                    diagonalJump = 
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row + 3, start.Col] == WALL || board[start.Row, start.Col - 3] == WALL);
                }
                else if (destination.Row == start.Row + 2 && destination.Col == start.Col + 2) // SE 
                {
                    targetOppRow = start.Row + 2;
                    targetOppoCol = start.Col + 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && (board[start.Row + 3, start.Col] == WALL || board[start.Row, start.Col + 3] == WALL);
                }
            }

            return overJump || diagonalJump;
        }

        private Tuple<int, int> FindMidpoint(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return new Tuple<int, int>((start.Row + destination.Row) / 2, (start.Col + destination.Col) / 2);
        }

    }
}
