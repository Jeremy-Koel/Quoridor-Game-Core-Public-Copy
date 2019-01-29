using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class Test
    {
        static void Main(string[] args)
        {
            GameBoard board = GameBoard.GetInstance("e1","e9");
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();

            board.MovePiece(GameBoard.PlayerEnum.ONE, new PlayerCoordinate("e2"));
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();

            board.PlaceWall(GameBoard.PlayerEnum.TWO, new WallCoordinate("e3h"));
            board.PrintBoard();
            Console.Write("\n\n");
            Console.ReadKey();
        }
    }
}
