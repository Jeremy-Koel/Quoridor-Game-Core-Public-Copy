using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class PlayerCoordinate
    {
        private static Dictionary<char, int> rowTranslationMap;
        private static Dictionary<char, int> colTranslationMap;

        static PlayerCoordinate()
        {
            rowTranslationMap = new Dictionary<char, int>();
            rowTranslationMap.Add('1', 16);
            rowTranslationMap.Add('2', 14);
            rowTranslationMap.Add('3', 12);
            rowTranslationMap.Add('4', 10);
            rowTranslationMap.Add('5', 8);
            rowTranslationMap.Add('6', 6);
            rowTranslationMap.Add('7', 4);
            rowTranslationMap.Add('8', 2);
            rowTranslationMap.Add('9', 0);

            colTranslationMap = new Dictionary<char, int>();
            colTranslationMap.Add('a', 0);
            colTranslationMap.Add('b', 2);
            colTranslationMap.Add('c', 4);
            colTranslationMap.Add('d', 6);
            colTranslationMap.Add('e', 8);
            colTranslationMap.Add('f', 10);
            colTranslationMap.Add('g', 12);
            colTranslationMap.Add('h', 14);
            colTranslationMap.Add('i', 16);
        }

        private int row;
        private int col;

        public PlayerCoordinate(string str)
        {
            if (str.Length != 2)
            {
                throw new Exception("Invalid coordinate format");
            }
            row = rowTranslationMap[str[1]];
            col = colTranslationMap[str[0]];
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

