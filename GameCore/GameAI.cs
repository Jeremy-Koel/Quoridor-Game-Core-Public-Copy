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
        private static readonly double explorationFactor = 1.0 / Math.Sqrt(2.0);
        private static readonly int randomPercentValue = 50;
        private static bool isHardAI;


        private static readonly double historyInfluence = 1;
        private static GameBoard.PlayerEnum monteCarloPlayerEnum;
        private static string MonteCarloPlayer;
        private static Random randomPercentileChance;
        private static List<BitArray> board;
        private static List<Dictionary<string, Tuple<double, double>>> moveTotals;
        private static Dictionary<string, MonteCarloNode> visitedNodes;

        private List<MonteCarloNode> children;
        private List<WallCoordinate> walls;
        private List<Tuple<string, double>> possibleMoves;
        private List<Tuple<string, double>> possibleBlocks;
        private List<Tuple<string, double>> possibleWalls;
        private List<string> possibleHorizontalWalls;
        private List<string> possibleVerticalWalls;
        private List<string> possibleBlocksList;
        private List<string> illegalWalls;
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

        public double GetScoreRatio()
        {
            return score / timesVisited;
        }

        public double GetVisits()
        {
            return timesVisited;
        }

        private int ShortestPathfinder(string move, int goalRow)
        {
            PlayerCoordinate start;
            string startString;
            int goalRowForBoard = goalRow == 9 ? 0 : 16;

            if (move.Length != 2)
            {
                WallCoordinate wallCoordinate = new WallCoordinate(move);

                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, true);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);

                //SetPlayerMoveValues(wallCoordinate, mid);
                start = new PlayerCoordinate(playerLocations[goalRow == 9 ? 0 : 1].Row, playerLocations[goalRow == 9 ? 0 : 1].Col);
            }
            else
            {
                start = new PlayerCoordinate(move);
            }

            startString = Convert.ToChar(97 + start.Col / 2) + (9 - (start.Row) / 2).ToString();

            PriorityQueue<MoveEvaluation> possiblePaths = new PriorityQueue<MoveEvaluation>();
            HashSet<string> exhaustedPaths = new HashSet<string>();
            exhaustedPaths.Add(startString);

            for (int i = 0; i < 4; ++i)
            {
                string path = null;
                switch (i)
                {
                    case 0:
                        if (startString[1] + 1 <= '9' && !board[start.Row - 1].Get(start.Col))
                        {
                            path = startString[0] + Convert.ToChar(startString[1] + 1).ToString();
                        }
                        break;
                    case 1:
                        if (startString[0] + 1 < 'i' && !board[start.Row].Get(start.Col + 1))
                        {
                            path = Convert.ToChar(startString[0] + 1).ToString() + startString[1];
                        }
                        break;
                    case 2:
                        if (startString[1] - 1 >= '1' && !board[start.Row + 1].Get(start.Col))
                        {
                            path = startString[0] + Convert.ToChar(startString[1] - 1).ToString();
                        }
                        break;
                    case 3:
                        if (startString[0] - 1 >= 'a' && !board[start.Row].Get(start.Col - 1))
                        {
                            path = Convert.ToChar(startString[0] - 1).ToString() + startString[1];
                        }
                        break;
                }
                if (path != null)
                {
                    possiblePaths.Enqueue(new MoveEvaluation(path, HeuristicCostEstimate(new PlayerCoordinate(path), new PlayerCoordinate(path[0].ToString() + goalRow.ToString())) + 2, 2));
                }
            }

            int shortestPath = 0;
            MoveEvaluation nextMove;

            do
            {
                if (possiblePaths.Count() > 0)
                {
                    nextMove = possiblePaths.Dequeue();
                    PlayerCoordinate current = new PlayerCoordinate(nextMove.Move);
                    string currentString = Convert.ToChar(97 + current.Col / 2) + (9 - (current.Row) / 2).ToString();
                    exhaustedPaths.Add(currentString);

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
                                if (currentString[1] + 1 <= '9' && !board[current.Row - 1].Get(current.Col))
                                {
                                    path = currentString[0] + Convert.ToChar(currentString[1] + 1).ToString();
                                }
                                break;
                            case 1:
                                if (currentString[0] + 1 < 'i' && !board[current.Row].Get(current.Col + 1))
                                {
                                    path = Convert.ToChar(currentString[0] + 1).ToString() + currentString[1];
                                }
                                break;
                            case 2:
                                if (currentString[1] - 1 >= '1' && !board[current.Row + 1].Get(current.Col))
                                {
                                    path = currentString[0] + Convert.ToChar(currentString[1] - 1).ToString();
                                }
                                break;
                            case 3:
                                if (currentString[0] - 1 >= 'a' && !board[current.Row].Get(current.Col - 1))
                                {
                                    path = Convert.ToChar(currentString[0] - 1).ToString() + currentString[1];
                                }
                                break;
                        }
                        if (path != null && !exhaustedPaths.Contains(path))
                        {
                            possiblePaths.Enqueue(new MoveEvaluation(path, HeuristicCostEstimate(new PlayerCoordinate(path), new PlayerCoordinate(path[0].ToString() + goalRow.ToString())) + nextMove.DistanceFromStart + 1, nextMove.DistanceFromStart + 1));
                        }
                    }
                }
            } while (possiblePaths.Count() > 0 && shortestPath == 0);

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
            return timesVisited.CompareTo(carloNode.timesVisited);
        }

        public MonteCarloNode(PlayerCoordinate playerOne, PlayerCoordinate playerTwo, int playerOneTotalWalls, int playerTwoTotalWalls, List<WallCoordinate> wallCoordinates, GameBoard.PlayerEnum currentTurn, List<string> lastStarts, bool difficulty)
        {
            board = new List<BitArray>();
            illegalWalls = new List<string>();
            visitedNodes = new Dictionary<string, MonteCarloNode>();
            moveTotals = new List<Dictionary<string, Tuple<double, double>>>();
            moveTotals.Add(new Dictionary<string, Tuple<double, double>>());
            moveTotals.Add(new Dictionary<string, Tuple<double, double>>());
            possibleBlocksList = new List<string>();
            possibleWalls = new List<Tuple<string, double>>();
            isHardAI = difficulty;

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
            lastPlayerMove.Add(lastStarts[0]);
            lastPlayerMove.Add(lastStarts[1]);

            thisMove = lastStarts[turn == 0 ? 0 : 1];

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

            SetIllegalWalls();

            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();

            randomPercentileChance = new Random();

            wins = 0;
            timesVisited = 1;
            turn = currentTurn;
            int myTurn = turn == 0 ? 0 : 1;
            int opponentTurn = turn == 0 ? 1 : 0;
            possibleMoves = PossibleMovesFromPosition(myTurn, opponentTurn);
            possibleBlocks = GetBlockingWalls(BoardUtil.PlayerCoordinateToString(playerLocations[opponentTurn]));

            if (isHardAI)
            {
                possibleWalls = GetBestNonImmediateBlockWalls(BoardUtil.PlayerCoordinateToString(playerLocations[opponentTurn]));
            }

            int locationOfPreviousMove = DoesMoveListContain(possibleMoves, lastPlayerMove[myTurn]);

            if (possibleMoves.Count != 1 && locationOfPreviousMove != -1)
            {
                possibleMoves.RemoveAt(locationOfPreviousMove);
            }

            gameOver = false;
            parent = null;
        }

        private void SetIllegalWalls()
        {
            foreach (WallCoordinate wall in walls)
            {
                string wallString = wall.StandardNotationString;
                if (wall.Orientation == WallCoordinate.WallOrientation.Horizontal)
                {
                    if (possibleHorizontalWalls.Contains(Convert.ToChar(wallString[0] - 1).ToString() + wallString[1]))
                    {
                        illegalWalls.Add(Convert.ToChar(wallString[0] - 1).ToString() + wallString[1] + "h");
                    }
                    if (possibleHorizontalWalls.Contains(Convert.ToChar(wallString[0] + 1).ToString() + wallString[1]))
                    {
                        illegalWalls.Add(Convert.ToChar(wallString[0] + 1).ToString() + wallString[1] + "h");
                    }
                }
                else
                {
                    if (possibleHorizontalWalls.Contains(wallString[0] + Convert.ToChar(wallString[1] - 1).ToString()))
                    {
                        illegalWalls.Add(wallString[0] + Convert.ToChar(wallString[1] - 1).ToString() + "v");
                    }
                    if (possibleHorizontalWalls.Contains(wallString[0] + Convert.ToChar(wallString[1] + 1).ToString()))
                    {
                        illegalWalls.Add(wallString[0] + Convert.ToChar(wallString[1] + 1).ToString() + "v");
                    }
                }
            }
        }
        /// <summary>
        /// MonteCarloNode Constructor to create a node which corresponds to a wall being placed
        /// </summary>
        /// <param name="move"></param>
        /// <param name="totals"></param>
        /// <param name="players"></param>
        /// <param name="wallCounts"></param>
        /// <param name="availableWalls"></param>
        /// <param name="illegalWallPlacements"></param>
        /// <param name="wallCoordinates"></param>
        /// <param name="newWallCoordinate"></param>
        /// <param name="currentTurn"></param>
        /// <param name="depth"></param>
        /// <param name="childParent"></param>
        private MonteCarloNode(string move, List<Dictionary<string, Tuple<double, double>>> totals, List<PlayerCoordinate> players, List<int> wallCounts, List<string> availableWalls, List<string> illegalWallPlacements, List<WallCoordinate> wallCoordinates, WallCoordinate newWallCoordinate, GameBoard.PlayerEnum currentTurn, int depth, MonteCarloNode childParent)
        {

            turn = currentTurn;
            parent = childParent;
            board = childParent.GetBoard();
            thisMove = move;
            depthCheck = depth;
            moveTotals = new List<Dictionary<string, Tuple<double, double>>>(totals);
            wins = 0;
            timesVisited = 0;

            lastPlayerMove = new List<string>(parent.lastPlayerMove);

            playerLocations = new List<PlayerCoordinate>(players);

            wallsRemaining = new List<int>(wallCounts);

            walls = new List<WallCoordinate>(wallCoordinates);

            illegalWalls = new List<string>(illegalWallPlacements);

            children = new List<MonteCarloNode>();
            childrensMoves = new List<string>();

            possibleBlocksList = new List<string>();
            possibleWalls = new List<Tuple<string, double>>();

            possibleHorizontalWalls = possibleVerticalWalls = new List<string>(availableWalls);

            possibleHorizontalWalls.Remove(move.Substring(0, 2));

            if (move[2] == 'v')
            {
                illegalWalls.Add(move[0] + Convert.ToChar(move[1] + 1).ToString() + move[2]);
                illegalWalls.Add(move[0] + Convert.ToChar(move[1] - 1).ToString() + move[2]);
            }
            else
            {
                illegalWalls.Add(Convert.ToChar(move[0] + 1).ToString() + move[1] + move[2]);
                illegalWalls.Add(Convert.ToChar(move[0] - 1).ToString() + move[1] + move[2]);
            }

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

            int myTurn = turn == 0 ? 0 : 1;
            int opponentTurn = turn == 0 ? 1 : 0;

            possibleMoves = PossibleMovesFromPosition(myTurn, opponentTurn);

            if (possibleMoves.Count == 0)
            {
                possibleMoves = PossibleMovesFromPosition(myTurn, opponentTurn);
            }

            int locationOfPreviousMove = DoesMoveListContain(possibleMoves, parent.lastPlayerMove[myTurn]);


            if (possibleMoves.Count != 1 && (parent != null ? locationOfPreviousMove != -1 : false))
            {
                possibleMoves.RemoveAt(locationOfPreviousMove);
            }

            if (wallsRemaining[myTurn] > 0)
            {
                possibleBlocks = GetBlockingWalls(BoardUtil.PlayerCoordinateToString(playerLocations[opponentTurn]));

                if (isHardAI)
                {
                    possibleWalls = GetBestNonImmediateBlockWalls(BoardUtil.PlayerCoordinateToString(playerLocations[opponentTurn]));
                }

            }
        }

        private int DoesMoveListContain(List<Tuple<string, double>> possibleMoves, string v)
        {
            int indexOfMove = -1;
            double lowestValue = possibleMoves[0].Item2;

            for (int index = 0; index < possibleMoves.Count && indexOfMove == -1; index++)
            {
                if (possibleMoves[index].Item1 == v && possibleMoves[index].Item2 != lowestValue)
                {
                    indexOfMove = index;
                }
            }

            return indexOfMove;
        }
        /// <summary>
        /// MonteCarloNode constructor that corresponds to a move being made
        /// </summary>
        /// <param name="move"></param>
        /// <param name="totals"></param>
        /// <param name="depth"></param>
        /// <param name="availableWalls"></param>
        /// <param name="illegalWallPlacements"></param>
        /// <param name="childParent"></param>
        private MonteCarloNode(string move, List<Dictionary<string, Tuple<double, double>>> totals, int depth, List<string> availableWalls, List<string> illegalWallPlacements, MonteCarloNode childParent)
        {
            parent = childParent;
            board = childParent.GetBoard();
            thisMove = move;
            depthCheck = depth;
            moveTotals = new List<Dictionary<string, Tuple<double, double>>>(totals);
            wins = 0;
            timesVisited = 0;

            possibleBlocksList = new List<string>();
            possibleWalls = new List<Tuple<string, double>>();

            playerLocations = new List<PlayerCoordinate>();
            playerLocations.Add(new PlayerCoordinate(parent.playerLocations[0].Row, parent.playerLocations[0].Col));
            playerLocations.Add(new PlayerCoordinate(parent.playerLocations[1].Row, parent.playerLocations[1].Col));

            lastPlayerMove = new List<string>();
            if (childParent.turn == 0)
            {
                if (BoardUtil.IsMoveAdjacentToPosition(move, playerLocations[0]))
                {
                    lastPlayerMove.Add(Convert.ToChar(97 + playerLocations[0].Col / 2).ToString() + (9 - (playerLocations[0].Row / 2)).ToString());
                }
                else
                {
                    lastPlayerMove.Add(childParent.lastPlayerMove[0]);
                }
                lastPlayerMove.Add(childParent.lastPlayerMove[1]);
            }
            else
            {
                lastPlayerMove.Add(childParent.lastPlayerMove[0]);

                if (BoardUtil.IsMoveAdjacentToPosition(move, playerLocations[1]))
                {
                    lastPlayerMove.Add(Convert.ToChar(97 + playerLocations[1].Col / 2).ToString() + (9 - (playerLocations[1].Row / 2)).ToString());
                }
                else
                {
                    lastPlayerMove.Add(childParent.lastPlayerMove[1]);
                }
            }

            wallsRemaining = new List<int>(childParent.wallsRemaining);

            illegalWalls = new List<string>(illegalWallPlacements);
            walls = new List<WallCoordinate>(childParent.walls);
            possibleHorizontalWalls = possibleVerticalWalls = new List<string>(availableWalls);

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
                int myTurn = turn == 0 ? 0 : 1;
                int opponentTurn = turn == 0 ? 1 : 0;

                possibleMoves = PossibleMovesFromPosition(myTurn, opponentTurn);

                if (possibleMoves.Count == 0)
                {
                    possibleMoves = PossibleMovesFromPosition(myTurn, opponentTurn);
                }

                int locationOfPreviousMove = DoesMoveListContain(possibleMoves, childParent.lastPlayerMove[myTurn]);

                if (possibleMoves.Count != 1 && locationOfPreviousMove != -1)
                {
                    possibleMoves.RemoveAt(locationOfPreviousMove);
                }

                if (wallsRemaining[myTurn] > 0)
                {
                    possibleBlocks = GetBlockingWalls(BoardUtil.PlayerCoordinateToString(playerLocations[opponentTurn]));

                    if (isHardAI)
                    {
                        possibleWalls = GetBestNonImmediateBlockWalls(BoardUtil.PlayerCoordinateToString(playerLocations[opponentTurn]));
                    }

                }
            }
        }

        private void PossibleHorizontalDiagonalJumps(List<Tuple<string, double>> validMoves, int goal, int direction)
        {
            if (playerLocations[goal == 9 ? 0 : 1].Row + 1 < 17 && playerLocations[goal == 9 ? 0 : 1].Row - 1 > -1
                       && playerLocations[goal == 9 ? 0 : 1].Col + 2 * direction < 17 && playerLocations[goal == 9 ? 0 : 1].Col + 2 * direction > -1)
            {
                if (!board[playerLocations[goal == 9 ? 0 : 1].Row - 1].Get(playerLocations[goal == 9 ? 0 : 1].Col + 2 * direction))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[goal == 9 ? 1 : 0].Col / 2)));
                    sb.Append(value: 9 - (playerLocations[goal == 9 ? 1 : 0].Row / 2) + 1 > 9 ? 9
                                   : 9 - (playerLocations[goal == 9 ? 1 : 0].Row / 2) + 1);

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                if (!board[playerLocations[goal == 9 ? 0 : 1].Row + 1].Get(playerLocations[goal == 9 ? 0 : 1].Col + 2 * direction))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(97 + (playerLocations[goal == 9 ? 1 : 0].Col / 2)));
                    sb.Append(value: 9 - (playerLocations[goal == 9 ? 1 : 0].Row / 2) - 1 < 1 ? 1
                                   : 9 - (playerLocations[goal == 9 ? 1 : 0].Row / 2) - 1);

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
            }
        }

        private void PossibleHorizontalJumps(List<Tuple<string, double>> validMoves, int goal, int direction)
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

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                else
                {
                    PossibleHorizontalDiagonalJumps(validMoves, goal, direction);
                }
            }
            else
            {
                PossibleHorizontalDiagonalJumps(validMoves, goal, direction);
            }
        }

        private void PossibleVerticalDiagonalJumps(List<Tuple<string, double>> validMoves, int goal, int direction)
        {
            if (playerLocations[goal == 9 ? 0 : 1].Col + 1 < 17 && playerLocations[goal == 9 ? 0 : 1].Col - 1 > -1
                        && playerLocations[goal == 9 ? 0 : 1].Row + 2 * direction < 17 && playerLocations[goal == 9 ? 0 : 1].Row + 2 * direction > -1)
            {
                if (!board[playerLocations[goal == 9 ? 0 : 1].Row + 2 * direction].Get(playerLocations[goal == 9 ? 0 : 1].Col + 1))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + (playerLocations[goal == 9 ? 1 : 0].Col / 2) + 1 > 105 ? 105
                                                  : 97 + (playerLocations[goal == 9 ? 1 : 0].Col / 2) + 1));
                    sb.Append(9 - (playerLocations[goal == 9 ? 1 : 0].Row / 2));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                if (!board[playerLocations[goal == 9 ? 0 : 1].Row + 2 * direction].Get(playerLocations[goal == 9 ? 0 : 1].Col - 1))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(Convert.ToChar(value: 97 + (playerLocations[goal == 9 ? 1 : 0].Col / 2) - 1 < 97 ? 97
                                                  : 97 + (playerLocations[goal == 9 ? 1 : 0].Col / 2) - 1));
                    sb.Append(9 - (playerLocations[goal == 9 ? 1 : 0].Row / 2));

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
            }
        }

        private void PossibleVerticalJumps(List<Tuple<string, double>> validMoves, int goal, int direction)
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

                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                else
                {
                    PossibleVerticalDiagonalJumps(validMoves, goal, direction);
                }
            }
            else
            {
                PossibleVerticalDiagonalJumps(validMoves, goal, direction);
            }
        }

        private List<Tuple<string, double>> PossibleMovesFromPosition(int playerSpot, int opponentSpot)
        {
            List<Tuple<string, double>> validMoves = new List<Tuple<string, double>>();

            int currentPlayer = playerSpot;
            int opponent = opponentSpot;
            int goal = playerSpot == 0 ? 9 : 1;
            lock (boardAccess)
            {
                Populate();
                if (PlayersAreAdjacent())
                {
                    if (playerLocations[currentPlayer].Row == playerLocations[opponent].Row)
                    {
                        if (playerLocations[currentPlayer].Col < playerLocations[opponent].Col)
                        {
                            PossibleHorizontalJumps(validMoves, goal, 1);
                        }
                        else
                        {
                            PossibleHorizontalJumps(validMoves, goal, -1);
                        }
                    }
                    else
                    {
                        if (playerLocations[currentPlayer].Row < playerLocations[opponent].Row)
                        {
                            PossibleVerticalJumps(validMoves, goal, 1);
                        }
                        else
                        {
                            PossibleVerticalJumps(validMoves, goal, -1);
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
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                if (playerLocations[currentPlayer].Row - 1 > -1 && !board[playerLocations[currentPlayer].Row - 1].Get(playerLocations[currentPlayer].Col)
                     && (playerLocations[currentPlayer].Row - 2 != playerLocations[opponent].Row || playerLocations[currentPlayer].Col != playerLocations[opponent].Col))
                {
                    //North
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2)));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2) + 1 > 9 ? 9 : 9 - (playerLocations[currentPlayer].Row / 2) + 1);
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                if (playerLocations[currentPlayer].Col + 1 < 17 && !board[playerLocations[currentPlayer].Row].Get(playerLocations[currentPlayer].Col + 1)
                    && (playerLocations[currentPlayer].Row != playerLocations[opponent].Row || playerLocations[currentPlayer].Col + 2 != playerLocations[opponent].Col))
                {
                    //East
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2) + 1));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2));
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }
                if (playerLocations[currentPlayer].Col - 1 > -1 && !board[playerLocations[currentPlayer].Row].Get(playerLocations[currentPlayer].Col - 1)
                    && (playerLocations[currentPlayer].Row != playerLocations[opponent].Row || playerLocations[currentPlayer].Col - 2 != playerLocations[opponent].Col))
                {
                    //West
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Convert.ToChar(97 + (playerLocations[currentPlayer].Col / 2) - 1));
                    sb.Append(9 - (playerLocations[currentPlayer].Row / 2));
                    validMoves.Add(new Tuple<string, double>(sb.ToString(), MinimumHeuristicEstimate(sb.ToString(), goal)));
                }

                Unpopulate();

                validMoves.Sort(delegate (Tuple<string, double> lValue, Tuple<string, double> rValue)
                {
                    if (lValue.Item2 == rValue.Item2) return 0;
                    else return lValue.Item2.CompareTo(rValue.Item2);
                });
                int indexToStartDeletions = -1;
                double lowestValue = validMoves[0].Item2;

                for (int i = 1; i < validMoves.Count && indexToStartDeletions == -1; i++)
                {
                    if (validMoves[i].Item2 != lowestValue)
                    {
                        indexToStartDeletions = i;
                    }
                }

                if (indexToStartDeletions != -1)
                {
                    validMoves.RemoveRange(indexToStartDeletions, validMoves.Count - indexToStartDeletions);
                }

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

        private bool CanPlayersReachGoal(WallCoordinate wallCoordinate)
        {
            lock (boardAccess)
            {
                Populate();

                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, true);
                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, true);

                int canPlayerOneReachGoal = ShortestPathfinder(BoardUtil.PlayerCoordinateToString(playerLocations[0]), 9);
                int canPlayerTwoReachGoal = ShortestPathfinder(BoardUtil.PlayerCoordinateToString(playerLocations[1]), 1);

                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);
                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);

                Unpopulate();

                return canPlayerOneReachGoal > 0 && canPlayerTwoReachGoal > 0;
            }

        }

        public double MinimumHeuristicEstimate(string locationToStart, int goal)
        {
            lock (boardAccess)
            {
                int EndRow = goal;

                PlayerCoordinate start;
                WallCoordinate wallCoordinate = null;

                if (locationToStart.Length > 2)
                {
                    start = playerLocations[goal == 1 ? 0 : 1];

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

                    //SetPlayerMoveValues(wallCoordinate, mid);
                }

                double possibleMinimumHeuristic;

                possibleMinimumHeuristic = ShortestPathfinder(BoardUtil.PlayerCoordinateToString(start), goal);

                if (wallCoordinate != null)
                {
                    board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);

                    Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                    board[mid.Item1].Set(mid.Item2, false);

                    board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);

                    //ResetPlayerMoveValues(wallCoordinate, mid);
                }

                return possibleMinimumHeuristic;
            }
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

        private Tuple<int, int> FindMidpoint(PlayerCoordinate start, PlayerCoordinate destination)
        {
            return new Tuple<int, int>((start.Row + destination.Row) / 2, (start.Col + destination.Col) / 2);
        }

        private string RandomMove()
        {
            int value = (turn == 0 ? (playerLocations[1].Row / 2) : (8 - playerLocations[0].Row / 2));
            return randomPercentileChance.Next(0, 100) >= (turn == 0 ? playerLocations[1].Row >= 8 ? randomPercentValue : 10 + (10 * value) : playerLocations[0].Row <= 8 ? randomPercentValue : 10 + (10 * value)) ? FindPlayerMove() : (turn == 0 ? wallsRemaining[0] : wallsRemaining[1]) > 0 ? FindWall() : FindPlayerMove();
        }

        private string FindPlayerMove(bool calledFromFindWall = false)
        {
            bool canBlockForGain = (DoesOpponentHaveEndRowMove() || CanBlockToIncreasePathByLargeAmount()) && wallsRemaining[turn == 0 ? 0 : 1] > 0 && AtLeastOneBlockLegal() && !calledFromFindWall && !DoIHaveAEndRowMove();
            if (!canBlockForGain)
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
            else
            {
                return FindWall(true);
            }
        }

        private bool CanBlockToIncreasePathByLargeAmount()
        {
            string opponent = BoardUtil.PlayerCoordinateToString(playerLocations[turn == 0 ? 1 : 0]);
            double opponentPath = MinimumHeuristicEstimate(opponent, turn == 0 ? 9 : 1);
            bool blockExistsThatBlocksForMoreThanTwo = false;

            foreach (Tuple<string, double> wall in possibleBlocks)
            {
                if (MinimumHeuristicEstimate(wall.Item1, turn == 0 ? 9 : 1) - opponentPath > 2)
                {
                    blockExistsThatBlocksForMoreThanTwo = true;
                }
            }

            return blockExistsThatBlocksForMoreThanTwo;
        }

        private bool DoIHaveAEndRowMove()
        {
            bool containsEndRowMove = false;

            for (int i = 0; i < possibleMoves.Count && !containsEndRowMove; i++)
            {
                if (possibleMoves[i].Item1[1] == (turn == 0 ? '9' : '1'))
                {
                    containsEndRowMove = true;
                }
            }

            return containsEndRowMove;
        }

        private bool AtLeastOneBlockLegal()
        {
            bool oneBlockLegal = false;
            foreach (Tuple<string, double> block in possibleBlocks)
            {
                if (!illegalWalls.Contains(block.Item1))
                {
                    oneBlockLegal = true;
                }
            }
            return oneBlockLegal;
        }

        private bool DoesOpponentHaveEndRowMove()
        {
            bool containsEndRowMove = false;
            List<Tuple<string, double>> opponentsMoves = PossibleMovesFromPosition(turn == 0 ? 1 : 0, turn == 0 ? 0 : 1);

            for (int i = 0; i < opponentsMoves.Count && !containsEndRowMove; i++)
            {
                if (opponentsMoves[i].Item1[1] == (turn == 0 ? '1' : '9'))
                {
                    containsEndRowMove = true;
                }
            }

            return containsEndRowMove;
        }

        private string FindWall(bool calledForPreventiveBlock = false)
        {
            lock (boardAccess)
            {
                if (possibleVerticalWalls.Count > 0)
                {
                    string wallMove = null;
                    PlayerCoordinate opponent = turn == 0 ? playerLocations[1] : playerLocations[0];

                    if (randomPercentileChance.Next(0, 100) >= 50)
                    {
                        if (possibleBlocks == null)
                        {
                            possibleBlocks = GetBlockingWalls(BoardUtil.PlayerCoordinateToString(playerLocations[turn == 0 ? 1 : 0]));
                        }

                        if (possibleBlocks.Count > 0)
                        {
                            wallMove = possibleBlocks[0].Item1;
                        }
                    }
                    else
                    {
                        if (isHardAI)
                        {
                            if (possibleWalls == null)
                            {
                                possibleWalls = GetBestNonImmediateBlockWalls(BoardUtil.PlayerCoordinateToString(playerLocations[turn == 0 ? 1 : 0]));
                            }

                            if (possibleWalls.Count > 0)
                            {
                                wallMove = possibleWalls[0].Item1;

                                for (int i = 1; (childrensMoves.Contains(wallMove) || illegalWalls.Contains(wallMove)) && i < possibleWalls.Count; ++i)
                                {
                                    wallMove = possibleWalls[i].Item1;
                                }

                                if (childrensMoves.Contains(wallMove) && illegalWalls.Contains(wallMove))
                                {
                                    for (int i = 0; i < possibleWalls.Count && illegalWalls.Contains(wallMove); ++i)
                                    {
                                        wallMove = possibleWalls[i].Item1;
                                    }
                                    if (illegalWalls.Contains(wallMove))
                                    {
                                        wallMove = FindPlayerMove(true);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (possibleBlocks == null)
                            {
                                possibleBlocks = GetBlockingWalls(BoardUtil.PlayerCoordinateToString(playerLocations[turn == 0 ? 1 : 0]));
                            }

                            if (possibleBlocks.Count > 0)
                            {
                                wallMove = possibleBlocks[0].Item1;
                            }
                        }
                    }

                    if (wallMove != null)
                    {
                        return wallMove;
                    }
                    else if (!calledForPreventiveBlock)
                    {
                        return FindPlayerMove(true);
                    }
                    else
                    {
                        return FindWall(true);
                    }
                }
                else
                {
                    return FindPlayerMove(true);
                }
            }

        }

        private List<Tuple<string, double>> GetBlockingWalls(string opponent)
        {
            Populate();
            int goal = turn == 0 ? 9 : 1;
            int opponentGoal = turn == 0 ? 1 : 9;
            double opponentEstimate = MinimumHeuristicEstimate(opponent, opponentGoal);

            List<string> blockingWalls = new List<string>();
            for (char col = Convert.ToChar(opponent[0] - 1); col < opponent[0] + 1; col++)
            {
                for (char row = Convert.ToChar(opponent[1] - 1); row < opponent[1] + 1; row++)
                {
                    if (possibleHorizontalWalls.Contains(col.ToString() + row.ToString()))
                    {
                        blockingWalls.Add(col.ToString() + row.ToString());
                    }
                }
            }

            List<Tuple<string, double>> validBlocks = new List<Tuple<string, double>>();

            foreach (string placement in blockingWalls)
            {
                string horizontalPlacement = placement + "h";
                string verticalPlacement = placement + "v";

                if (!illegalWalls.Contains(horizontalPlacement) && opponentEstimate < MinimumHeuristicEstimate(horizontalPlacement, opponentGoal) /*placement[1] != Convert.ToChar(opponent[1] + (turn == 0 ? 1 : -1))*/)
                {
                    validBlocks.Add(new Tuple<string, double>(horizontalPlacement, MinimumHeuristicEstimate(horizontalPlacement, goal)));
                    possibleBlocksList.Add(horizontalPlacement);
                }
                if (!illegalWalls.Contains(verticalPlacement) && opponentEstimate < MinimumHeuristicEstimate(verticalPlacement, opponentGoal))
                {
                    validBlocks.Add(new Tuple<string, double>(verticalPlacement, MinimumHeuristicEstimate(horizontalPlacement, goal)));
                    possibleBlocksList.Add(verticalPlacement);
                }

            }

            Unpopulate();
            //if (turn == 0)
            //{
            //    return validBlocks.OrderByDescending(vB => vB.Item2).ThenBy(vB => vB.Item1[2]).ToList();
            //}
            //else
            //{
            return validBlocks.OrderBy(vB => vB.Item2).ThenBy(vB => vB.Item1[2]).ToList();
            //}
        }


        private List<Tuple<string, double>> GetBestNonImmediateBlockWalls(string opponent)
        {
            Populate();
            int goal = turn == 0 ? 9 : 1;
            int opponentGoal = turn == 0 ? 1 : 9;
            double opponentEstimate = MinimumHeuristicEstimate(opponent, opponentGoal);

            List<Tuple<string, double>> validBlocks = new List<Tuple<string, double>>();

            foreach (string placement in possibleHorizontalWalls)
            {
                string horizontalPlacement = placement + "h";
                string verticalPlacement = placement + "v";

                if (!illegalWalls.Contains(horizontalPlacement) && !possibleBlocksList.Contains(horizontalPlacement))
                {
                    Tuple<double, int> heuristicEstimatePlayer = AverageHeuristicEstimateOfNearbyWalls(horizontalPlacement, goal);
                    Tuple<double, int> heuristicEstimateOpponent = AverageHeuristicEstimateOfNearbyWalls(horizontalPlacement, opponentGoal);
                    double horizontalHeuristic = (MinimumHeuristicEstimate(horizontalPlacement, goal) + heuristicEstimatePlayer.Item1) / 2;
                    double opponentHorizontalHeuristic = (MinimumHeuristicEstimate(horizontalPlacement, opponentGoal) + heuristicEstimateOpponent.Item1) / 2;

                    if (opponentEstimate < opponentHorizontalHeuristic && horizontalHeuristic < opponentHorizontalHeuristic)
                    {
                        validBlocks.Add(new Tuple<string, double>(horizontalPlacement, opponentHorizontalHeuristic - horizontalHeuristic));
                    }
                }
                if (!illegalWalls.Contains(verticalPlacement) && !possibleBlocksList.Contains(verticalPlacement))
                {
                    Tuple<double, int> heuristicEstimatePlayer = AverageHeuristicEstimateOfNearbyWalls(verticalPlacement, goal);
                    Tuple<double, int> heuristicEstimateOpponent = AverageHeuristicEstimateOfNearbyWalls(verticalPlacement, opponentGoal);
                    double verticalHeuristic = (MinimumHeuristicEstimate(verticalPlacement, goal) + heuristicEstimatePlayer.Item1) / 2;
                    double opponentVerticalHeuristic = (MinimumHeuristicEstimate(verticalPlacement, opponentGoal) + heuristicEstimateOpponent.Item1) / 2;

                    if (opponentEstimate < opponentVerticalHeuristic && verticalHeuristic < opponentVerticalHeuristic)
                    {
                        validBlocks.Add(new Tuple<string, double>(verticalPlacement, opponentVerticalHeuristic - verticalHeuristic));
                    }
                }

            }
            Unpopulate();
            if (turn == 0)
            {
                return validBlocks.OrderBy(vB => vB.Item2).ThenBy(vB => vB.Item1[2]).ToList();
            }
            else
            {
                return validBlocks.OrderByDescending(vB => vB.Item2).ThenBy(vB => vB.Item1[2]).ToList();
            }
        }

        private Tuple<double, int> AverageHeuristicEstimateOfNearbyWalls(string wallPlacement, int goal)
        {
            if (wallPlacement[2] == 'h')
            {
                return AverageHeuristicEstimateOfNearbyWallsHorizontal(wallPlacement, goal);
            }
            else
            {
                return AverageHeuristicEstimateOfNearbyWallsVertical(wallPlacement, goal);
            }
        }

        private Tuple<double, int> AverageHeuristicEstimateOfNearbyWallsHorizontal(string wallPlacement, int goal)
        {
            List<Tuple<string, double>> nearbyWalls = new List<Tuple<string, double>>();
            double total = 0;

            if (Convert.ToChar(wallPlacement[0] - 1) >= 'a')
            {
                string newWall = Convert.ToChar(wallPlacement[0] - 1).ToString() + wallPlacement[1] + 'v';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                if (Convert.ToChar(wallPlacement[1] + 1) < '9')
                {
                    newWall = Convert.ToChar(wallPlacement[0] - 1).ToString() + Convert.ToChar(wallPlacement[1] + 1).ToString() + 'v';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[1] - 1) >= '1')
                {
                    newWall = Convert.ToChar(wallPlacement[0] - 1).ToString() + Convert.ToChar(wallPlacement[1] - 1).ToString() + 'v';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[0] - 2) >= 'a')
                {
                    newWall = Convert.ToChar(wallPlacement[0] - 2).ToString() + wallPlacement[1] + 'h';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
            }
            if (Convert.ToChar(wallPlacement[0] + 1) < 'i')
            {
                string newWall = Convert.ToChar(wallPlacement[0] + 1).ToString() + wallPlacement[1] + 'v';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                if (Convert.ToChar(wallPlacement[1] + 1) < '9')
                {
                    newWall = Convert.ToChar(wallPlacement[0] + 1).ToString() + Convert.ToChar(wallPlacement[1] + 1).ToString() + 'v';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[1] - 1) >= '1')
                {
                    newWall = Convert.ToChar(wallPlacement[0] + 1).ToString() + Convert.ToChar(wallPlacement[1] - 1).ToString() + 'v';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[0] + 2) < 'i')
                {
                    newWall = Convert.ToChar(wallPlacement[0] + 2).ToString() + wallPlacement[1] + 'h';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
            }
            if (Convert.ToChar(wallPlacement[1] + 1) < '9')
            {
                string newWall = wallPlacement[0] + Convert.ToChar(wallPlacement[1] + 1).ToString() + 'v';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
            }
            if (Convert.ToChar(wallPlacement[1] - 1) >= '1')
            {
                string newWall = wallPlacement[0] + Convert.ToChar(wallPlacement[1] - 1).ToString() + 'v';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
            }

            return new  
        }

        private double AverageHeuristicEstimateOfNearbyWallsVertical(string wallPlacement, int goal)
        {
            List<Tuple<string, double>> nearbyWalls = new List<Tuple<string, double>>();
            double total = 0;

            if (Convert.ToChar(wallPlacement[1] - 1) >= '1')
            {
                string newWall = wallPlacement[0] + Convert.ToChar(wallPlacement[1] - 1).ToString() + 'h';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);

                if (Convert.ToChar(wallPlacement[0] + 1) < 'i')
                {
                    newWall = Convert.ToChar(wallPlacement[0] + 1).ToString() + Convert.ToChar(wallPlacement[1] - 1).ToString() + 'h';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[0] - 1) >= 'a')
                {
                    newWall = Convert.ToChar(wallPlacement[0] - 1).ToString() + Convert.ToChar(wallPlacement[1] - 1).ToString() + 'h';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[1] - 2) >= '1')
                {
                    newWall = wallPlacement[0] + Convert.ToChar(wallPlacement[1] - 2).ToString() + 'v';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
            }
            if (Convert.ToChar(wallPlacement[1] + 1) < '9')
            {
                string newWall = wallPlacement[0] + Convert.ToChar(wallPlacement[1] + 1).ToString() + 'h';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);

                if (Convert.ToChar(wallPlacement[0] + 1) < 'i')
                {
                    newWall = Convert.ToChar(wallPlacement[0] + 1).ToString() + Convert.ToChar(wallPlacement[1] + 1).ToString() + 'h';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[0] - 1) >= 'a')
                {
                    newWall = Convert.ToChar(wallPlacement[0] - 1).ToString() + Convert.ToChar(wallPlacement[1] + 1).ToString() + 'h';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
                if (Convert.ToChar(wallPlacement[1] + 2) < '9')
                {
                    newWall = wallPlacement[0] + Convert.ToChar(wallPlacement[1] + 2).ToString() + 'v';
                    total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
                }
            }
            if (Convert.ToChar(wallPlacement[0] + 1) < 'i')
            {
                string newWall = Convert.ToChar(wallPlacement[0] + 1).ToString() + wallPlacement[1] + 'h';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
            }
            if (Convert.ToChar(wallPlacement[0] - 2) >= 'a')
            {
                string newWall = Convert.ToChar(wallPlacement[0] - 2).ToString() + wallPlacement[1] + 'h';
                total = AddWallTotalToList(nearbyWalls, total, newWall, goal);
            }

            possibleWalls = new List<Tuple<string, double>>(nearbyWalls);

            return total / nearbyWalls.Count;
        }

        double AddWallTotalToList(List<Tuple<string, double>> nearbyWalls, double total, string newWall, int goal)
        {
            if (!illegalWalls.Contains(newWall))
            {
                Tuple<string, double> temporaryWall = new Tuple<string, double>(newWall, MinimumHeuristicEstimate(newWall, goal));
                nearbyWalls.Add(temporaryWall);
                total += temporaryWall.Item2;
            }
            return total;
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

                //SetPlayerMoveValues(wallCoordinate, mid);
            }
        }

        private void Unpopulate()
        {
            foreach (var wallCoordinate in walls)
            {
                board[wallCoordinate.StartRow].Set(wallCoordinate.StartCol, false);

                Tuple<int, int> mid = FindMidpoint(new PlayerCoordinate(wallCoordinate.StartRow, wallCoordinate.StartCol), new PlayerCoordinate(wallCoordinate.EndRow, wallCoordinate.EndCol));
                board[mid.Item1].Set(mid.Item2, false);

                board[wallCoordinate.EndRow].Set(wallCoordinate.EndCol, false);
                //ResetPlayerMoveValues(wallCoordinate, mid);
            }
        }

        //Selection Phase Code
        /// <summary>
        /// SelectNode selects a node at random given a nodes children. If there are no nodes available the function returns -1 otherwise it returns the index of the selcted node.
        /// </summary>
        /// <returns></returns>
        public MonteCarloNode SelectNode(MonteCarloNode root, List<Tuple<string, MonteCarloNode>> path)
        {
            if (root.children.Count != 0 && (randomPercentileChance.Next(1, 100) >= 51))
            {
                MonteCarloNode nextNode = SelectionAlgorithm(root.children);
                if (path.Count > 0 && ExistsWithin(nextNode, path))
                {
                    List<MonteCarloNode> listOfChildren = new List<MonteCarloNode>(root.children);
                    listOfChildren.Remove(nextNode);
                    nextNode = SelectionAlgorithm(listOfChildren);

                    while (path.Count > 0 && ExistsWithin(nextNode, path))
                    {
                        listOfChildren = new List<MonteCarloNode>(listOfChildren);
                        listOfChildren.Remove(nextNode);
                        nextNode = SelectionAlgorithm(listOfChildren);
                    }

                    if (path.Count > 0 && ExistsWithin(nextNode, path))
                    {
                        return root;
                    }

                }
                path.Add(new Tuple<string, MonteCarloNode>(nextNode.thisMove, nextNode));
                return root.SelectNode(nextNode, path);

            }
            else
            {
                return root;
            }

        }

        private bool ExistsWithin(MonteCarloNode nextNode, List<Tuple<string, MonteCarloNode>> path)
        {
            bool existsWithin = false;
            for (int i = 0; i < path.Count && !existsWithin; i++)
            {
                if (nextNode.thisMove == path[i].Item1 && nextNode.IdString() == path[i].Item2.IdString())
                {
                    existsWithin = true;
                }
            }
            return existsWithin;
        }

        /// <summary>
        /// The SelectionAlgorithm calculates a score for each move based on the knowledge currently in the tree.
        /// </summary>
        /// <returns></returns>
        private MonteCarloNode SelectionAlgorithm(List<MonteCarloNode> children)
        {
            lock (childrenAccess)
            {
                List<MonteCarloNode> newList = children.OrderBy(o => o.GetVisits()).ToList();

                return newList[newList.Count - 1];
            }
        }

        //Expansion Phase Code
        /// <summary>
        /// The <c>ExpandOptions</c> method calls the <c>RandomMove</c> method to generate a move to expand the current options from the current <c>MonteCarloNode</c>
        /// and returns true after it has expanded the child options.
        /// </summary>
        public MonteCarloNode ExpandOptions(MonteCarloNode root)
        {
            if (!root.gameOver)
            {
                string move;
                move = root.RandomMove();

                while (!root.InsertChild(move))
                {

                    move = root.RandomMove();

                }


                return root.children[root.FindMove(move)];
            }
            else
            {
                return root;
            }
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

            return child.StateValue(opponent, player) / child.timesVisited + explorationFactor
                            * Math.Sqrt(Math.Log(timesVisited) / child.GetVisits()) +
                            child.GetMoveTotalsRatio(move) *
                            (historyInfluence / (child.GetVisits() - child.GetWins() + 1));

        }

        private double GetMoveTotalsRatio(string move)
        {
            int index = turn == 0 ? 0 : 1;
            if (!moveTotals[index].ContainsKey(move))
            {
                moveTotals[index].Add(move, new Tuple<double, double>(0, 1));
                return moveTotals[index][move].Item1 / moveTotals[index][move].Item2;
            }
            else
            {
                return moveTotals[index][move].Item1 / moveTotals[index][move].Item2;
            }

        }

        private double StateValue(string opponent, string player)
        {
            int goalPlayer = turn == 0 ? 9 : 1;
            int goalOpponent = turn == 0 ? 1 : 9;
            return (Math.Atan(0.5 * (0.9 * ((MinimumHeuristicEstimate(player, goalPlayer) - MinimumHeuristicEstimate(opponent, goalOpponent))))) / Math.PI) + 0.5;
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
                                    MonteCarloNode newNode = new MonteCarloNode(move, moveTotals, playerLocations, wallsRemaining, possibleHorizontalWalls, illegalWalls, walls, new WallCoordinate(move), turn, depthCheck + 1, this);
                                    if (!visitedNodes.ContainsKey(newNode.IdString()))
                                    {
                                        children.Add(newNode);

                                        //if (moveTotals.ContainsKey(move))
                                        //{
                                        //    moveTotals[move] = new Tuple<double, double>(moveTotals[move].Item1, moveTotals[move].Item2 + 1);
                                        //}
                                        visitedNodes.Add(newNode.IdString(), newNode);
                                        childrensMoves.Add(move);
                                        //#if DEBUGEE
                                        //                            Console.WriteLine(move + ' ' + (turn == 0 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO).ToString());
                                        //#endif
                                    }
                                    else
                                    {
                                        children.Add(visitedNodes[newNode.IdString()]);
                                        childrensMoves.Add(move);
                                    }
                                }
                                successfulInsert = true;
                            }
                        }
                        else if (!illegalWalls.Contains(move))
                        {
                            illegalWalls.Add(move);
                        }
                    }
                    else if (!illegalWalls.Contains(move))
                    {
                        illegalWalls.Add(move);
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
                                MonteCarloNode newNode = new MonteCarloNode(move, moveTotals, depthCheck + 1, possibleHorizontalWalls, illegalWalls, this);
                                if (!visitedNodes.ContainsKey(newNode.IdString()))
                                {
                                    children.Add(newNode);

                                    //if (moveTotals.ContainsKey(move))
                                    //{
                                    //    moveTotals[move] = new Tuple<double, double>(moveTotals[move].Item1, moveTotals[move].Item2 + 1);
                                    //}
                                    visitedNodes.Add(newNode.IdString(), newNode);
                                    childrensMoves.Add(move);
                                    //#if DEBUG
                                    //                                Console.WriteLine(move + ' ' + (turn == 0 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO).ToString());
                                    //#endif
                                }
                                else
                                {
                                    children.Add(visitedNodes[newNode.IdString()]);
                                    childrensMoves.Add(move);
                                }
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

        private string IdString()
        {
            List<string> wallStrings = new List<string>();
            string returnString = BoardUtil.PlayerCoordinateToString(playerLocations[0]) + BoardUtil.PlayerCoordinateToString(playerLocations[1]);

            for (int i = 0; i < walls.Count; ++i)
            {
                wallStrings.Add(walls[i].StandardNotationString);
            }

            wallStrings = wallStrings.OrderByDescending(i => i).ToList();

            for (int i = 0; i < walls.Count; ++i)
            {
                returnString += "-" + wallStrings[i];
            }

            returnString += wallsRemaining[0].ToString() + '-' + wallsRemaining[1].ToString();

            return returnString;
        }

        //Simulation & Backpropagation Phase Code
        /// <summary>
        /// SimulatedGame evaluates a game from a node and plays a series of moves until it reaches an endstate when it recursively backpropagates and updates the previous nodes. 
        /// On a losing endstate the function returns false and true on a victory.
        /// </summary>
        /// <returns>Whether or not the function reached a victorious endstate</returns>

        public void Backpropagate(MonteCarloNode newlyAddedNode, List<Tuple<string, MonteCarloNode>> path, int pathIndex = -1, double result = 0)
        {
            lock (childrenAccess)
            {
                MonteCarloNode node = newlyAddedNode;

                if (pathIndex == -1)
                {
                    pathIndex = path.Count - 1;
                }
                else
                {
                    --pathIndex;
                }

                node.timesVisited++;

                if (result == 0)
                {
                    result = Evaluate(newlyAddedNode);
                    node.SetScore(result);
                }
                else
                {
                    node.SetScore(score + result);
                }

                int index = newlyAddedNode.turn == 0 ? 0 : 1;
                moveTotals[index][node.thisMove] = new Tuple<double, double>(moveTotals[index][node.thisMove].Item1 + result, moveTotals[index][node.thisMove].Item2 + 1);

                if (pathIndex >= 0 && node.parent.thisMove != path[pathIndex].Item1)
                {
                    Backpropagate(path[pathIndex].Item2, path, pathIndex, result);
                }

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
        public MonteCarlo(GameBoard boardState, bool isHard = false)
        {
            isHardAI = isHard;
            TreeSearch = new MonteCarloNode(boardState.GetPlayerCoordinate(GameBoard.PlayerEnum.ONE), boardState.GetPlayerCoordinate(GameBoard.PlayerEnum.TWO),
                                                              boardState.GetPlayerWallCount(GameBoard.PlayerEnum.ONE), boardState.GetPlayerWallCount(GameBoard.PlayerEnum.TWO),
                                                              boardState.GetWalls(), boardState.GetWhoseTurn() == 1 ? GameBoard.PlayerEnum.ONE : GameBoard.PlayerEnum.TWO,
                                                              boardState.GetLastStart(), isHardAI);
        }

        private void ThreadedTreeSearchEasy(Stopwatch timer, MonteCarloNode MonteCarlo)
        {
            for (/*int i = 0*/; /*i < 10000*/ /*&&*/ timer.Elapsed.TotalSeconds < 2.5; /*++i*/)
            {
                List<Tuple<string, MonteCarloNode>> path = new List<Tuple<string, MonteCarloNode>>();
                MonteCarlo.Backpropagate(MonteCarlo.ExpandOptions(MonteCarlo.SelectNode(MonteCarlo, path)), path);
            }
        }

        private void ThreadedTreeSearchHard(Stopwatch timer, MonteCarloNode MonteCarlo)
        {
            for (/*int i = 0*/; /*i < 10000*/ /*&&*/ timer.Elapsed.TotalSeconds < 4.5;/* ++i*/)
            {
                List<Tuple<string, MonteCarloNode>> path = new List<Tuple<string, MonteCarloNode>>();
                MonteCarlo.Backpropagate(MonteCarlo.ExpandOptions(MonteCarlo.SelectNode(MonteCarlo, path)), path);
            }
        }

        public string MonteCarloTreeSearch()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            List<Thread> simulatedGames = new List<Thread>();

            for (int i = 0; i < 8; ++i)
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
            List<MonteCarloNode> childrenToChoose = TreeSearch.GetChildrenNodes().OrderBy(o => o.GetVisits()).ToList();

            string move = childrenToChoose[childrenToChoose.Count - 1].GetMove();

            return move;
        }
    }
}
