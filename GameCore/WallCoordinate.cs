using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    public class WallCoordinate
    {
        public enum WallOrientation
        {
            Vertical, Horizontal
        }

        internal WallOrientation Orientation { get; set; }
        public int StartRow { get; set; }
        public int StartCol { get; set; }
        public int EndRow { get; set; }
        public int EndCol { get; set; }

        public WallCoordinate(string str)
        {
            if (str.Length != 3 || !(str[2] == 'v' || str[2] == 'h'))
            {
                throw new Exception("Invalid input format");
            }

            if (str[2] == 'v')
            {
                Orientation = WallOrientation.Vertical;
            }
            else
            {
                Orientation = WallOrientation.Horizontal;
            }
            
            PlayerCoordinate referenceCoordinate = new PlayerCoordinate(str.Substring(0, 2));

            if (Orientation == WallOrientation.Vertical)
            {
                StartRow = referenceCoordinate.Row;
                StartCol = referenceCoordinate.Col + 1;
                EndRow = StartRow - 2;
                EndCol = StartCol;
            }
            else
            {
                StartRow = referenceCoordinate.Row - 1;
                StartCol = referenceCoordinate.Col;
                EndRow = StartRow;
                EndCol = StartCol + 2;
            }
        }

        public WallCoordinate(int x, int y, char c)
        {
            char orientation = char.ToLower(c);
            if (x < 0 || x >= GameBoard.TOTAL_ROWS || y < 0 || y >= GameBoard.TOTAL_COLS || !(orientation == 'h' || orientation == 'v'))
            {
                throw new Exception("Invalid input format");
            }

            if (orientation == 'h')
            {
                Orientation = WallOrientation.Horizontal;
            }
            else if (orientation == 'v')
            {
                Orientation = WallOrientation.Vertical;
            }

            PlayerCoordinate referenceCoordinate = new PlayerCoordinate(x, y);

            if (Orientation == WallOrientation.Vertical)
            {
                StartRow = referenceCoordinate.Row;
                StartCol = referenceCoordinate.Col + 1;
                EndRow = StartRow - 2;
                EndCol = StartCol;
            }
            else
            {
                StartRow = referenceCoordinate.Row - 1;
                StartCol = referenceCoordinate.Col;
                EndRow = StartRow;
                EndCol = StartCol + 2;
            }
        }
    }
}
