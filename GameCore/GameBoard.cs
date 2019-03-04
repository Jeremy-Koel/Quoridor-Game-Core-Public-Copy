using System;
using System.Collections.Generic;
using System.Text;

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
        private List<WallCoordinate> walls;
        private List<List<string>> possibleMoves;
        private List<string> possibleWalls;
        private int player1walls = 10;
        private int player2walls = 10;
        private char[,] board;
        private bool gameOver;
        private bool playerOneWin;
        private bool playerTwoWin;
        private PlayerEnum whoseTurn;


        public enum PlayerEnum
        {
            ONE, TWO
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

        public GameBoard(GameBoard boardState)
        {
            gameOver = false;
            playerOneWin = false;
            playerTwoWin = false;

            playerOneLocation = new PlayerCoordinate(boardState.playerOneLocation.Row, boardState.playerOneLocation.Col);
            playerTwoLocation = new PlayerCoordinate(boardState.playerTwoLocation.Row, boardState.playerTwoLocation.Col);

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
                        if (boardState.board[r, c].Equals(WALL))
                        {
                            board[r, c] = WALL;
                        }
                        else
                        {
                            board[r, c] = WALL_SPACE;
                        }
                    }
                }
            }
        }

        public List<WallCoordinate> GetWalls()
        {
            return walls;
        }

        public int GetPlayerWallCount(PlayerEnum player)
        {
            int wallCount = 10;

            switch (player)
            {
                case PlayerEnum.ONE:
                    wallCount = player1walls;
                    break;
                case PlayerEnum.TWO:
                    wallCount = player2walls;
                    break;
            }

            return wallCount;
        }

        public PlayerCoordinate GetPlayerCoordinate(int player)
        {
            if (player == 1)
            {
                return playerOneLocation;
            }
            else
            {
                return playerTwoLocation;
            }
        }

        public PlayerCoordinate GetPlayerCoordinate(PlayerEnum player)
        {
            PlayerCoordinate playerCoordinate = null;

            switch (player)
            {
                case PlayerEnum.ONE:
                    playerCoordinate = playerOneLocation;
                    break;
                case PlayerEnum.TWO:
                    playerCoordinate = playerTwoLocation;
                    break;
            }

            return playerCoordinate;
        }

        public int GetWhoseTurn()
        {
            int currentPlayer = 0;
            if (whoseTurn == PlayerEnum.ONE)
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

            possibleWalls = new List<string>();
            playerOneLocation = new PlayerCoordinate(playerOneStart);
            playerTwoLocation = new PlayerCoordinate(playerTwoStart);
            walls = new List<WallCoordinate>();
            possibleMoves = new List<List<string>>();

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

            for (int r = 0; r < 8; ++r)
            {
                for (int c = 0; c < 8; ++c)
                {
                    possibleWalls.Add((Convert.ToChar('a' + c)).ToString() + (Convert.ToChar('1' + r)).ToString());
                }
            }

            PlayerEnum actualStartingPlayer = whoseTurn;

            whoseTurn = PlayerEnum.ONE;

            possibleMoves.Add(GetPossibleMovesFromPosition());

            whoseTurn = PlayerEnum.TWO;

            possibleMoves.Add(GetPossibleMovesFromPosition());

            whoseTurn = actualStartingPlayer;
        }

        public bool PlayerOneWin()
        {
            return playerOneWin;
        }

        public bool PlayerTwoWin()
        {
            return playerTwoWin;
        }

        public bool IsGameOver()
        {
            return gameOver;
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

        private void ChangeTurn()
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

            string move = Convert.ToChar(97 + (destinationCoordinate.Col / 2)).ToString() + (9 - destinationCoordinate.Row / 2).ToString();

            if (possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1].Contains(move)/*IsValidPlayerMove(player, startCoordinate, destinationCoordinate)*/)
            {
                board[startCoordinate.Row, startCoordinate.Col] = PLAYER_SPACE;
                switch (player)
                {
                    case PlayerEnum.ONE:
                        playerOneLocation.Row = destinationCoordinate.Row;
                        playerOneLocation.Col = destinationCoordinate.Col;
                        possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1] = GetPossibleMovesFromPosition();
                        whoseTurn = PlayerEnum.TWO;
                        possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1] = GetPossibleMovesFromPosition();
                        break;
                    case PlayerEnum.TWO:
                        playerTwoLocation.Row = destinationCoordinate.Row;
                        playerTwoLocation.Col = destinationCoordinate.Col;
                        possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1] = GetPossibleMovesFromPosition();
                        whoseTurn = PlayerEnum.ONE;
                        possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1] = GetPossibleMovesFromPosition();
                        break;
                }
                retValue = true;
            }

            // check for win 
            if (playerOneLocation.Row == 0)
            {
                playerOneWin = true;
            }
            if (playerTwoLocation.Row == (TOTAL_ROWS - 1))
            {
                playerTwoWin = true;
            }
            gameOver = playerOneWin || playerTwoWin;

            return retValue;
        }

        public bool PlaceWall(PlayerEnum player, WallCoordinate wallCoordinate)
        {
            string wallString;
            if (wallCoordinate.Orientation == WallCoordinate.WallOrientation.Horizontal)
            {
                wallString = Convert.ToChar(97 + wallCoordinate.StartCol / 2) + (9 - (wallCoordinate.StartRow + 1) / 2).ToString();
            }
            else
            {
                wallString = Convert.ToChar(97 + (wallCoordinate.StartCol - 1) / 2) + (9 - wallCoordinate.StartRow / 2).ToString();
            }


            if (gameOver || whoseTurn != player)
            {
                return false;
            }
            if (player == PlayerEnum.ONE && player1walls <= 0)
            {
                return false;
            }
            else if (player == PlayerEnum.TWO && player2walls <= 0)
            {
                return false;
            }
            if (possibleWalls.Contains(wallString)/*IsValidWallPlacement(wallCoordinate)*/ && CanPlayersReachGoal(wallCoordinate))
            {
                walls.Add(wallCoordinate);
                board[wallCoordinate.StartRow, wallCoordinate.StartCol] = board[wallCoordinate.EndRow, wallCoordinate.EndCol] = WALL;
                possibleWalls.Remove(wallString);
                if (player == PlayerEnum.ONE)
                {
                    --player1walls;
                }
                else if (player == PlayerEnum.TWO)
                {
                    --player2walls;
                }
                // Mark that this player has taken their turn 
                possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1] = GetPossibleMovesFromPosition();
                ChangeTurn();
                possibleMoves[whoseTurn == PlayerEnum.ONE ? 0 : 1] = GetPossibleMovesFromPosition();
                return true;
            }

            return false;
        }

        private void GeneratePossibleHorizontalDiagonalJumps(List<string> validMoves, int direction)
        {
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 1 < 17 && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 1 > -1
                       && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 2 * direction < 17 && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 2 * direction > -1)
            {
                if (board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 1, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 2 * direction] != WALL)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2)));
                    sb.Append(value: 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) + 1 > 9 ? 9
                                   : 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) + 1);

                    validMoves.Add(sb.ToString());
                }
                if (board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 1, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 2 * direction] != WALL)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2)));
                    sb.Append(value: 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) - 1 < 1 ? 1
                                   : 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) - 1);

                    validMoves.Add(sb.ToString());
                }
            }
        }

        private void GeneratePossibleHorizontalJumps(List<string> validMoves, int direction)
        {
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + (3 * direction) < 17 && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + (3 * direction) > -1)
            {
                if (board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + (3 * direction)] != WALL)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) + (1 * direction) > 105 ? 105
                                            : 97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) + (1 * direction) < 97 ? 97
                                            : 97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) + (1 * direction)));
                    sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2));

                    validMoves.Add(sb.ToString());
                }
                else
                {
                    GeneratePossibleHorizontalDiagonalJumps(validMoves, direction);
                }
            }
            else
            {
                GeneratePossibleHorizontalDiagonalJumps(validMoves, direction);
            }
        }

        private void GeneratePossibleVerticalDiagonalJumps(List<string> validMoves, int direction)
        {
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 1 < 17 && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 1 > -1
                        && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 2 * direction < 17 && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 2 * direction > -1)
            {
                if (board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 2 * direction, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 1] != WALL)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) + 1 > 105 ? 105
                                                  : 97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) + 1));
                    sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2));

                    validMoves.Add(sb.ToString());
                }
                if (board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 2 * direction, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 1] != WALL)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) - 1 < 97 ? 97
                                                  : 97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2) - 1));
                    sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2));

                    validMoves.Add(sb.ToString());
                }
            }
        }

        private void GeneratePossibleVerticalJumps(List<string> validMoves, int direction)
        {
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + (3 * direction) < 17 && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + (3 * direction) > -1)
            {
                if (board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + (3 * direction), (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col] != WALL)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col / 2)));
                    sb.Append(value: 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) - (1 * direction) > 9 ? 9
                                   : 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) - (1 * direction) < 1 ? 1
                                   : 9 - ((whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row / 2) - (1 * direction));

                    validMoves.Add(sb.ToString());
                }
                else
                {
                    GeneratePossibleVerticalDiagonalJumps(validMoves, direction);
                }
            }
            else
            {
                GeneratePossibleVerticalDiagonalJumps(validMoves, direction);
            }
        }


        private List<string> GetPossibleMovesFromPosition()
        {
            List<string> validMoves = new List<string>();

            if (ArePlayersAdjacent())
            {
                if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row)
                {
                    if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col < (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col)
                    {
                        GeneratePossibleHorizontalJumps(validMoves, 1);
                    }
                    else
                    {
                        GeneratePossibleHorizontalJumps(validMoves, -1);
                    }
                }
                else
                {
                    if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row < (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row)
                    {
                        GeneratePossibleVerticalJumps(validMoves, 1);
                    }
                    else
                    {
                        GeneratePossibleVerticalJumps(validMoves, -1);
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 1 < 17 && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 1, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col] != WALL
                && ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 2 != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row || (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col))
            {
                //South
                sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col / 2)));
                sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row / 2) - 1 < 1 ? 1 : 9 - ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row / 2) - 1);
                validMoves.Add(sb.ToString());
            }
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 1 > -1 && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 1, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col] != WALL
                 && ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 2 != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row || (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col))
            {
                //North
                sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col / 2)));
                sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row / 2) + 1 > 9 ? 9 : 9 - ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row / 2) + 1);
                validMoves.Add(sb.ToString());
            }
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 1 < 17 && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 1] != WALL
                && ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row || (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col + 2 != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col))
            {
                //East
                sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col / 2) + 1));
                sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row / 2));
                validMoves.Add(sb.ToString());
            }
            if ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 1 > -1 && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 1] != WALL
                && ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row || (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 2 != (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col))
            {
                //West
                sb.Append(Convert.ToChar(97 + ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col / 2) - 1));
                sb.Append(9 - ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row / 2));
                validMoves.Add(sb.ToString());
            }


            validMoves.Sort(delegate (string lValue, string rValue)
            {
                if (lValue == rValue) return 0;
                else return lValue.CompareTo(rValue);
            });

            return validMoves;
        }

        private bool ArePlayersAdjacent()
        {
            return ((whoseTurn == PlayerEnum.ONE ? playerOneLocation : playerTwoLocation).Row == (whoseTurn == PlayerEnum.ONE ? playerTwoLocation : playerOneLocation).Row && (whoseTurn == PlayerEnum.ONE ? playerOneLocation : playerTwoLocation).Col + 2 == (whoseTurn == PlayerEnum.ONE ? playerTwoLocation : playerOneLocation).Col && board[(whoseTurn == PlayerEnum.ONE ? playerOneLocation : playerTwoLocation).Row, (whoseTurn == PlayerEnum.ONE ? playerOneLocation : playerTwoLocation).Col + 1] != WALL)
                || ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 2 == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row, ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col - 1)] != WALL)
                || ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 2 == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row + 1, ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col)] != WALL)
                || ((whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 2 == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Row && (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col == (whoseTurn == PlayerEnum.ONE ? (playerTwoLocation) : (playerOneLocation)).Col && board[(whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Row - 1, (whoseTurn == PlayerEnum.ONE ? (playerOneLocation) : (playerTwoLocation)).Col] != WALL);
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

        public bool IsValidWallPlacement(WallCoordinate wall)
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

        public bool IsValidPlayerMove(PlayerEnum player, PlayerCoordinate start, PlayerCoordinate destination)
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
                    blocked = (board[start.Row, start.Col + 1] == WALL) || (board[destination.Row, destination.Col - 1] == WALL);
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
                    blocked = (board[start.Row + 1, start.Col] == WALL) || (board[destination.Row - 1, destination.Col] == WALL);
                }
                else
                {
                    blocked = (board[start.Row - 1, start.Col] == WALL) || (board[destination.Row + 1, destination.Col] == WALL);
                }
            }
            return blocked;
        }

        private bool IsValidJump(PlayerEnum player, PlayerCoordinate start, PlayerCoordinate destination)
        {
            // Jumping over? 
            Tuple<int, int> midpoint = FindMidpoint(start, destination);
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
                        && ((start.Row - 3 == -1 || start.Col + 3 == 17) || (board[start.Row - 3, start.Col] == WALL || board[start.Row, start.Col + 3] == WALL));
                }
                else if (destination.Row == start.Row - 2 && destination.Col == start.Col - 2) // NW
                {
                    targetOppRow = start.Row - 2;
                    targetOppoCol = start.Col - 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && ((start.Row - 3 == -1 || start.Col - 3 == -1) || (board[start.Row - 3, start.Col] == WALL || board[start.Row, start.Col - 3] == WALL));
                }
                else if (destination.Row == start.Row + 2 && destination.Col == start.Col - 2) // SW
                {
                    targetOppRow = start.Row + 2;
                    targetOppoCol = start.Col - 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && ((start.Row + 3 == 17 || start.Col - 3 == -1) || (board[start.Row + 3, start.Col] == WALL || board[start.Row, start.Col - 3] == WALL));
                }
                else if (destination.Row == start.Row + 2 && destination.Col == start.Col + 2) // SE 
                {
                    targetOppRow = start.Row + 2;
                    targetOppoCol = start.Col + 2;
                    diagonalJump =
                        ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                        && ((start.Row + 3 == 17 || start.Col + 3 == 17) || (board[start.Row + 3, start.Col] == WALL || board[start.Row, start.Col + 3] == WALL));
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
