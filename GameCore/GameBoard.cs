using System;
using System.Collections.Generic;

namespace GameCore
{
    class GameBoard
    {
        private static char SPACE = '#';
        private static char WALL = '*';
        private static char PLAYER_1 = '1';
        private static char PLAYER_2 = '2';
        private static int TOTAL_ROWS = 17;
        private static int TOTAL_COLS = 17;

        private static GameBoard instance;

        private PlayerCoordinate playerOneLocation;
        private PlayerCoordinate playerTwoLocation;
        private char[,] board;
        private bool gameOver;
        private bool playerOneWin;
        private bool playerTwoWin;

        public bool GameOver { get => gameOver; set => gameOver = value; }
        public bool PlayerOneWin { get => playerOneWin; set => playerOneWin = value; }
        public bool PalyerTwoWin { get => playerTwoWin; set => playerTwoWin = value; }

        public enum PlayerEnum
        {
            ONE, TWO
        }

        public static GameBoard GetInstance()
        {
            if (instance == null)
            {
                instance = new GameBoard();
            }
            return instance;
        }

        private GameBoard()
        {
            gameOver = false;
            playerOneLocation = new PlayerCoordinate("e1");
            playerTwoLocation = new PlayerCoordinate("e9");

            // init gameboard 
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
                        board[r, c] = ' ';
                    }
                }
            }
        }

        public bool MovePiece(PlayerEnum player, PlayerCoordinate destinationCoordinate)
        {
            if (gameOver)
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

            if (IsValidPlayerMove(startCoordinate, destinationCoordinate))
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
                playerOneWin = true;
            }
            if (playerTwoLocation.Row == 16)
            {
                playerTwoWin = true;
            }
            gameOver = playerOneWin || playerTwoWin;

            return retValue;
        }

        public bool PlaceWall(PlayerEnum player, WallCoordinate wallCoordinate)
        {
            if (gameOver)
            {
                return false;
            }

            bool retValue = false;
            if (IsValidWallPlacement(wallCoordinate))
            {
                // TODO - check to ensure player has path to goal 

                retValue = true;
                board[wallCoordinate.StartRow, wallCoordinate.StartCol] = board[wallCoordinate.EndRow, wallCoordinate.EndCol] = WALL;
            }

            return retValue;
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

        private bool IsValidWallPlacement(WallCoordinate wall)
        {
            bool onBoard = IsMoveInBounds(wall.StartRow, wall.StartCol) 
                        && IsMoveInBounds(wall.EndRow, wall.EndCol);
            bool onWallSpace = IsOddSpace(wall.StartRow, wall.StartCol, wall.Orientation) 
                            && IsOddSpace(wall.EndRow, wall.EndCol, wall.Orientation);
            bool isEmpty = false;
            if (onBoard)
            {
                isEmpty = IsEmptyWallSpace(wall.StartRow, wall.StartCol) 
                       && IsEmptyWallSpace(wall.EndRow, wall.EndCol);
            }
            return onBoard 
                && onWallSpace 
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
            return board[row, col] == ' ';
        }

        private bool IsValidPlayerMove(PlayerCoordinate start, PlayerCoordinate destination)
        {
            bool inBounds = IsMoveInBounds(destination.Row, destination.Col);
            bool onSpace = IsMoveOnSpace(destination);
            bool destinationEmpty = IsDestinationEmpty(start, destination);

            bool canReach = false;
            if (IsDestinationAdjacent(start, destination))
            {
                canReach = true;
            }
            else // is it a jump? 
            {
                // TODO - validate jumps 
            }
            return inBounds 
                && destinationEmpty 
                && onSpace
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

    }
}
