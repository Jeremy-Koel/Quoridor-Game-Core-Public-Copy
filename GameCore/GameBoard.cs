using System;
using System.Collections.Generic;

namespace GameCore
{
    class GameBoard
    {
        private static char EMPTY = '#';
        private static char PLAYER_1 = '1';
        private static char PLAYER_2 = '2';
        private static int COLS = 9;
        private static int ROWS = 9;

        private static GameBoard instance;

        private PlayerCoordinate playerOneLocation;
        private PlayerCoordinate playerTwoLocation;
        private char[,] board;
        private bool[,] walls;
        private bool gameOver;

        public bool GameOver { get => gameOver; set => gameOver = value; }

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

            // init gameboard 
            board = new char[ROWS, COLS];
            for (int r = 0; r < ROWS; ++r)
            {
                for (int c = 0; c < COLS; ++c)
                {
                    board[r,c] = EMPTY;
                }
            }
            playerOneLocation = new PlayerCoordinate(0, 4);
            playerTwoLocation = new PlayerCoordinate(8, 4);
            board[playerOneLocation.Row, playerOneLocation.Col] = PLAYER_1;
            board[playerTwoLocation.Row, playerTwoLocation.Col] = PLAYER_2;

            // init wall array 
            walls = new bool[ROWS+1, COLS+1];

        }

        public bool MovePiece(PlayerEnum player, PlayerCoordinate destinationCoordinate)
        {
            bool bReturn = false;

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

            if (IsValidMove(startCoordinate, destinationCoordinate))
            {
                board[startCoordinate.Row, startCoordinate.Col] = EMPTY;
                switch (player)
                {
                    case PlayerEnum.ONE:
                        playerOneLocation.Row = destinationCoordinate.Row;
                        playerOneLocation.Col = destinationCoordinate.Col;
                        board[playerOneLocation.Row, playerOneLocation.Col] = PLAYER_1;
                        break;
                    case PlayerEnum.TWO:
                        playerTwoLocation.Row = destinationCoordinate.Row;
                        playerTwoLocation.Col = destinationCoordinate.Col;
                        board[playerTwoLocation.Row, playerTwoLocation.Col] = PLAYER_2;
                        break;
                }
                bReturn = true;
            }

            // TODO - check for win 

            return bReturn;
        }

        public bool PlaceWall(PlayerEnum player, WallCoordinate wallCoordinate)
        {
            throw new Exception("Not yet implemented");
        }

        public void PrintBoard()
        {
            for (int r = 0; r < ROWS; ++r)
            {
                for (int c = 0; c < COLS; ++c)
                {
                    Console.Write(board[r, c]);
                }
                Console.Write("\n");
            }
        }

        private bool IsValidMove(PlayerCoordinate start, PlayerCoordinate destination)
        {
            bool inBounds = destination.Row >= 0
                && destination.Row < ROWS
                && destination.Col >= 0
                && destination.Col < COLS;

            bool adjacent = Math.Abs(destination.Row - start.Row) <= 1
                && Math.Abs(destination.Col - start.Col) <= 1;

            bool destinationIsEmpty = board[destination.Row, destination.Col] == EMPTY;

            return inBounds && adjacent && destinationIsEmpty;
        }

    }
}
