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
            GameBoard board = GameBoard.GetInstance();
            int currentPlayer = 0, player1walls = 10, player2walls = 10;
            Random randomNumber = new Random();
            int oneOrTwo = randomNumber.Next(1, 2);
            if (oneOrTwo == 1)
                currentPlayer = 1;
            else if (oneOrTwo == 2)
                currentPlayer = 2;
            char input;
            string coordinates = "";
            cout << "1 for move;  2 for wall;  q for quit:  ";
            cin >> input;
            while (input != 'q') {
                if (input == '1') {
                    if (currentPlayer == 1) {
                        cout << "P1 -- Enter move coordinates:  ";
                        cin >> coordinates;
                        if (board.MovePiece(GameBoard.PlayerEnum.ONE, new PlayerCoordinate(coordinates) == true)) {
                            currentPlayer = 2;
                        }
                        else {
                            cout << "Invalid move";
                        }
                    }
                    else if (currentPlayer == 2) {
                        cout << "P2 -- Enter move coordinates:  ";
                        cin >> coordinates;
                        if (board.MovePiece(GameBoard.PlayerEnum.TWO, new PlayerCoordinate(coordinates)) == true) {
                            currentPlayer = 1;
                        }
                        else {
                            cout << "Invalid move";
                        }
                    }
                }
                else if (input == '2') {
                    if (currentPlayer == 1 && player1walls > 0) {
                        cout << "P1 -- Enter wall coordinates:  ";
                        cin >> coordinates;
                        if (board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate(coordnates)) == true) {
                            currentPlayer = 2;
                        }
                        else {
                            cout << "Invalid wall";
                        }
                        
                    }
                    else if (currentPlayer == 2 && player2walls > 0) {
                        cout << "P2 -- Enter wall coordinates:  ";
                        cin >> coordinates;
                        if (board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate(coordinates)) == true) {
                            currentPlayer = 1;
                        }
                        else {
                            cout << "Invalid wall";
                        }
                    }
                }

                if (currentPlayer == 1) {
                    if (player1walls > 0) {
                        cout << "P1 -- 1 for move;  2 for wall;  q for quit:  ";
                        cin >> input;
                    }
                    else {
                        cout << "P1 -- 1 for move;  q for quit:  ";
                        cin >> input;
                    }
                }
                if (currentPlayer == 2) {
                    if (player2walls > 0) {
                        cout << "P2 -- 1 for move;  2 for wall;  q for quit:  ";
                        cin >> input;
                    }
                    else {
                        cout << "P2 -- 1 for move;  q for quit:  ";
                        cin >> input;
                    }
                }
            }
        }







    }
}
