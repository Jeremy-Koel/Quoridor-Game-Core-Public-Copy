using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class CLI
    {
        static void Main(string[] args)
        {
            int currentPlayer = 0, player1walls = 10, player2walls = 10;
            GameBoard board = new GameBoard("e4", "e5");
            currentPlayer = board.GetWhoseTurn();
            char input;
            string coordinates = "";
            Console.WriteLine("Player " + currentPlayer + " goes first!");
            board.PrintBoard();
            Console.Write("1 for move;  2 for wall;  q for quit:  ");
            input = Console.ReadLine().ToCharArray()[0];
            bool validMove;
            while (input != 'q')
            {
                if (input == '1')
                {
                    if (currentPlayer == 1)
                    {
                        do
                        {
                            validMove = true;
                            Console.Write("P1 -- Enter move coordinates:  ");
                            coordinates = Console.ReadLine();
                            if (board.MovePiece(GameBoard.PlayerEnum.ONE, new PlayerCoordinate(coordinates)) == true)
                            {
                                board.PrintBoard();
                                currentPlayer = 2;
                            }
                            else
                            {
                                Console.WriteLine("Invalid move. Try again.  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                    }
                    else if (currentPlayer == 2)
                    {
                        do
                        {
                            validMove = true;
                            Console.Write("P2 -- Enter move coordinates:  ");
                            coordinates = Console.ReadLine();
                            if (board.MovePiece(GameBoard.PlayerEnum.TWO, new PlayerCoordinate(coordinates)) == true)
                            {
                                board.PrintBoard();
                                currentPlayer = 1;
                            }
                            else
                            {
                                Console.WriteLine("Invalid move. Try again.  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                    }
                }
                else if (input == '2')
                {
                    if (currentPlayer == 1 && player1walls > 0)
                    {
                        do
                        {
                            validMove = true;
                            Console.Write("P1 -- Enter wall coordinates:  ");
                            coordinates = Console.ReadLine();
                            if (board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate(coordinates)) == true)
                            {
                                board.PrintBoard();
                                currentPlayer = 2;
                                player1walls--;
                            }
                            else
                            {
                                Console.WriteLine("Invalid wall. Try again.  ");
                                validMove = false;
                            }
                        } while (validMove == false);

                    }
                    else if (currentPlayer == 2 && player2walls > 0)
                    {
                        do
                        {
                            validMove = true;
                            Console.Write("P2 -- Enter wall coordinates:  ");
                            coordinates = Console.ReadLine();
                            if (board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate(coordinates)) == true)
                            {
                                board.PrintBoard();
                                currentPlayer = 1;
                                player2walls--;
                            }
                            else
                            {
                                Console.WriteLine("Invalid wall. Try again.  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                    }
                }

                if (currentPlayer == 1)
                {
                    if (player1walls > 0)
                    {
                        Console.Write("P1 -- 1 for move;  2 for wall (" + player1walls + ");  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                    else
                    {
                        Console.Write("P1 -- 1 for move;  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                }
                if (currentPlayer == 2)
                {
                    if (player2walls > 0)
                    {
                        Console.Write("P2 -- 1 for move;  2 for wall (" + player2walls + ");  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                    else
                    {
                        Console.Write("P2 -- 1 for move;  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                }
            }
        }
    }
}
