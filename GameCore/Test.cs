using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class Test
    {
        static void Main(string[] args)
        {
            GameBoard board = GameBoard.GetInstance("e3","f4");
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
            
            board.MovePiece(GameBoard.PlayerEnum.ONE, new PlayerCoordinate("g5"));
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
            /*
            board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate("e2h")); // invalid move for testing 
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
            
            board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate("i1h")); // invalid move for testing 
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();*/
        }
    }
}
