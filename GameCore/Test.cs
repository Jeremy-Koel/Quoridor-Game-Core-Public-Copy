using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class Test
    {
        static void Main(string[] args)
        {
            GameBoard board = GameBoard.GetInstance("e4","f4");
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
            
            board.PlaceWall(GameBoard.PlayerEnum.ONE, new WallCoordinate("d4v"));
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
            
            board.MovePiece(GameBoard.PlayerEnum.TWO, new PlayerCoordinate("d4"));
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
        }
    }
}
