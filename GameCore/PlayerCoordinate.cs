using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class PlayerCoordinate
    {
        public int Row { get; set; }
        public int Col { get; set; }

        public PlayerCoordinate(string str)
        {
            if (str.Length != 2)
            {
                throw new Exception("Invalid coordinate format");
            }
            Row = BoardUtil.GetInteralPlayerRow(str[1]);
            Col = BoardUtil.GetInternalPlayerCol(str[0]);
        }

        public PlayerCoordinate(int row, int col)
        {
            Row = row;
            Col = col;
        }

    }
}

