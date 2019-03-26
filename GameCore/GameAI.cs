#define DEBUG 
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
        public class PriorityQueue<T> where T : IComparable<T>
        {
            private List<T> data;

            public PriorityQueue()
            {
                this.data = new List<T>();
            }

            public void Enqueue(T item)
            {
                data.Add(item);
                int ci = data.Count - 1; // child index; start at end
                while (ci > 0)
                {
                    int pi = (ci - 1) / 2; // parent index
                    if (data[ci].CompareTo(data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
                    T tmp = data[ci]; data[ci] = data[pi]; data[pi] = tmp;
                    ci = pi;
                }
            }

            public T Dequeue()
            {
                // assumes pq is not empty; up to calling code
                int li = data.Count - 1; // last index (before removal)
                T frontItem = data[0];   // fetch the front
                data[0] = data[li];
                data.RemoveAt(li);

                --li; // last index (after removal)
                int pi = 0; // parent index. start at front of pq
                while (true)
                {
                    int ci = pi * 2 + 1; // left child index of parent
                    if (ci > li) break;  // no children so done
                    int rc = ci + 1;     // right child
                    if (rc <= li && data[rc].CompareTo(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                        ci = rc;
                    if (data[pi].CompareTo(data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
                    T tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp; // swap parent and child
                    pi = ci;
                }
                return frontItem;
            }

            public T Peek()
            {
                T frontItem = data[0];
                return frontItem;
            }

            public int Count()
            {
                return data.Count;
            }

            public override string ToString()
            {
                string s = "";
                for (int i = 0; i < data.Count; ++i)
                    s += data[i].ToString() + " ";
                s += "count = " + data.Count;
                return s;
            }

            public bool IsConsistent()
            {
                // is the heap property true for all data?
                if (data.Count == 0) return true;
                int li = data.Count - 1; // last index
                for (int pi = 0; pi < data.Count; ++pi) // each parent index
                {
                    int lci = 2 * pi + 1; // left child index
                    int rci = 2 * pi + 2; // right child index

                    if (lci <= li && data[pi].CompareTo(data[lci]) > 0) return false; // if lc exists and it's greater than parent then bad.
                    if (rci <= li && data[pi].CompareTo(data[rci]) > 0) return false; // check the right child too.
                }
                return true; // passed all checks
            } // IsConsistent
        } // PriorityQueue

       private class MoveEvaluation : IComparable<MoveEvaluation>
        {
            private string move;
            private double value;
            private int distanceFromStart;

            public MoveEvaluation(string givenMove, double givenValue, int givenDistance)
            {
                Move = givenMove;
                value = givenValue;
                distanceFromStart = givenDistance;
            }

            public string Move { get => move; set => move = value; }
            public double Value { get => value; set => this.value = value; }
            public int DistanceFromStart { get => distanceFromStart; set => distanceFromStart = value; }

            public int CompareTo(MoveEvaluation moveEvaluation)
            {
                return value.CompareTo(moveEvaluation.value);
            }
        }


        private static readonly object boardAccess = new object();
        private readonly object childrenAccess = new object();
        private readonly object childrenMovesAccess = new object();
        private static double explorationFactor = 1.0 / Math.Sqrt(2.0);


        private static double historyInfluence = 1.15;
        private static GameBoard.PlayerEnum monteCarloPlayerEnum;
        private static string MonteCarloPlayer;
        private static Random randomPercentileChance;
        private static List<BitArray> board;
        private static int[,] possibleMoveValues;
        private static Dictionary<string, Tuple<double, double>> moveTotals;

        private List<MonteCarloNode> children;
        private List<WallCoordinate> walls;
        private List<Tuple<string, double>> possibleMoves;
        private List<Tuple<string, double>> possibleBlocks;
        private List<string> possibleHorizontalWalls;
        private List<string> possibleVerticalWalls;
        private List<string> lastPlayerMove;
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

        private int shortestPathfinder(string move)
        {
            PlayerCoordinate start;
            string startString;
            int goalRow = turn == 0 ? 9 : 1;
            int goalRowForBoard = turn == 0 ? 0 : 16;

            if (move.Length != 2)
            {
                WallCoordinate wallCoordinate = new WallCoordinate(move);

                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, true);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);

                SetPlayerMoveValues(wallCoordinate, mid);
                start = new PlayerCoordinate(playerLocations[turn == 0 ? 0 : 1].Row, playerLocations[turn == 0 ? 0 : 1].Col);
            }
            else
            {
                start = new PlayerCoordinate(move);
            }

            startString = Convert.ToChar(97 + start.Col / 2) + (9 - (start.Row) / 2).ToString();

            PriorityQueue<MoveEvaluation> possiblePaths = new PriorityQueue<MoveEvaluation>();

            for (int i = 0; i < 4; ++i)
            {
                string path = null;
                switch (i)
                {
                    case 0:
                        if (startString[1] + 1 < '9' && !board[start.Row + 1].Get(start.Col))
                        {
                            path = startString[0].ToString() + (startString[1] + 1).ToString();
                        }
                        break;
                    case 1:
                        if (startString[0] + 1 < 'i' && !board[start.Row].Get(start.Col + 1))
                        {
                            path = (startString[0] + 1).ToString() + startString[1].ToString();
                        }
                        break;
                    case 2:
                        if (startString[1] - 1 >= '1' && !board[start.Row - 1].Get(start.Col))
                        {
                            path = startString[0].ToString() + (startString[1] - 1).ToString();
                        }
                        break;
                    case 3:
                        if (startString[0] - 1 >= 'a' && !board[start.Row].Get(start.Col - 1))
                        {
                            path = startString[0].ToString() + (startString[1] + 1).ToString();
                        }
                        break;
                }
                if (path != null) 
                {
                    possiblePaths.Enqueue(new MoveEvaluation(path, HeuristicCostEstimate(new PlayerCoordinate(path), new PlayerCoordinate(path[0].ToString() + goalRow.ToString())), 1));
                }
            }

            int shortestPath = 0;
            MoveEvaluation nextMove;

            do
            {
                nextMove = possiblePaths.Dequeue();
                PlayerCoordinate current = new PlayerCoordinate(nextMove.Move);
                string currentString = Convert.ToChar(97 + current.Col / 2) + (9 - (current.Row) / 2).ToString();

                if (current.Row == goalRowForBoard)
                {
                    shortestPath = nextMove.DistanceFromStart;
                }

                for (int i = 0; i < 4; ++i)
                {
                    string path = null;
                    switch (i)
                    {
                        case 0:
                            if (currentString[1] + 1 < '9' && !board[current.Row + 1].Get(current.Col))
                            {
                                path = currentString[0].ToString() + (currentString[1] + 1).ToString();
                            }
                            break;
                        case 1:
                            if (currentString[0] + 1 < 'i' && !board[current.Row].Get(current.Col + 1))
                            {
                                path = (currentString[0] + 1).ToString() + currentString[1].ToString();
                            }
                            break;
                        case 2:
                            if (currentString[1] - 1 >= '1' && !board[current.Row - 1].Get(current.Col))
                            {
                                path = currentString[0].ToString() + (currentString[1] - 1).ToString();
                            }
                            break;
                        case 3:
                            if (currentString[0] - 1 >= 'a' && !board[current.Row].Get(current.Col - 1))
                            {
                                path = currentString[0].ToString() + (currentString[1] + 1).ToString();
                            }
                            break;
                    }
                    if (path != null)
                    { 
                        possiblePaths.Enqueue(new MoveEvaluation(path, HeuristicCostEstimate(new PlayerCoordinate(path), new PlayerCoordinate(path[0].ToString() + goalRow.ToString())), nextMove.DistanceFromStart + 1));
                    }
                }

            } while (possiblePaths.Count() != 0 && shortestPath == 0);

            return shortestPath;

        }

        //public double GetStateValue()
        //{
        //    return (Math.Atan(0.5 * (MinimumHeuristicEstimate(thisMove, turn))) / Math.PI) + 0.5;
        //}

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
            return score / timesVisited;
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
            //moveTotals = new Dictionary<string, Tuple<double, double>>();
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
                monteCarloPlayerEnum = currentTurn;
                MonteCarloPlayer = currentTurn.ToString();
            }

            lastPlayerMove = new List<string>();
            lastPlayerMove.Add(Convert.ToChar(97 + playerOne.Col / 2).ToString() + (9 - (playerOne.Row / 2)).ToString());
            lastPlayerMove.Add(Convert.ToChar(97 + playerTwo.Col / 2).ToString() + (9 - (playerTwo.Row / 2)).ToString());

            playerLocations = new List<PlayerCoordinate>();
            playerLocations.Add(new PlayerCoordinate(playerOne.Row, playerOne.Col));
            playerLocations.Add(new PlayerCoordinate(playerTwo.Row, playerTwo.Col));

            possibleHorizontalWalls = possibleVerticalWalls = new List<string>();

            for (int characterIndex = 0; characterIndex < 8; characterIndex++)
            {
                for (int numberIndex = 1; numberIndex < 9; numberIndex++)
                {
                    possibleVerticalWalls.Add(Convert.ToChar(97 + characterIndex).ToString() + numberIndex.ToString());
                }
            }

            wallsRemaining = new List<int>();
            wallsRemaining.Add(playerOneTotalWalls);
            wallsRemaining.Add(playerTwoTotalWalls);


            walls = new List<WallCoordinate>(wallCoordinates);

            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();

            randomPercentileChance = new Random();

            wins = 0;
            timesVisited = 1;
            turn = currentTurn;
            possibleMoves = PossibleMovesFromPosition();

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

            lastPlayerMove = new List<string>(parent.lastPlayerMove);

            playerLocations = new List<PlayerCoordinate>(players);

            wallsRemaining = new List<int>(wallCounts);

            walls = new List<WallCoordinate>(wallCoordinates);


            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();

            possibleHorizontalWalls = possibleVerticalWalls = new List<string>();

            for (int characterIndex = 0; characterIndex < 9; characterIndex++)
            {
                for (int numberIndex = 1; numberIndex < 10; numberIndex++)
                {
                    possibleVerticalWalls.Add(Convert.ToChar(97 + characterIndex).ToString() + numberIndex.ToString());
                }
            }

            possibleHorizontalWalls.Remove(move.Substring(0, 2));

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

            int locationOfPreviousMove = DoesMoveListContain(possibleMoves, parent.lastPlayerMove[turn == 0 ? 0 : 1]);

            if (possibleMoves.Count != 1 && (parent != null ? locationOfPreviousMove != -1 : false))
            {
                possibleMoves.RemoveAt(locationOfPreviousMove);
            }
        }

        private int DoesMoveListContain(List<Tuple<string, double>> possibleMoves, string v)
        {
            int indexOfMove = -1;

            for (int index = 0; index < possibleMoves.Count && indexOfMove == -1; index++)
            {
                if (possibleMoves[index].Item1 == v)
                {
                    indexOfMove = index;
                }
            }

            return indexOfMove;
        }

        private MonteCarloNode(string move, int depth, List<string> availableWalls, MonteCarloNode childParent)
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

            lastPlayerMove = new List<string>();
            if (childParent.turn == 0)
            {
                lastPlayerMove.Add(Convert.ToChar(97 + playerLocations[0].Col / 2).ToString() + (9 - (playerLocations[0].Row / 2)).ToString());
                lastPlayerMove.Add(childParent.lastPlayerMove[1]);
            }
            else
            {
                lastPlayerMove.Add(childParent.lastPlayerMove[0]);
                lastPlayerMove.Add(Convert.ToChar(97 + playerLocations[1].Col / 2).ToString() + (9 - (playerLocations[1].Row / 2)).ToString());
            }

            wallsRemaining = new List<int>(childParent.wallsRemaining);

            walls = new List<WallCoordinate>(childParent.walls);
            possibleHorizontalWalls = possibleVerticalWalls = availableWalls;

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

            if (!gameOver)
            {
                possibleMoves = PossibleMovesFromPosition();

                int locationOfPreviousMove = DoesMoveListContain(possibleMoves, childParent.lastPlayerMove[turn == 0 ? 0 : 1]);

                if (possibleMoves.Count != 1 && locationOfPreviousMove != -1)
                {
                    possibleMoves.RemoveAt(locationOfPreviousMove);
                }
            }
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

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
                }
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row + 1].Get(playerLocations[turn == 0 ? 0 : 1].Col + 2 * direction))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2)));
                    sb.Append(value: 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - 1 < 1 ? 1
                                   : 9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2) - 1);

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
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

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
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

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
                }
                if (!board[playerLocations[turn == 0 ? 0 : 1].Row + 2 * direction].Get(playerLocations[turn == 0 ? 0 : 1].Col - 1))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) - 1 < 97 ? 97
                                                  : 97 + (playerLocations[turn == 0 ? 1 : 0].Col / 2) - 1));
                    sb.Append(9 - (playerLocations[turn == 0 ? 1 : 0].Row / 2));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
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

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
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
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
                }
                if (playerLocations[currentPlayer].Row - 1 > -1 && !board[playerLocations[currentPlayer].Row - 1].Get(playerLocations[currentPlayer].Col)
                     && (playerLocations[currentPlayer].Row - 2 != playerLocations[opponent].Row || playerLocations[currentPlayer].Col != playerLocations[opponent].Col))
                {
                    //North
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2)));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2) + 1 > 9 ? 9 : 9 - (playerLocations[currentPlayer].Row / 2) + 1);
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
                }
                if (playerLocations[currentPlayer].Col + 1 < 17 && !board[playerLocations[currentPlayer].Row].Get(playerLocations[currentPlayer].Col + 1)
                    && (playerLocations[currentPlayer].Row != playerLocations[opponent].Row || playerLocations[currentPlayer].Col + 2 != playerLocations[opponent].Col))
                {
                    //East
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2) + 1));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2));
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
                }
                if (playerLocations[currentPlayer].Col - 1 > -1 && !board[playerLocations[currentPlayer].Row].Get(playerLocations[currentPlayer].Col - 1)
                    && (playerLocations[currentPlayer].Row != playerLocations[opponent].Row || playerLocations[currentPlayer].Col - 2 != playerLocations[opponent].Col))
                {
                    //West
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2) - 1));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2));
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), turn == 0 ? 9 : 1)));
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

        public double MinimumHeuristicEstimate(string locationToStart, int goal)
        {
            int EndRow = goal;

            PlayerCoordinate start;
            WallCoordinate wallCoordinate = null;

            if (locationToStart.Length > 2)
            {
                start = playerLocations[turn == 0 ? 0 : 1];

                wallCoordinate = new WallCoordinate(locationToStart);
            }
            else
            {
                start = new PlayerCoordinate(locationToStart);
            }

            if (wallCoordinate != null)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, true);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);

                SetPlayerMoveValues(wallCoordinate, mid);
            }

            double possibleMinimumHeuristic = shortestPathfinder(start.Col.ToString() + start.Row.ToString());

            if (wallCoordinate != null)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, false);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);

                ResetPlayerMoveValues(wallCoordinate, mid);
            }

            return possibleMinimumHeuristic + possibleMoveValues[start.Row / 2, start.Col / 2] + 1;
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
            return randomPercentileChance.Next(1, 100) >= 37 ? FindPlayerMove() : (turn == 0 ? wallsRemaining[0] : wallsRemaining[1]) > 0 ? BoardUtil.GetRandomWallPlacementMove() : FindPlayerMove();
        }

        private string FindPlayerMove()
        {
            string move = null;

            move = possibleMoves[0].Item1;

            for (int i = 1; childrensMoves.Contains(move) && i < possibleMoves.Count; ++i)
            {
                move = possibleMoves[i].Item1;
            }

            if (childrensMoves.Contains(move))
            {
                move = possibleMoves[randomPercentileChance.Next(0, possibleMoves.Count)].Item1;
            }

            return move;
        }

        private string FindWall()
        {
            if (possibleVerticalWalls.Count > 0)
            {
                lock (boardAccess)
                {
                    Populate();
                    string wallMove = null;
                    PlayerCoordinate opponent = turn == 0 ? playerLocations[1] : playerLocations[0];

                    switch (randomPercentileChance.Next(0, 2))
                    {
                        case 0:
                            wallMove = possibleVerticalWalls[randomPercentileChance.Next(0, possibleVerticalWalls.Count)] + "v";
                            break;
                        case 1:
                            wallMove = possibleHorizontalWalls[randomPercentileChance.Next(0, possibleHorizontalWalls.Count)] + "h";
                            break;
                    }

                    Unpopulate();
                    return wallMove;
                }

            }
            else
            {
                return FindPlayerMove();
            }
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

        private List<Tuple<int, int>> CanGoalReachOpenLocation(Tuple<int, int> goalStart, int[,] possibleMoveValues)
        {
            HashSet<Tuple<int, int>> markedSet = new HashSet<Tuple<int, int>>();
            Queue<Tuple<Tuple<int, int>, List<Tuple<int, int>>>> queue = new Queue<Tuple<Tuple<int, int>, List<Tuple<int, int>>>>();

            Dictionary<Tuple<int, int>, List<Tuple<int, int>>> cameFrom = new Dictionary<Tuple<int, int>, List<Tuple<int, int>>>
            {
                { goalStart, new List<Tuple<int, int>>() }
            };

            markedSet.Add(goalStart);
            queue.Enqueue(new Tuple<Tuple<int, int>, List<Tuple<int, int>>>(goalStart, cameFrom[goalStart]));

            while (queue.Count > 0)
            {
                Tuple<Tuple<int, int>, List<Tuple<int, int>>> current = queue.Dequeue();
                if (possibleMoveValues[current.Item1.Item1, current.Item1.Item2] == 0 && current.Item1.Item1 != goalStart.Item1)
                {
                    if (IsThereAnAdjacentLocationEqualToZero(current.Item1, possibleMoveValues))
                    {
                        return current.Item2;
                    }
                }

                if ((current.Item1.Item2 * 2) + 1 < GameBoard.TOTAL_COLS
                    && board[current.Item1.Item1 * 2].Get((current.Item1.Item2 * 2) + 1) != true && current.Item1.Item1 != goalStart.Item1) // Can move East
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1.Item1, current.Item1.Item2 + 1);
                    if (!markedSet.Contains(neighbor))
                    {
                        List<Tuple<int, int>> nodesBefore = new List<Tuple<int, int>>(current.Item2);
                        nodesBefore.Add(current.Item1);
                        markedSet.Add(neighbor);
                        queue.Enqueue(new Tuple<Tuple<int, int>, List<Tuple<int, int>>>(neighbor, nodesBefore));
                    }
                }
                if ((current.Item1.Item2 * 2) - 1 >= 0
                    && board[current.Item1.Item1 * 2].Get((current.Item1.Item2 * 2) - 1) != true && current.Item1.Item1 != goalStart.Item1) // Can move West 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1.Item1, current.Item1.Item2 - 1);
                    if (!markedSet.Contains(neighbor))
                    {
                        List<Tuple<int, int>> nodesBefore = new List<Tuple<int, int>>(current.Item2);
                        nodesBefore.Add(current.Item1);
                        markedSet.Add(neighbor);
                        queue.Enqueue(new Tuple<Tuple<int, int>, List<Tuple<int, int>>>(neighbor, nodesBefore));
                    }
                }
                if ((current.Item1.Item1 * 2) - 1 >= 0
                    && board[(current.Item1.Item1 * 2) - 1].Get((current.Item1.Item2 * 2)) != true && current.Item1.Item1 - 1 != goalStart.Item1) // Can move North 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1.Item1 - 1, current.Item1.Item2);
                    if (!markedSet.Contains(neighbor))
                    {
                        List<Tuple<int, int>> nodesBefore = new List<Tuple<int, int>>(current.Item2);
                        nodesBefore.Add(current.Item1);
                        markedSet.Add(neighbor);
                        queue.Enqueue(new Tuple<Tuple<int, int>, List<Tuple<int, int>>>(neighbor, nodesBefore));
                    }
                }
                if ((current.Item1.Item1 * 2) + 1 < GameBoard.TOTAL_COLS
                    && board[(current.Item1.Item1 * 2) + 1].Get((current.Item1.Item2 * 2)) != true && current.Item1.Item1 + 1 != goalStart.Item1) // Can move South 
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1.Item1 + 1, current.Item1.Item2);
                    if (!markedSet.Contains(neighbor))
                    {
                        List<Tuple<int, int>> nodesBefore = new List<Tuple<int, int>>(current.Item2);
                        nodesBefore.Add(current.Item1);
                        markedSet.Add(neighbor);
                        queue.Enqueue(new Tuple<Tuple<int, int>, List<Tuple<int, int>>>(neighbor, nodesBefore));
                    }
                }
            }
            return new List<Tuple<int, int>>();
        }

        private bool IsThereAnAdjacentLocationEqualToZero(Tuple<int, int> current, int[,] possibleMoveValues)
        {
            return (current.Item1 - 1 > -1 && possibleMoveValues[current.Item1 - 1, current.Item2] == 0 && !board[(current.Item1 * 2) - 1].Get((current.Item2 * 2))) ||
                   (current.Item1 + 1 < 9 && possibleMoveValues[current.Item1 + 1, current.Item2] == 0 && !board[(current.Item1 * 2) + 1].Get((current.Item2 * 2))) ||
                   (current.Item2 - 1 > -1 && possibleMoveValues[current.Item1, current.Item2 - 1] == 0 && !board[(current.Item1 * 2)].Get((current.Item2 * 2) - 1)) ||
                   (current.Item2 + 1 < 9 && possibleMoveValues[current.Item1, current.Item2 + 1] == 0 && !board[(current.Item1 * 2)].Get((current.Item2 * 2) + 1));
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
        public MonteCarloNode SelectNode(MonteCarloNode root)
        {
            if (root.children.Count != 0 && (randomPercentileChance.Next(1, 100) >= 51))
            {
                return root.SelectNode(root.SelectionAlgorithm());

            }
            else
            {
                return root;
            }

        }

        /// <summary>
        /// The SelectionAlgorithm calculates a score for each move based on the knowledge currently in the tree.
        /// </summary>
        /// <returns></returns>
        private MonteCarloNode SelectionAlgorithm()
        {
            lock (childrenAccess)
            {
                children.Sort();

                return children[children.Count - 1];
            }
        }

        //Expansion Phase Code
        /// <summary>
        /// The <c>ExpandOptions</c> method calls the <c>RandomMove</c> method to generate a move to expand the current options from the current <c>MonteCarloNode</c>
        /// and returns true after it has expanded the child options.
        /// </summary>
        public MonteCarloNode ExpandOptions(MonteCarloNode root)
        {
            string move;
            move = root.RandomMove();
            while (!root.InsertChild(move))
            {

                move = root.RandomMove();

            }

            return root.children[root.FindMove(move)];
        }

        private double Evaluate(MonteCarloNode child)
        {
            string opponent = Convert.ToChar(97 + playerLocations[turn == 0 ? 1 : 0].Col / 2).ToString() + (9 - playerLocations[turn == 0 ? 1 : 0].Row / 2).ToString();
            string player;
            string move = child.thisMove;

            if (move.Length > 2)
            {
                player = Convert.ToChar(97 + playerLocations[turn == 0 ? 0 : 1].Col / 2).ToString() + (9 - playerLocations[turn == 0 ? 0 : 1].Row / 2).ToString();

            }
            else
            {
                player = move;

            }

            //if (!moveTotals.ContainsKey(child.thisMove))
            //{
            //    moveTotals.Add(child.thisMove, new Tuple<double, double>(0, 1));
            //}

            return StateValue(opponent, player) + explorationFactor
                             * Math.Sqrt(Math.Log(timesVisited) / child.GetVisits());
                            //(moveTotals[child.GetMove()].Item1 / moveTotals[child.GetMove()].Item2)
                            //* (historyInfluence / (child.GetVisits() - child.GetWins() + 1));

        }

        private double StateValue(string opponent, string player)
        {
           return (Math.Atan(0.5 * (0.9 *((MinimumHeuristicEstimate(player, turn == 0 ? 9 : 1) - MinimumHeuristicEstimate(opponent, turn == 0 ? 1 : 9))))) / Math.PI) + 0.5;
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

                                    //if (moveTotals.ContainsKey(move))
                                    //{
                                    //    moveTotals[move] = new Tuple<double, double>(moveTotals[move].Item1, moveTotals[move].Item2 + 1);
                                    //}

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
                                children.Add(new MonteCarloNode(move, depthCheck + 1, possibleHorizontalWalls, this));

                                //if (moveTotals.ContainsKey(move))
                                //{
                                //    moveTotals[move] = new Tuple<double, double>(moveTotals[move].Item1, moveTotals[move].Item2 + 1);
                                //}

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
        
        public void Backpropagate(MonteCarloNode newlyAddedNode)
        {
            lock (childrenAccess)
            {
                MonteCarloNode node = newlyAddedNode;
                node.timesVisited++;
                double result = Evaluate(newlyAddedNode);
                node.SetScore(result);
                node = node.parent;

                while (node != null)
                {
                    node.SetScore(score + result);
                    node.timesVisited++;
                    node = node.parent;
                }
            }
        }

    }
    public class MonteCarlo
    {
        MonteCarloNode TreeSearch;
        bool isHardAI;
        /// <summary>
        /// The MonteCarlo class is initialized with a GameBoard instance and can calculate a move given a GameBoard
        /// </summary>
        /// <param name="boardState">The current GameBoard to calculate a move from</param>
        public MonteCarlo(GameBoard boardState, bool isHard)
        {
            isHardAI = isHard;
            TreeSearch = new MonteCarloNode(boardState.GetPlayerCoordinate(GameBoard.PlayerEnum.ONE), boardState.GetPlayerCoordinate(GameBoard.PlayerEnum.TWO),
                                                              boardState.GetPlayerWallCount(GameBoard.PlayerEnum.ONE), boardState.GetPlayerWallCount(GameBoard.PlayerEnum.TWO),
                                                              boardState.GetWalls(), boardState.GetWhoseTurn() == 1 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO);
        }

        private void ThreadedTreeSearchEasy(Stopwatch timer, MonteCarloNode MonteCarlo)
        {            
            for (int i = 0; /*i < 10000 &&*/ timer.Elapsed.TotalSeconds < 1; ++i)
            {
                MonteCarlo.Backpropagate(MonteCarlo.ExpandOptions(MonteCarlo.SelectNode(MonteCarlo)));
            }
        }

        private void ThreadedTreeSearchHard(Stopwatch timer, MonteCarloNode MonteCarlo)
        {
            for (int i = 0; /*i < 10000 &&*/ timer.Elapsed.TotalSeconds < 5; ++i)
            {
                MonteCarlo.Backpropagate(MonteCarlo.ExpandOptions(MonteCarlo.SelectNode(MonteCarlo)));
            }
        }

        public string MonteCarloTreeSearch()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            List<Thread> simulatedGames = new List<Thread>();

            for (int i = 0; i < 4; ++i)
            {
                Thread simulatedGameThread;

                if (isHardAI)
                {
                    simulatedGameThread = new Thread(() => ThreadedTreeSearchHard(timer, TreeSearch)) { IsBackground = true };
                }
                else
                {
                    simulatedGameThread = new Thread(() => ThreadedTreeSearchEasy(timer, TreeSearch)) { IsBackground = true };
                }

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
            Console.WriteLine("Score: " + TreeSearch.GetScore());
            Console.WriteLine("Visits: " + TreeSearch.GetVisits());
            List<MonteCarloNode> childrenToChoose = TreeSearch.GetChildrenNodes();
            childrenToChoose.Sort();
            return childrenToChoose[childrenToChoose.Count - 1].GetMove();
        }


    }
}