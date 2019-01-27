﻿using System;
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
            row = BoardUtil.GetInteralPlayerRow(str[1]);
            col = BoardUtil.GetInternalPlayerCol(str[0]);
        }

        public PlayerCoordinate(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public int Row { get => row; set => row = value; }
        public int Col { get => col; set => col = value; }
    }

    // Jacob 
    /*public KeyValuePair<int, int> coordinateToNotation = new KeyValuePair<int, int>();

    void fixCoordinateToNotation()
    {
        coordinateToNotation(12, 1);
        coordinateToNotation(10, 2);
        coordinateToNotation(8, 3);
        coordinateToNotation(6, 4);
        coordinateToNotation(4, 5);
        coordinateToNotation(2, 6);
        coordinateToNotation(0, 7);

    }*/
}

