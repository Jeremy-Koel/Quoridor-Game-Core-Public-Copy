using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class WallCoordinate
    {
        public enum WallOrientation
        {
            Vertical, Horizontal
        }

        private int startRow;
        private int startCol;
        private int endRow;
        private int endCol;
        private WallOrientation orientation;
        
        internal WallOrientation Orientation { get => orientation; set => orientation = value; }
        public int StartRow { get => startRow; set => startRow = value; }
        public int StartCol { get => startCol; set => startCol = value; }
        public int EndRow { get => endRow; set => endRow = value; }
        public int EndCol { get => endCol; set => endCol = value; }

        public WallCoordinate(string str)
        {
            if (str.Length != 3 || !(str[2] == 'v' || str[2] == 'h'))
            {
                throw new Exception("Invalid input format");
            }

            if (str[2] == 'v')
            {
                orientation = WallOrientation.Vertical;
            }
            else
            {
                orientation = WallOrientation.Horizontal;
            }
            
            PlayerCoordinate referenceCoordinate = new PlayerCoordinate(str.Substring(0, 2));

            if (orientation == WallOrientation.Vertical)
            {
                startRow = referenceCoordinate.Row;
                startCol = referenceCoordinate.Col + 1;
                endRow = startRow - 1;
                endCol = startCol;
            }
            else
            {
                startRow = referenceCoordinate.Row - 1;
                startCol = referenceCoordinate.Col;
                endRow = startRow;
                endCol = startCol + 1;
            }

        }
        

    }
}
