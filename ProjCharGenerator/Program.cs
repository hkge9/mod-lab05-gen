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

                if (!double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double rank))
                    continue;

             


                weight = 1.0 / (rank + 1);

                if (!bigramTable.ContainsKey(first))
                    bigramTable[first] = new List<(char, double)>();

                bigramTable[first].Add((second, weight));

            }
        }
        public void SaveBigramComparison(string generatedText, string outputPath)
        {
            // Построим словарь: биграмма -> сколько раз она встретилась
            Dictionary<string, int> actualCounts = new Dictionary<string, int>();
            for (int i = 0; i < generatedText.Length - 1; i++)
            {
                string bigram = $"{generatedText[i]}{generatedText[i + 1]}";
                if (!actualCounts.ContainsKey(bigram))
                    actualCounts[bigram] = 0;
                actualCounts[bigram]++;
            }

            // Теперь создаём словарь: биграмма -> ожидаемый ранг
            Dictionary<string, int> expectedRanks = new Dictionary<string, int>();
            foreach (var line in File.ReadLines("res/bigrams.txt"))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4 || parts[1].Length != 2)
                    continue;

                string bigram = parts[1];
                if (int.TryParse(parts[3], out int rank))
                {
                    expectedRanks[bigram] = rank;
                }
            }

            // Сохраняем в CSV: bigram,rank,actual_count
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("bigram,expected_rank,actual_count");
                foreach (var kvp in actualCounts)
                {
                    string bigram = kvp.Key;
                    int actual = kvp.Value;
                    int rank = expectedRanks.ContainsKey(bigram) ? expectedRanks[bigram] : -1; // -1 если нет в таблице

                    writer.WriteLine($"{bigram},{rank},{actual}");
                }
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

            bigramGen.SaveBigramComparison(bigramText, "bigram_comparison.csv");


        }
    }
}

