using System;

namespace GameCore
{
    class GameBoard
    {
        
        private char[,] board;
        private bool gameOver;

        public bool GameOver { get => gameOver; set => gameOver = value; }

        public enum PlayerEnum
        {
            Player1, Player2
        }

        public GameBoard()
        {
            board = new char[9,9];
            gameOver = false;
        }

        public bool MovePiece(PlayerEnum player, Coordinate coordinate)
        {
            bool bReturn = false;
            

            return bReturn;
        }

    }
}
