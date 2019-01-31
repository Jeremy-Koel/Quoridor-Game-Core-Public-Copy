using System;
using System.Collections.Generic;

namespace GameCore
{
    class GameBoard
    {
        public static char SPACE = '#';
        public static char WALL = '*';
        public static char NO_WALL = ' ';
        public static char PLAYER_1 = '1';
        public static char PLAYER_2 = '2';
        public static int TOTAL_ROWS = 17;
        public static int TOTAL_COLS = 17;

        private static GameBoard instance;

        private PlayerCoordinate playerOneLocation;
        private PlayerCoordinate playerTwoLocation;
        private char[,] board;

        public bool GameOver { get; set; }
        public bool PlayerOneWin { get; set; }
        public bool PalyerTwoWin { get; set; }

        public enum PlayerEnum
        {
            ONE, TWO
        }

        public static GameBoard GetInstance()
        {
            if (instance == null)
            {
                instance = new GameBoard("e1","e9");
            }
            return instance;
        }

        public static GameBoard GetInstance(string playerOneStart, string playerTwoStart)
        {
            if (instance == null)
            {
                instance = new GameBoard(playerOneStart, playerTwoStart);
            }
            return instance;
        }

        private GameBoard(string playerOneStart, string playerTwoStart)
        {
            GameOver = false;
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
                        board[r, c] = SPACE;
                    }
                    else
                    {
                        board[r, c] = NO_WALL;
                    }
                }
            }
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

        public bool MovePiece(PlayerEnum player, PlayerCoordinate destinationCoordinate)
        {
            if (GameOver)
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
                board[startCoordinate.Row, startCoordinate.Col] = SPACE;
                switch (player)
                {
                    case PlayerEnum.ONE:
                        playerOneLocation.Row = destinationCoordinate.Row;
                        playerOneLocation.Col = destinationCoordinate.Col;
                        break;
                    case PlayerEnum.TWO:
                        playerTwoLocation.Row = destinationCoordinate.Row;
                        playerTwoLocation.Col = destinationCoordinate.Col;
                        break;
                }
                retValue = true;
            }

            // check for win 
            if (playerOneLocation.Row == 0)
            {
                PlayerOneWin = true;
            }
            if (playerTwoLocation.Row == TOTAL_ROWS)
            {
                PalyerTwoWin = true;
            }
            GameOver = PlayerOneWin || PalyerTwoWin;

            return retValue;
        }

        public bool PlaceWall(PlayerEnum player, WallCoordinate wallCoordinate)
        {
            if (GameOver)
            {
                return false;
            }

            if (IsValidWallPlacement(wallCoordinate) && CanPlayersReachGoal(wallCoordinate))
            {
                board[wallCoordinate.StartRow, wallCoordinate.StartCol] = board[wallCoordinate.EndRow, wallCoordinate.EndCol] = WALL;
                return true;
            }

            return false;
        }

        private bool CanPlayersReachGoal(WallCoordinate wallCoordinate)
        {
            // Make a copy of the board, we don't want to change the original yet 
            char[,] copy = board.Clone() as char[,];
            copy[wallCoordinate.StartRow, wallCoordinate.StartCol] = copy[wallCoordinate.EndRow, wallCoordinate.EndCol] = WALL;

            bool canPlayerOneReachGoal = false;
            bool canPlayerTwoReachGoal = false;
            canPlayerOneReachGoal = BoardUtil.CanReachGoal(copy, 0, playerOneLocation.Row, playerOneLocation.Col);
            canPlayerTwoReachGoal = BoardUtil.CanReachGoal(copy, 16, playerTwoLocation.Row, playerTwoLocation.Col);
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
            bool isEmpty = false;
            if (onBoard)
            {
                isEmpty = IsEmptyWallSpace(wall.StartRow, wall.StartCol) 
                       && IsEmptyWallSpace(wall.EndRow, wall.EndCol);
            }
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
            return board[row, col] == NO_WALL;
        }

        private bool IsValidPlayerMove(PlayerEnum player, PlayerCoordinate start, PlayerCoordinate destination)
        {
            bool inBounds = IsMoveInBounds(destination.Row, destination.Col);
            if (!inBounds)
            {
                return false;
            }

            bool onSpace = IsMoveOnSpace(destination);
            bool notBlocked = !IsMoveBlocked(start, destination);
            bool destinationEmpty = IsDestinationEmpty(start, destination);

            bool adjacent = (IsDestinationAdjacent(start, destination));
            bool validJump = (IsValidJump(player, start, destination));
            bool canReach = adjacent || validJump;

            return destinationEmpty 
                && onSpace
                && notBlocked
                && canReach;
        }

        private bool IsMoveInBounds(int row, int col)
        {
            return row >= 0
                && row < TOTAL_ROWS
                && col >= 0
                && col < TOTAL_COLS;
        }

        private bool IsMoveOnSpace(PlayerCoordinate destination)
        {
            return destination.Row % 2 == 0  // odd rows are walls 
                && destination.Col % 2 == 0; // odd cols are walls 
        }

        private bool IsDestinationAdjacent(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return (Math.Abs(destination.Row - start.Row) == 2 || Math.Abs(destination.Row - start.Row) == 0)
                && (Math.Abs(destination.Col - start.Col) == 2 || Math.Abs(destination.Col - start.Col) == 0);
        }

        private bool IsDestinationEmpty(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return board[destination.Row, destination.Col] == SPACE;
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

            // TODO - deal with diagonal jumps 
            return midRow == opponentRow
                && midCol == opponentCol
                && (Math.Abs(destination.Row - start.Row) == 4 || Math.Abs(destination.Col - start.Col) == 4);
        }

        private Tuple<int, int> FindMidpoint(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return new Tuple<int, int>((start.Row + destination.Row) / 2, (start.Col + destination.Col) / 2);
        }

    }
}
