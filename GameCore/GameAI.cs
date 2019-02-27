﻿#define DEBUG 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;


namespace GameCore
{

    /// <summary>
    /// Class Name: MonteCarloNode
    /// Description: MonteCarloNode is a node to be used in the building of a Monte Carlo Search Tree. The constructor for a Monte Carlo node
    /// either having it start at e9 or can be given coordinates of where to start along, and may also be passed a List of WallCoordinates.
    /// </summary>
    class MonteCarloNode : IComparable<MonteCarloNode>
    {

        private static readonly object boardAccess = new object();
        private readonly object childrenAccess = new object();
        private readonly object childrenMovesAccess = new object();
        private static double explorationFactor = 1.0 / FastSqRt(2.0);

        private static double FastSqRt(double value)
        {
            return BitConverter.Int64BitsToDouble(((long)((-1 * ((int)(BitConverter.DoubleToInt64Bits(Rsqrt(value)) >> 32) - 1072632447)) + 1072632447)) << 32);
        }

        private static double historyInfluence = 1.15;

        private static string MonteCarloPlayer;
        private static Random randomPercentileChance;
        private static List<BitArray> board;
        private static int[,] possibleMoveValues;
        private static Dictionary<string, Tuple<double, double>> moveTotals;

        private List<MonteCarloNode> children;
        private List<WallCoordinate> walls;
        private List<Tuple<string, double>> possibleMoves;
        private List<string> childrensMoves;
        private MonteCarloNode parent;
        private List<PlayerCoordinate> playerLocations;
        private List<int> wallsRemaining;

        private double score;
        private double wins;
        private double timesVisited;
        private bool gameOver;
        private GameBoard.PlayerEnum turn;

        private int depthCheck = 0;
        public static int TOTAL_ROWS = 17;
        public static int TOTAL_COLS = 17;
        private string thisMove;

        public List<BitArray> GetBoard()
        {
            return board;
        }

        public string GetMove()
        {
            return thisMove;
        }

        public double GetWins()
        {
            return wins;
        }

        public double GetVisits()
        {
            return timesVisited;
        }

        public double GetScore()
        {
            return score;
        }

        private void SetScore(double v)
        {
            score = v;
        }

        private double GetWinRate()
        {
            return wins / timesVisited;
        }

        public int CompareTo(MonteCarloNode carloNode)
        {
            // A null value means that this object is greater.
            if (carloNode == null)
            {
                return 1;
            }
            else if (GetVisits() > carloNode.GetVisits())
            {
                return 1;
            }
            else if (GetVisits() < carloNode.GetVisits())
            {
                return -1;
            }
            else
            {
                return 0;
            }

        }

        public MonteCarloNode(PlayerCoordinate playerOne, PlayerCoordinate playerTwo, int playerOneTotalWalls, int playerTwoTotalWalls, List<WallCoordinate> wallCoordinates, GameBoard.PlayerEnum currentTurn)
        {
            board = new List<BitArray>();
            moveTotals = new Dictionary<string, Tuple<double, double>>();
            possibleMoveValues = new int[9, 9];

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    possibleMoveValues[r, c] = 0;
                }
            }

            for (int i = 0; i < TOTAL_ROWS; i++)
            {
                board.Add(new BitArray(17));
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

            walls = new List<WallCoordinate>(wallCoordinates);

            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();

            randomPercentileChance = new Random();

            wins = 0;
            timesVisited = 0;
            turn = currentTurn;
            possibleMoves = PossibleMovesFromPosition();

#if DEBUG
            if (possibleMoves.Count == 0)
            {
                Console.WriteLine("Abort");
            }
#endif

            gameOver = false;
            parent = null;
        }

        private MonteCarloNode(string move, List<PlayerCoordinate> players, List<int> wallCounts, List<WallCoordinate> wallCoordinates, WallCoordinate newWallCoordinate, GameBoard.PlayerEnum currentTurn, int depth, MonteCarloNode childParent)
        {
            turn = currentTurn;
            parent = childParent;
            board = childParent.GetBoard();
            thisMove = move;
            depthCheck = depth;
            wins = 0;
            timesVisited = 0;

            playerLocations = new List<PlayerCoordinate>(players);

            wallsRemaining = new List<int>(wallCounts);

            walls = new List<WallCoordinate>(wallCoordinates);

            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();


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

            possibleMoves = PossibleMovesFromPosition();
#if DEBUG
            if (possibleMoves.Count == 0)
            {
                Console.WriteLine("Abort");
            }
#endif
        }

        private MonteCarloNode(string move, int depth, MonteCarloNode childParent)
        {
            parent = childParent;
            board = childParent.GetBoard();
            thisMove = move;
            depthCheck = depth;
            wins = 0;
            timesVisited = 0;

            playerLocations = new List<PlayerCoordinate>();
            playerLocations.Add(new PlayerCoordinate(parent.playerLocations[0].Row, parent.playerLocations[0].Col));
            playerLocations.Add(new PlayerCoordinate(parent.playerLocations[1].Row, parent.playerLocations[1].Col));

            wallsRemaining = new List<int>(childParent.wallsRemaining);

            walls = new List<WallCoordinate>(childParent.walls);

            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();

            PlayerCoordinate destinationCoordinate = new PlayerCoordinate(move);


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

            possibleMoves = PossibleMovesFromPosition();
#if DEBUG
            if (possibleMoves.Count == 0)
            {
                Console.WriteLine("Abort");
            }
#endif
        }

        private void PossibleHorizontalDiagonalJumps(List<Tuple<string, double>> validMoves, int direction)
        {
            if (playerLocations[turn == 0 ? 0 : 1].Row + 1 < 17 && playerLocations[turn == 0 ? 0 : 1].Row - 1 > -1
                       && playerLocations[turn == 0 ? 0 : 1].Col + 2 * direction < 17 && playerLocations[turn == 0 ? 0 : 1].Col + 2 * direction > -1)
            {
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row - 1].Get(playerLocations[turn == 0 ? 0 : 1].Col + 2 * direction))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2)));
                    sb.Append(value: 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) + 1 > 9 ? 9
                                   : 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) + 1);

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row + 1].Get(playerLocations[turn == 0 ? 0 : 1].Col + 2 * direction))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2)));
                    sb.Append(value: 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - 1 < 1 ? 1
                                   : 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - 1);

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
            }
        }

        private void PossibleHorizontalJumps(List<Tuple<string, double>> validMoves, int direction)
        {
            if (playerLocations[turn == 0 ? 0 : 1].Col + (3 * direction) < 17 && playerLocations[turn == 0 ? 0 : 1].Col + (3 * direction) > -1)
            {
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row].Get(playerLocations[turn == 0 ? 0 : 1].Col + (3 * direction)))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) + (1 * direction) > 105 ? 105
                                            : 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) + (1 * direction) < 97 ? 97
                                            : 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) + (1 * direction)));
                    sb.Append(9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                else
                {
                    PossibleHorizontalDiagonalJumps(validMoves, direction);
                }
            }
            else
            {
                PossibleHorizontalDiagonalJumps(validMoves, direction);
            }
        }

        private void PossibleVerticalDiagonalJumps(List<Tuple<string, double>> validMoves, int direction)
        {
            if (playerLocations[turn == 0 ? 0 : 1].Col + 1 < 17 && playerLocations[turn == 0 ? 0 : 1].Col - 1 > -1
                        && playerLocations[turn == 0 ? 0 : 1].Row + 2 * direction < 17 && playerLocations[turn == 0 ? 0 : 1].Row + 2 * direction > -1)
            {
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row + 2 * direction].Get(playerLocations[turn == 0 ? 0 : 1].Col + 1))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) + 1 > 105 ? 105
                                                  : 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) + 1));
                    sb.Append(9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row + 2 * direction].Get(playerLocations[turn == 0 ? 0 : 1].Col - 1))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) - 1 < 97 ? 97
                                                  : 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) - 1));
                    sb.Append(9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
            }
        }

        private void PossibleVerticalJumps(List<Tuple<string, double>> validMoves, int direction)
        {
            if (playerLocations[turn == 0 ? 0 : 1].Row + (3 * direction) < 17 && playerLocations[turn == 0 ? 0 : 1].Row + (3 * direction) > -1)
            {
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row + (3 * direction)].Get(playerLocations[turn == 0 ? 0 : 1].Col))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2)));
                    sb.Append(value: 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (1 * direction) > 9 ? 9
                                   : 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (1 * direction) < 1 ? 1
                                   : 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (1 * direction));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                else
                {
                    PossibleVerticalDiagonalJumps(validMoves, direction);
                }
            }
            else
            {
                PossibleVerticalDiagonalJumps(validMoves, direction);
            }
        }


        private List<Tuple<string, double>> PossibleMovesFromPosition()
        {
            List<Tuple<string, double>> validMoves = new List<Tuple<string, double>>();

            int currentPlayer = turn == 0 ? 0 : 1;
            int opponent = turn == 0 ? 1 : 0;
            lock (boardAccess)
            {
                Populate();
                if (PlayersAreAdjacent())
                {
                    if (playerLocations[currentPlayer].Row == playerLocations[opponent].Row)
                    {
                        if (playerLocations[currentPlayer].Col < playerLocations[opponent].Col)
                        {
                            PossibleHorizontalJumps(validMoves, 1);
                        }
                        else
                        {
                            PossibleHorizontalJumps(validMoves, -1);
                        }
                    }
                    else
                    {
                        if (playerLocations[currentPlayer].Row < playerLocations[opponent].Row)
                        {
                            PossibleVerticalJumps(validMoves, 1);
                        }
                        else
                        {
                            PossibleVerticalJumps(validMoves, -1);
                        }
                    }
                }
                if (playerLocations[currentPlayer].Row + 1 < 17 && !board[playerLocations[currentPlayer].Row + 1].Get(playerLocations[currentPlayer].Col)
                    && (playerLocations[currentPlayer].Row + 2 != playerLocations[opponent].Row || playerLocations[currentPlayer].Col != playerLocations[opponent].Col))
                {
                    //South
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2)));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2) - 1 < 1 ? 1 : 9 - (playerLocations[currentPlayer].Row / 2) - 1);
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                if (playerLocations[currentPlayer].Row - 1 > -1 && !board[playerLocations[currentPlayer].Row - 1].Get(playerLocations[currentPlayer].Col)
                     && (playerLocations[currentPlayer].Row - 2 != playerLocations[opponent].Row || playerLocations[currentPlayer].Col != playerLocations[opponent].Col))
                {
                    //North
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2)));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2) + 1 > 9 ? 9 : 9 - (playerLocations[currentPlayer].Row / 2) + 1);
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                if (playerLocations[currentPlayer].Col + 1 < 17 && !board[playerLocations[currentPlayer].Row].Get(playerLocations[currentPlayer].Col + 1)
                    && (playerLocations[currentPlayer].Row != playerLocations[opponent].Row || playerLocations[currentPlayer].Col + 2 != playerLocations[opponent].Col))
                {
                    //East
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2) + 1));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2));
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }
                if (playerLocations[currentPlayer].Col - 1 > -1 && !board[playerLocations[currentPlayer].Row].Get(playerLocations[currentPlayer].Col - 1)
                    && (playerLocations[currentPlayer].Row != playerLocations[opponent].Row || playerLocations[currentPlayer].Col - 2 != playerLocations[opponent].Col))
                {
                    //West
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2) - 1));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2));
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString())));
                }


                Unpopulate();


                validMoves.Sort(delegate (Tuple<string, double> lValue, Tuple<string, double> rValue)
                {
                    if (lValue.Item2 == rValue.Item2) return 0;
                    else return lValue.Item2.CompareTo(rValue.Item2);
                });

                return validMoves;
            }
        }

        private int FindMove(string move)
        {
            int indexOfMove = randomPercentileChance.Next(0, childrensMoves.Count);

            for (int i = 0; i < childrensMoves.Count; i++)
            {
                if (childrensMoves[i].Equals(move))
                {
                    indexOfMove = i;
                }
            }

            return indexOfMove;
        }


        public List<MonteCarloNode> GetChildrenNodes()
        {
            return children;
        }

        public List<string> GetChildrenMoves()
        {
            return childrensMoves;
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

            if (WallIsAdjacentToCurrentlyPlacedWalls(wallCoordinate))
            {
                if (CanPlayersReachGoal(wallCoordinate))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }

        }

        private bool WallIsAdjacentToCurrentlyPlacedWalls(WallCoordinate wallToPlace)
        {
            lock (boardAccess)
            {
                Populate();

                bool adjacency = false;

                if (wallToPlace.Orientation == WallCoordinate.WallOrientation.Horizontal)
                {
                    adjacency = CheckForAdjacentWallsHorizontal(wallToPlace);
                }
                else
                {
                    adjacency = CheckForAdjacentWallsVertical(wallToPlace);
                }

                Unpopulate();

                return adjacency;
            }
        }

        private bool CheckForAdjacentWallsHorizontal(WallCoordinate wallToPlace)
        {
            Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallToPlace.StartRow, wallToPlace.StartCol), new PlayerCoordinate(wallToPlace.EndRow, wallToPlace.EndCol));

            if (wallToPlace.StartRow + 1 < 17 && wallToPlace.StartCol - 1 > -1 && board[wallToPlace.StartRow + 1].Get(wallToPlace.StartCol - 1))
            {
                return true;
            }
            if (wallToPlace.StartRow - 1 > -1 && wallToPlace.StartCol - 1 > -1 && board[wallToPlace.StartRow - 1].Get(wallToPlace.StartCol - 1))
            {
                return true;
            }
            if (mid.Item1 + 1 < 17 && board[mid.Item1 + 1].Get(mid.Item2))
            {
                return true;
            }
            if (mid.Item1 - 1 - 1 > -1 && board[mid.Item1 - 1].Get(mid.Item2))
            {
                return true;
            }
            if (wallToPlace.EndRow + 1 < 17 && wallToPlace.EndCol + 1 < 17 && board[wallToPlace.EndRow + 1].Get(wallToPlace.EndCol + 1))
            {
                return true;
            }
            if (wallToPlace.EndRow - 1 > -1 && wallToPlace.EndCol + 1 < 17 && board[wallToPlace.EndRow - 1].Get(wallToPlace.EndCol + 1))
            {
                return true;
            }

            return false;
        }

        private bool CheckForAdjacentWallsVertical(WallCoordinate wallToPlace)
        {
            Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallToPlace.StartRow, wallToPlace.StartCol), new PlayerCoordinate(wallToPlace.EndRow, wallToPlace.EndCol));

            if (wallToPlace.StartRow + 1 < 17 && wallToPlace.StartCol + 1 < 17 && board[wallToPlace.StartRow + 1].Get(wallToPlace.StartCol + 1))
            {
                return true;
            }
            if (wallToPlace.StartRow + 1 < 17 && wallToPlace.StartCol - 1 > -1 && board[wallToPlace.StartRow + 1].Get(wallToPlace.StartCol - 1))
            {
                return true;
            }
            if (mid.Item2 + 1 < 17 && board[mid.Item1].Get(mid.Item2 + 1))
            {
                return true;
            }
            if (mid.Item2 - 1 > -1 && board[mid.Item1].Get(mid.Item2 - 1))
            {
                return true;
            }
            if (wallToPlace.EndRow - 1 < -1 && wallToPlace.EndCol - 1 > -1 && board[wallToPlace.EndRow - 1].Get(wallToPlace.EndCol - 1))
            {
                return true;
            }
            if (wallToPlace.EndRow - 1 < -1 && wallToPlace.EndCol + 1 < 17 && board[wallToPlace.EndRow - 1].Get(wallToPlace.EndCol + 1))
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
            bool isEmpty = IsEmptyWallSpace(wall);
            return onWallSpace
                && isEmpty;
        }

        private bool CanPlayersReachGoal(WallCoordinate wallCoordinate)
        {
            lock (boardAccess)
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

        }

        private double MinimumHeuristicEstimate(string locationToStart)
        {
            int EndRow;
            double minimumHeuristic = double.PositiveInfinity;

            if (turn == 0)
            {
                EndRow = 9;
            }
            else
            {
                EndRow = 1;
            }

            PlayerCoordinate start = new PlayerCoordinate(locationToStart);

            for (int characterIndex = 0; characterIndex < 9; characterIndex++)
            {
                double possibleMinimumHeuristic = HeuristicCostEstimate(start, new PlayerCoordinate(Convert.ToChar(97 + characterIndex).ToString() + EndRow.ToString()));
                if (possibleMinimumHeuristic < minimumHeuristic)
                {
                    minimumHeuristic = possibleMinimumHeuristic;
                }
            }

            int moveValue = possibleMoveValues[start.Row / 2, start.Col / 2] / 2;

            return minimumHeuristic * (moveValue <= 1 ? 1 : moveValue);
        }

        private double HeuristicCostEstimate(PlayerCoordinate start, PlayerCoordinate goal)
        {
            return Math.Abs(start.Row - goal.Row) + Math.Abs(start.Col - goal.Col);
        }

        private PlayerCoordinate LowestCostNodeInOpenSet(HashSet<PlayerCoordinate> openSet, Dictionary<PlayerCoordinate, double> fScore)
        {
            double lowestCost = double.PositiveInfinity;
            PlayerCoordinate lowestCostNode = null;

            foreach (var node in openSet)
            {
                if (fScore[node] < lowestCost)
                {
                    lowestCost = fScore[node];
                    lowestCostNode = node;
                }
            }

            return lowestCostNode;
        }

        private double DistanceBetween(PlayerCoordinate currentPath, PlayerCoordinate neighbor)
        {
            double distance;
            if (ValidPlayerMove(currentPath, neighbor))
            {
                distance = 1;
            }
            else
            {
                distance = double.PositiveInfinity;
            }
            return distance;
        }

        // Fast Inverse Square Root Function
        public static double Rsqrt(double number)
        {
            long i;
            double x2, y;
            const float threehalfs = 1.5F;

            x2 = number * 0.5F;
            y = number;
            i = BitConverter.ToInt32(BitConverter.GetBytes(y), 0);                       // evil floating point bit level hacking
            i = 0x5f3759df - (i >> 1);
            y = BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
            y = y * (threehalfs - (x2 * y * y));


            return y;
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
                //#if DEBUG
                //                return false;
                //#else
                canReach = IsValidJump(turn, start, destination);
                //#endif
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

                    bool isEmpty;

                    lock (boardAccess)
                    {
                        Populate();

                        isEmpty = IsEmptyWallSpace(givenMove) && IsOppositeOrientationWallSpaceEmpty(givenMove);

                        Unpopulate();
                    }

                    return onWallSpace
                        && isEmpty;
                }
            }

            return validityOfWallPlacement;
        }

        private bool IsEmptyWallSpace(WallCoordinate givenMove)
        {
            return !(board[givenMove.StartRow].Get(givenMove.StartCol) || board[givenMove.EndRow].Get(givenMove.EndCol)) == true;
        }

        private bool IsOppositeOrientationWallSpaceEmpty(WallCoordinate givenMove)
        {
            return !(board[givenMove.StartRow - 1].Get(givenMove.StartCol + 1) && board[givenMove.EndRow + 1].Get(givenMove.EndCol - 1)) == true;
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

            lock (boardAccess)
            {
                Populate();

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

                Unpopulate();
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
            lock (boardAccess)
            {
                Populate();
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
                            && ((board[start.Row - 3 < 0 ? 0 : start.Row - 3].Get(start.Col) == true || board[start.Row].Get(start.Col + 3 > 16 ? 16 : start.Col + 3) == true) || (start.Row - 3 == -1 || start.Col + 3 == 17));
                    }
                    else if (destination.Row == start.Row - 2 && destination.Col == start.Col - 2) // NW
                    {
                        targetOppRow = start.Row - 2;
                        targetOppoCol = start.Col - 2;
                        diagonalJump =
                            ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                            && ((board[start.Row - 3 < 0 ? 0 : start.Row - 3].Get(start.Col) == true || board[start.Row].Get(start.Col - 3 < 0 ? 0 : start.Col - 3) == true) || (start.Row - 3 == -1 || start.Col - 3 == -1));
                    }
                    else if (destination.Row == start.Row + 2 && destination.Col == start.Col - 2) // SW
                    {
                        targetOppRow = start.Row + 2;
                        targetOppoCol = start.Col - 2;
                        diagonalJump =
                            ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                            && ((board[start.Row + 3 > 16 ? 16 : start.Row + 3].Get(start.Col) == true || board[start.Row].Get(start.Col - 3 < 0 ? 0 : start.Col - 3) == true) || (start.Row + 3 == 17 || start.Col - 3 == -1));
                    }
                    else if (destination.Row == start.Row + 2 && destination.Col == start.Col + 2) // SE 
                    {
                        targetOppRow = start.Row + 2;
                        targetOppoCol = start.Col + 2;
                        diagonalJump =
                            ((opponent.Row == targetOppRow && opponent.Col == start.Col) || (opponent.Row == start.Row && opponent.Col == targetOppoCol))
                            && ((board[start.Row + 3 > 16 ? 16 : start.Row + 3].Get(start.Col) == true || board[start.Row].Get(start.Col + 3 > 16 ? 16 : start.Col + 3) == true) || (start.Row + 3 == 17 || start.Col + 3 == 17));
                    }
                }
                Unpopulate();
                return overJump || diagonalJump;
            }

        }

        private Tuple<int, int> FindMidpoint(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return new Tuple<int, int>((start.Row + destination.Row) / 2, (start.Col + destination.Col) / 2);
        }

        private string RandomMove()
        {
            return randomPercentileChance.Next(1, 100) >= 11 + (10 * (turn == 0 ? (playerLocations[1].Row / 2) : (8 - playerLocations[0].Row /2 + 1))) ? FindPlayerMove() : (turn == 0 ? wallsRemaining[0] : wallsRemaining[1]) > 0 ? (turn == 0 ? playerLocations[1].Row / 2 <= 5 : playerLocations[0].Row / 2 >= 3) ? FindBlockingWall() : BoardUtil.GetRandomWallPlacementMove() : FindPlayerMove();
        }

        private string FindBlockingWall()
        {
            return PlaceBlockingWall()[randomPercentileChance.Next(0, 4)];
        }

        private List<string> PlaceBlockingWall()
        {
            List<string> blockingWalls = new List<string>();

            blockingWalls.Add(Convert.ToChar(97 + playerLocations[turn == 0 ? 1 : 0].Col / 2 + (playerLocations[turn == 0 ? 1 : 0].Col != 0 ? -1 : 0)).ToString() + (9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (turn == 0 ? 1 : 0)).ToString() + "h");
            blockingWalls.Add(Convert.ToChar(97 + playerLocations[turn == 0 ? 1 : 0].Col / 2 + (playerLocations[turn == 0 ? 1 : 0].Col != 0 ? -1 : 0)).ToString() + (9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (turn == 0 ? 1 : 0)).ToString() + "v");

            blockingWalls.Add(Convert.ToChar(97 + playerLocations[turn == 0 ? 1 : 0].Col / 2).ToString() + (9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (turn == 0 ? 1 : 0)).ToString() + "h");
            blockingWalls.Add(Convert.ToChar(97 + playerLocations[turn == 0 ? 1 : 0].Col / 2).ToString() + (9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - (turn == 0 ? 1 : 0)).ToString() + "v");

            return blockingWalls;
        }

        private string FindPlayerMove()
        {
            string move;

            List<string> blockingWalls = PlaceBlockingWall();

            if (randomPercentileChance.Next(1, 100) <= 11 || (playerLocations[turn == 0 ? 0 : 1].Row / 2) + (turn == 0 ? -1 : 1) == (turn == 0 ? 0 : 8) || (turn == 0 ? playerLocations[1].Row / 2 <= 5 : playerLocations[0].Row / 2 >= 3))
            {
                move = possibleMoves[0].Item1;

                for (int i = 1; childrensMoves.Contains(move) && i < possibleMoves.Count; ++i)
                {
                    move = possibleMoves[i].Item1;
                }

                if (childrensMoves.Contains(move))
                {
                    move = possibleMoves[randomPercentileChance.Next(0, possibleMoves.Count)].Item1;
                }
            }
            else if (wallsRemaining[turn == 0 ? 0: 1] != 0 && randomPercentileChance.Next(1, 100) <= 50 && new WallCoordinate(blockingWalls[0]).StartCol - 1 == playerLocations[turn == 0 ? 1 : 0].Col && ValidWallMove(blockingWalls[0]))
            {
                move = blockingWalls[0];
            }
            else if (wallsRemaining[turn == 0 ? 0 : 1] != 0 && randomPercentileChance.Next(1, 100) <= 50 && new WallCoordinate(blockingWalls[1]).StartCol - 1 == playerLocations[turn == 0 ? 1 : 0].Col && ValidWallMove(blockingWalls[1]))
            {
                move = blockingWalls[1];
            }
            else if (wallsRemaining[turn == 0 ? 0 : 1] != 0 && randomPercentileChance.Next(1, 100) <= 50 && ValidWallMove(blockingWalls[2]))
            {
                move = blockingWalls[2];
            }
            else if (wallsRemaining[turn == 0 ? 0 : 1] != 0 && ValidWallMove(blockingWalls[3]))
            {
                move = blockingWalls[3];
            }
            else
            {
                move = possibleMoves[0].Item1;

                for (int i = 1; childrensMoves.Contains(move) && i < possibleMoves.Count; ++i)
                {
                    move = possibleMoves[i].Item1;
                }

                if (childrensMoves.Contains(move))
                {
                    move = possibleMoves[randomPercentileChance.Next(0, possibleMoves.Count)].Item1;
                }
            }

            return move;
        }


        private Tuple<bool, string> GetValidJumpMove(List<PlayerCoordinate> players)
        {
            StringBuilder sb = new StringBuilder();
            Tuple<bool, string> possibleJump = new Tuple<bool, string>(false, null);
            int move = randomPercentileChance.Next(0, 3);

            if (players[turn == 0 ? 0 : 1].Row == players[turn == 0 ? 1 : 0].Row && players[turn == 0 ? 0 : 1].Col + 2 == players[turn == 0 ? 1 : 0].Col)
            {
                switch (move)
                {
                    case 0:
                        // East Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2) + 1));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 1:
                        // Northeast Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2)));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2) + 1);
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 2:
                        // Southeast Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2)));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2) - 1);
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                }
            }
            else if (players[turn == 0 ? 0 : 1].Row == players[turn == 0 ? 1 : 0].Row && players[turn == 0 ? 0 : 1].Col == players[turn == 0 ? 1 : 0].Col + 2)
            {
                switch (move)
                {
                    case 0:
                        // West Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2) - 1));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 1:
                        // Northwest Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2)));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2) + 1);
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 2:
                        // Southwest Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2)));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2) - 1);
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                }
            }
            else if (players[turn == 0 ? 0 : 1].Row == players[turn == 0 ? 1 : 0].Row + 2 && players[turn == 0 ? 0 : 1].Col == players[turn == 0 ? 1 : 0].Col)
            {
                switch (move)
                {
                    case 0:
                        // North Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2)));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2) + 1 > 9 ? 9 : (9 - (players[turn == 0 ? 1 : 0].Row / 2) + 1));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 1:
                        // Northeast Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2) + 1));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 2:
                        // Northwest Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2) - 1));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                }
            }
            else if (players[turn == 0 ? 0 : 1].Row == players[turn == 0 ? 1 : 0].Row - 2 && players[turn == 0 ? 0 : 1].Col == players[turn == 0 ? 1 : 0].Col)
            {
                switch (move)
                {
                    case 0:
                        // South Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2)));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2) - 1 < 1 ? 1 : (9 - (players[turn == 0 ? 1 : 0].Row / 2) - 1));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 1:
                        // Southeast Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2) + 1));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                    case 2:
                        // Southwest Jump
                        sb.Append(Convert.ToChar(97 + (players[turn == 0 ? 1 : 0].Col / 2) - 1));
                        sb.Append(9 - (players[turn == 0 ? 1 : 0].Row / 2));
                        possibleJump = new Tuple<bool, string>(true, sb.ToString());
                        break;
                }
            }

            return possibleJump;

        }

        private bool PlayersAreAdjacent()
        {
            return (playerLocations[turn == 0 ? 0 : 1].Row == playerLocations[turn == 0 ? 1 : 0].Row && playerLocations[turn == 0 ? 0 : 1].Col + 2 == playerLocations[turn == 0 ? 1 : 0].Col && !board[playerLocations[turn == 0 ? 0 : 1].Row].Get(playerLocations[turn == 0 ? 0 : 1].Col + 1))
                || (playerLocations[turn == 0 ? 0 : 1].Row == playerLocations[turn == 0 ? 1 : 0].Row && playerLocations[turn == 0 ? 0 : 1].Col - 2 == playerLocations[turn == 0 ? 1 : 0].Col && !board[playerLocations[turn == 0 ? 0 : 1].Row].Get(playerLocations[turn == 0 ? 0 : 1].Col - 1))
                || (playerLocations[turn == 0 ? 0 : 1].Row + 2 == playerLocations[turn == 0 ? 1 : 0].Row && playerLocations[turn == 0 ? 0 : 1].Col == playerLocations[turn == 0 ? 1 : 0].Col && !board[playerLocations[turn == 0 ? 0 : 1].Row + 1].Get(playerLocations[turn == 0 ? 0 : 1].Col))
                || (playerLocations[turn == 0 ? 0 : 1].Row - 2 == playerLocations[turn == 0 ? 1 : 0].Row && playerLocations[turn == 0 ? 0 : 1].Col == playerLocations[turn == 0 ? 1 : 0].Col && !board[playerLocations[turn == 0 ? 0 : 1].Row - 1].Get(playerLocations[turn == 0 ? 0 : 1].Col));
        }

        private void Populate()
        {
            foreach (var wallCoordinate in walls)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, true);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);

                SetPlayerMoveValues(wallCoordinate, mid);
            }
        }

        private void SetPlayerMoveValues(WallCoordinate wallCoordinate, Tuple<int, int> mid)
        {
            if (wallCoordinate.Orientation == WallCoordinate.WallOrientation.Horizontal)
            {
                SetPlayerMoveValuesHorizontal(wallCoordinate, mid);
            }
            else
            {
                SetPlayerMoveValuesVertical(wallCoordinate, mid);
            }
        }

        private void SetPlayerMoveValuesHorizontal(WallCoordinate wallCoordinate, Tuple<int, int> mid)
        {
            ++possibleMoveValues[(wallCoordinate.StartRow + 1) / 2, (wallCoordinate.StartCol) / 2];
            ++possibleMoveValues[(wallCoordinate.StartRow - 1) / 2, (wallCoordinate.StartCol) / 2];

            ++possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 + 1) / 2];
            ++possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 + 1) / 2];

            ++possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 - 1) / 2];
            ++possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 - 1) / 2];

            ++possibleMoveValues[(wallCoordinate.EndRow + 1) / 2, (wallCoordinate.EndCol) / 2];
            ++possibleMoveValues[(wallCoordinate.EndRow - 1) / 2, (wallCoordinate.EndCol) / 2];
        }

        private void SetPlayerMoveValuesVertical(WallCoordinate wallCoordinate, Tuple<int, int> mid)
        {
            ++possibleMoveValues[(wallCoordinate.EndRow) / 2, (wallCoordinate.EndCol + 1) / 2];
            ++possibleMoveValues[(wallCoordinate.EndRow) / 2, (wallCoordinate.EndCol - 1) / 2];

            ++possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 + 1) / 2];
            ++possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 + 1) / 2];

            ++possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 - 1) / 2];
            ++possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 - 1) / 2];

            ++possibleMoveValues[(wallCoordinate.StartRow) / 2, (wallCoordinate.StartCol - 1) / 2];
            ++possibleMoveValues[(wallCoordinate.StartRow) / 2, (wallCoordinate.StartCol - 1) / 2];
        }

        private void Unpopulate()
        {
            foreach (var wallCoordinate in walls)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, false);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);
                ResetPlayerMoveValues(wallCoordinate, mid);
            }
        }

        private void ResetPlayerMoveValues(WallCoordinate wallCoordinate, Tuple<int, int> mid)
        {
            if (wallCoordinate.Orientation == WallCoordinate.WallOrientation.Horizontal)
            {
                ResetPlayerMoveValuesHorizontal(wallCoordinate, mid);
            }
            else
            {
                ResetPlayerMoveValuesVertical(wallCoordinate, mid);
            }
        }

        private void ResetPlayerMoveValuesHorizontal(WallCoordinate wallCoordinate, Tuple<int, int> mid)
        {
            possibleMoveValues[(wallCoordinate.StartRow + 1) / 2, (wallCoordinate.StartCol) / 2] =
            possibleMoveValues[(wallCoordinate.StartRow - 1) / 2, (wallCoordinate.StartCol) / 2] =

            possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 + 1) / 2] =
            possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 + 1) / 2] =

            possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 - 1) / 2] =
            possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 - 1) / 2] =

            possibleMoveValues[(wallCoordinate.EndRow + 1) / 2, (wallCoordinate.EndCol) / 2] =
            possibleMoveValues[(wallCoordinate.EndRow - 1) / 2, (wallCoordinate.EndCol) / 2] = 0;
        }

        private void ResetPlayerMoveValuesVertical(WallCoordinate wallCoordinate, Tuple<int, int> mid)
        {
            possibleMoveValues[(wallCoordinate.EndRow) / 2, (wallCoordinate.EndCol + 1) / 2] =
            possibleMoveValues[(wallCoordinate.EndRow) / 2, (wallCoordinate.EndCol - 1) / 2] =

            possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 + 1) / 2] =
            possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 + 1) / 2] =

            possibleMoveValues[(mid.Item1 + 1) / 2, (mid.Item2 - 1) / 2] =
            possibleMoveValues[(mid.Item1 - 1) / 2, (mid.Item2 - 1) / 2] =

            possibleMoveValues[(wallCoordinate.StartRow) / 2, (wallCoordinate.StartCol - 1) / 2] =
            possibleMoveValues[(wallCoordinate.StartRow) / 2, (wallCoordinate.StartCol - 1) / 2] = 0;
        }

        //Selection Phase Code
        /// <summary>
        /// SelectNode selects a node at random given a nodes children. If there are no nodes available the function returns -1 otherwise it returns the index of the selcted node.
        /// </summary>
        /// <returns></returns>
        private int SelectNode(int expandedNodeIndex = -1)
        {
            int isAValidNodeAvailable = -1;

            if (children.Count != 0 && (randomPercentileChance.Next(1, 100) < 71))
            {
                isAValidNodeAvailable = SelectionAlgorithm();
            }
            else if (expandedNodeIndex >= 0)
            {
                isAValidNodeAvailable = expandedNodeIndex;
            }

            return isAValidNodeAvailable;
        }

        /// <summary>
        /// The SelectionAlgorithm calculates a score for each move based on the knowledge currently in the tree.
        /// </summary>
        /// <returns></returns>
        private int SelectionAlgorithm()
        {
            lock (childrenAccess)
            {
                foreach (var child in children)
                {
                    if (!moveTotals.ContainsKey(child.thisMove))
                    {
                        moveTotals.Add(child.thisMove, new Tuple<double, double>(0, 0));
                        child.SetScore(double.PositiveInfinity);
                    }
                    else if (child.GetVisits() == 0)
                    {
                        child.SetScore(double.PositiveInfinity);
                    }
                    else
                    {
                        child.SetScore((((child.GetWins() / child.GetVisits()) + explorationFactor) * FastSqRt(Math.Log(timesVisited) / child.GetVisits()))
                        + (moveTotals[child.GetMove()].Item1 / moveTotals[child.GetMove()].Item2) * (historyInfluence / (child.GetVisits() - child.GetWins() + 1)));
                    }
                }

                children.Sort(delegate (MonteCarloNode lValue, MonteCarloNode rValue)
                {
                    if (lValue.GetScore() == rValue.GetScore()) return 0;
                    else return lValue.GetScore().CompareTo(rValue.GetScore());
                });

                return children.Count - 1;
            }
        }

        //Expansion Phase Code
        /// <summary>
        /// The <c>ExpandOptions</c> method calls the <c>RandomMove</c> method to generate a move to expand the current options from the current <c>MonteCarloNode</c>
        /// and returns true after it has expanded the child options.
        /// </summary>
        private int ExpandOptions()
        {
            string move;
            move = RandomMove();
            while (!InsertChild(move))
            {

                move = RandomMove();

            }

            return FindMove(move);
        }

        /// <summary>
        /// The <c>InsertChild</c> method inserts a new <c>MonteCarloNode</c> child into the current <c>children</c> List. If the move is valid it will return true signifying success. 
        /// If the move was an invalid move the method will return false
        /// </summary>
        /// <param name="move">specified move - either place a wall or move a pawn</param>
        private bool InsertChild(string move)
        {
            bool successfulInsert = false;

            if (!childrensMoves.Contains(move))
            {
                if (move.Length != 2)
                {
                    if (ValidWallMove(move))
                    {
                        if (PlaceWall(turn == 0 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO, new WallCoordinate(move)))
                        {
                            lock (childrenAccess)
                            {
                                if (!childrensMoves.Contains(move))
                                {
                                    children.Add(new MonteCarloNode(move, playerLocations, wallsRemaining, walls, new WallCoordinate(move), turn, depthCheck + 1, this));

                                    if (!moveTotals.ContainsKey(move))
                                    {
                                        moveTotals.Add(move, new Tuple<double, double>(0, 0));
                                    }
                                    else
                                    {
                                        moveTotals[move] = new Tuple<double, double>(moveTotals[move].Item1, moveTotals[move].Item2 + 1);
                                    }

                                    childrensMoves.Add(move);
                                    //#if DEBUG
                                    //                            Console.WriteLine(move + ' ' + (turn == 0 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO).ToString());
                                    //#endif
                                }
                                successfulInsert = true;
                            }
                        }
                    }
                }
                else
                {
                    PlayerCoordinate moveToInsert = new PlayerCoordinate(move);
                    if (!(moveToInsert.Row == (turn == 0 ? playerLocations[0] : playerLocations[1]).Row && moveToInsert.Col == (turn == 0 ? playerLocations[0] : playerLocations[1]).Col))
                    {
                        lock (childrenAccess)
                        {
                            if (!childrensMoves.Contains(move))
                            {
                                children.Add(new MonteCarloNode(move, depthCheck + 1, this));

                                if (!moveTotals.ContainsKey(move))
                                {
                                    moveTotals.Add(move, new Tuple<double, double>(0, 0));
                                }
                                else
                                {
                                    moveTotals[move] = new Tuple<double, double>(moveTotals[move].Item1, moveTotals[move].Item2 + 1);
                                }

                                childrensMoves.Add(move);
                                //#if DEBUG
                                //                                Console.WriteLine(move + ' ' + (turn == 0 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO).ToString());
                                //#endif
                            }
                            successfulInsert = true;
                        }
                    }
                }
            }
            else
            {
                successfulInsert = true;
            }

            return successfulInsert;
        }

        //Simulation & Backpropagation Phase Code
        /// <summary>
        /// SimulatedGame evaluates a game from a node and plays a series of moves until it reaches an endstate when it recursively backpropagates and updates the previous nodes. 
        /// On a losing endstate the function returns false and true on a victory.
        /// </summary>
        /// <returns>Whether or not the function reached a victorious endstate</returns>
        public bool SimulatedGame()
        {
            ++timesVisited;
            bool mctsVictory = false;

            if (depthCheck > 125)
            {
                gameOver = true;
            }

            if (!gameOver)
            {
                int nextNodeIndex = SelectNode();

                if (nextNodeIndex < 0)
                {
                    nextNodeIndex = SelectNode(ExpandOptions());
                    lock (childrenAccess)
                    {
                        children.Sort(delegate (MonteCarloNode lValue, MonteCarloNode rValue)
                        {
                            if (lValue.wins == rValue.wins) return 0;
                            else return lValue.wins.CompareTo(rValue.wins);
                        });
                    }
                }
                if (children[nextNodeIndex].SimulatedGame())
                {
                    mctsVictory = true;
                    ++wins;
                }
            }
            else
            {
                if (parent.turn.ToString() == MonteCarloPlayer)
                {
                    mctsVictory = true;
                    moveTotals[thisMove] = new Tuple<double, double>(moveTotals[thisMove].Item1 + 1, moveTotals[thisMove].Item2);
                    ++wins;
                }
            }
            lock (childrenAccess)
            {
                children.Sort(delegate (MonteCarloNode lValue, MonteCarloNode rValue)
                {
                    if (lValue.wins == rValue.wins) return 0;
                    else return lValue.wins.CompareTo(rValue.wins);
                });
            }
            return mctsVictory;
        }

    }
    public class MonteCarlo
    {
        MonteCarloNode TreeSearch;
        /// <summary>
        /// The MonteCarlo class is initialized with a GameBoard instance and can calculate a move given a GameBoard
        /// </summary>
        /// <param name="boardState">The current GameBoard to calculate a move from</param>
        public MonteCarlo(GameBoard boardState)
        {
            TreeSearch = new MonteCarloNode(boardState.GetPlayerCoordinate(GameBoard.PlayerEnum.ONE), boardState.GetPlayerCoordinate(GameBoard.PlayerEnum.TWO),
                                                            boardState.GetPlayerWallCount(GameBoard.PlayerEnum.ONE), boardState.GetPlayerWallCount(GameBoard.PlayerEnum.TWO),
                                                            boardState.GetWalls(), boardState.GetWhoseTurn() == 1 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO);
        }

        private void ThreadedTreeSearch(Stopwatch timer, MonteCarloNode MonteCarlo)
        {
            for (int i = 0; i < 1000 && timer.Elapsed.TotalSeconds < 3; ++i)
            {
                MonteCarlo.SimulatedGame();
            }
        }

        public string MonteCarloTreeSearch()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            List<Thread> simulatedGames = new List<Thread>();

            for (int i = 0; i < 4; ++i)
            {
                Thread simulatedGameThread = new Thread(() => ThreadedTreeSearch(timer, TreeSearch)) { IsBackground = true };
                simulatedGameThread.Name = String.Format("SimulatedGameThread{0}", i + 1);
                simulatedGameThread.Start();
                simulatedGames.Add(simulatedGameThread);
            }


            foreach (Thread thread in simulatedGames)
            {
                thread.Join();
            }

            timer.Stop();
            //#endif
            Console.WriteLine("Wins: " + TreeSearch.GetWins());
            Console.WriteLine("Visits: " + TreeSearch.GetVisits());
            List<MonteCarloNode> childrenToChoose = TreeSearch.GetChildrenNodes();
            childrenToChoose.Sort();
            return childrenToChoose[childrenToChoose.Count - 1].GetMove();
        }


    }
}