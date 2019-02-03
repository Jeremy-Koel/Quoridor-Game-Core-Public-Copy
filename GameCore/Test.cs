using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class Test
    {
        static void Main(string[] args)
        {
            //TestWallPlacement();
            //TestDiagonalJump();
            CLI();
        }

        static void TestWallPlacement()
        {
            GameBoard board = GameBoard.GetInstance();
            board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate("a4h"));
            board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate("c4h"));
            board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate("e4h"));
            board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate("e5v"));
            board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate("f5h"));
            board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate("g5v"));
            board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate("h4h")); // This one will not work, and won't be added to board 

            board.PrintBoard();
            Console.WriteLine();
            Console.ReadKey();
        }

        static void TestDiagonalJump()
        {
            GameBoard board = GameBoard.GetInstance(GameBoard.PlayerEnum.ONE, "i4", "i5");
            board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate("h3h"));
            board.MovePiece(GameBoard.PlayerEnum.TWO, new PlayerCoordinate("h4"));
            
            board.PrintBoard();
            Console.WriteLine();
            Console.ReadKey();
        }

        static void CLI()
        {
            GameBoard board = GameBoard.GetInstance(GameBoard.PlayerEnum.ONE, "e1", "e9");
            int currentPlayer = 0, player1walls = 10, player2walls = 10;
            Random randomNumber = new Random();
            int oneOrTwo = randomNumber.Next(1, 2);
            if (oneOrTwo == 1)
                currentPlayer = 1;
            else if (oneOrTwo == 2)
                currentPlayer = 2;
            char input;
            string coordinates = "";
            Console.Write("1 for move;  2 for wall;  q for quit:  ");
            input = Console.ReadLine().ToCharArray()[0];
            bool validMove;
            while (input != 'q') {
                if (input == '1') {
                    if (currentPlayer == 1) { 
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
                                Console.Write("Invalid move. Try again;  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                    }
                    else if (currentPlayer == 2) {
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
                                Console.Write("Invalid move. Try again;  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                    }
                }
                else if (input == '2') {
                    if (currentPlayer == 1 && player1walls > 0) {
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
                                Console.Write("Invalid wall. Try again;  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                        
                    }
                    else if (currentPlayer == 2 && player2walls > 0) {
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
                                Console.Write("Invalid wall. Try again;  ");
                                validMove = false;
                            }
                        } while (validMove == false);
                    }
                }

                if (currentPlayer == 1) {
                    if (player1walls > 0) {
                        Console.Write("P1 -- 1 for move;  2 for wall (" + player1walls + ");  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                    else {
                        Console.Write("P1 -- 1 for move;  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                }
                if (currentPlayer == 2) {
                    if (player2walls > 0) {
                        Console.Write("P2 -- 1 for move;  2 for wall (" + player2walls + ");  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                    else {
                        Console.Write("P2 -- 1 for move;  q for quit:  ");
                        input = Console.ReadLine().ToCharArray()[0];
                    }
                }
            }
        }







    }
}
