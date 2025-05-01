using System;
using System.Collections.Generic;
using System.Globalization;
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
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2 || parts[1].Length != 2)
                    continue;

                char first = parts[1][0];
                char second = parts[1][1];
                if (!double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double weight))
                    continue;

                if (!bigramTable.ContainsKey(first))
                    bigramTable[first] = new List<(char, double)>();

                bigramTable[first].Add((second, weight));
                Console.WriteLine($"Загружено {bigramTable.Count} стартовых символов.");

            }
        }
        private char GetNextChar(char current)
        {
            if (!bigramTable.ContainsKey(current))
                return ' '; 

            var options = bigramTable[current];
            double totalWeight = options.Sum(x => x.weight);
            double roll = random.NextDouble() * totalWeight;

            foreach (var (nextChar, weight) in options)
            {
                roll -= weight;
                if (roll <= 0)
                    return nextChar;
            }

            return options.Last().nextChar; 
        }

        public string GenerateText(int length)
        {
            if (bigramTable.Count == 0)
                throw new InvalidOperationException("Bigram data not loaded.");

            char current = bigramTable.Keys.First(); 
            var result = new List<char> { current };

            for (int i = 1; i < length; i++)
            {
                current = GetNextChar(current);
                result.Add(current);
            }

            return new string(result.ToArray());
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var bigramGen = new BigramTextGenerator();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res/bigrams.txt");

            bigramGen.LoadBigramData(path);
            var bigramText = bigramGen.GenerateText(1000);
            Console.WriteLine(bigramText);
            File.WriteAllText("output_bigram.txt", bigramText);
        }
    }
}

