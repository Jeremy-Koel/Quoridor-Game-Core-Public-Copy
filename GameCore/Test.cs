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
            TestDiagonalJump();
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
    }
}
