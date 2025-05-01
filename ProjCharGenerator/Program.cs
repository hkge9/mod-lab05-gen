using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace generator
{
    class CharGenerator 
    {
        private string syms = "абвгдеёжзийклмнопрстуфхцчшщьыъэюя"; 
        private char[] data;
        private int size;
        private Random random = new Random();
        public CharGenerator() 
        {
           size = syms.Length;
           data = syms.ToCharArray(); 
        }
        public char getSym() 
        {
           return data[random.Next(0, size)]; 
        }
    }

    public class BigramTextGenerator
    {
        private Dictionary<char, List<(char nextChar, double weight)>> bigramTable;
        private Random random;

        public BigramTextGenerator()
        {
            bigramTable = new Dictionary<char, List<(char, double)>>();
            random = new Random();
        }
        public void LoadBigramData(string path)
        {
            
            foreach (var line in File.ReadLines(path))
            {
                var parts = line.Split(',');
                if (parts.Length != 3) continue;

                char first = parts[0][0];
                char second = parts[1][0];
                double weight = double.Parse(parts[2]);

                if (!bigramTable.ContainsKey(first))
                    bigramTable[first] = new List<(char, double)>();

                bigramTable[first].Add((second, weight));
            }
        }

    }
    class Program
    {
        static void Main(string[] args)
        {
            var bigramGen = new BigramTextGenerator();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res/bigrams.txt");

            bigramGen.LoadBigramData(path);

        }
    }
}

