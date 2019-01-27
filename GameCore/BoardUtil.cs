using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class BoardUtil
    {
        private static Dictionary<char, int> playerRowTranslationMap;
        private static Dictionary<char, int> playerColTranslationMap;

        static BoardUtil()
        {
            playerRowTranslationMap = new Dictionary<char, int>();
            playerRowTranslationMap.Add('1', 16);
            playerRowTranslationMap.Add('2', 14);
            playerRowTranslationMap.Add('3', 12);
            playerRowTranslationMap.Add('4', 10);
            playerRowTranslationMap.Add('5', 8);
            playerRowTranslationMap.Add('6', 6);
            playerRowTranslationMap.Add('7', 4);
            playerRowTranslationMap.Add('8', 2);
            playerRowTranslationMap.Add('9', 0);

            playerColTranslationMap = new Dictionary<char, int>();
            playerColTranslationMap.Add('a', 0);
            playerColTranslationMap.Add('b', 2);
            playerColTranslationMap.Add('c', 4);
            playerColTranslationMap.Add('d', 6);
            playerColTranslationMap.Add('e', 8);
            playerColTranslationMap.Add('f', 10);
            playerColTranslationMap.Add('g', 12);
            playerColTranslationMap.Add('h', 14);
            playerColTranslationMap.Add('i', 16);
        }

        public static int GetInteralPlayerRow(char c)
        {
            return playerRowTranslationMap[c];
        }

        public static int GetInternalPlayerCol(char c)
        {
            return playerColTranslationMap[c];
        }
        
    }
}
