using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class Test
    {
        static void Main(string[] args)
        {
            GameBoard board = GameBoard.GetInstance();
            board.PrintBoard();

            Console.Write("\n"); 
            
            board.MovePiece(GameBoard.PlayerEnum.ONE, new PlayerCoordinate("e2"));
            board.PrintBoard();

            Console.Write("\n");
            
            board.MovePiece(GameBoard.PlayerEnum.TWO, new PlayerCoordinate("f9"));
            board.PrintBoard();

            Console.Write("\n");
            
            Console.ReadKey();
        }
    }
}
