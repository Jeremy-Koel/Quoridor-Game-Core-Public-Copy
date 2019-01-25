using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class PlayerCoordinate
    {
        private int row;
        private int col;

        public PlayerCoordinate(string str)
        {
            if (str.Length != 2)
            {
                throw new Exception("Invalid coordinate format");
            }
            row = Int32.Parse(str[1].ToString()) - 1; // incoming notation is one-indexed 
            char c = Char.ToLower(str[0]);
            col = (c - 'a'); 
        }

        public PlayerCoordinate(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public int Row { get => row; set => row = value; }
        public int Col { get => col; set => col = value; }
    }
}
