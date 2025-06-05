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
            Dictionary<string, int> actualCounts = new Dictionary<string, int>();
            for (int i = 0; i < generatedText.Length - 1; i++)
            {
                string bigram = $"{generatedText[i]}{generatedText[i + 1]}";
                if (!actualCounts.ContainsKey(bigram))
                    actualCounts[bigram] = 0;
                actualCounts[bigram]++;
            }
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

    public class WordGenerator
    {
        public string Word { get; set; }
        public int Frequency { get; set; }
        public double Probability { get; set; }


        public List<WordGenerator> LoadWordsFreq(string filePath)
        {
            var list = new List<WordGenerator>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не найден.");
                return list;
            }

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6)
                    continue;

                string word = parts[1];
                if (!double.TryParse(parts[4], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double freqDouble))
                    continue;

                int freq = (int)Math.Round(freqDouble);

                list.Add(new WordGenerator { Word = word, Frequency = freq });
            }
            return list;
        }

        public string GenerateText(List<WordGenerator> wordFreqs, int wordCount)
        {
            var rnd = new Random();

            var cumulativeProbs = new double[wordFreqs.Count];
            cumulativeProbs[0] = wordFreqs[0].Probability;
            for (int i = 1; i < wordFreqs.Count; i++)
            {
                cumulativeProbs[i] = cumulativeProbs[i - 1] + wordFreqs[i].Probability;
            }

            var words = new List<string>();

            for (int i = 0; i < wordCount; i++)
            {
                double r = rnd.NextDouble();
                int index = Array.BinarySearch(cumulativeProbs, r);
                if (index < 0)
                    index = ~index;

                if (index >= wordFreqs.Count)
                    index = wordFreqs.Count - 1;

                words.Add(wordFreqs[index].Word);
            }

            return string.Join(" ", words);
        }

        public void SaveWordComparison(string generatedText, List<WordGenerator> wordFreqs, string outputPath)
        {
            var generatedWords = generatedText.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int totalGenerated = generatedWords.Length;

            var actualCounts = generatedWords
                .GroupBy(word => word)
                .ToDictionary(g => g.Key, g => g.Count());

            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("word,expected,actual");

                foreach (var wf in wordFreqs)
                {
                    double expectedProb = wf.Probability;
                    double actualProb = actualCounts.TryGetValue(wf.Word, out int count)
                        ? (double)count / totalGenerated
                        : 0.0;

                    writer.WriteLine($"{wf.Word},{expectedProb.ToString(CultureInfo.InvariantCulture)},{actualProb.ToString(CultureInfo.InvariantCulture)}");
                }
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
            var bigramText = bigramGen.GenerateText(1000);
            Console.WriteLine(bigramText);


            string resultsPathBigrams = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
            Directory.CreateDirectory(resultsPathBigrams);

            File.WriteAllText(Path.Combine(resultsPathBigrams, "gen-1.txt"), bigramText);

            bigramGen.SaveBigramComparison(bigramText, "bigram_comparison.csv");

            Console.WriteLine();

            string pathWords = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res/words_freq.txt");

            var textGenerator = new WordGenerator();



            var wordFreqs = textGenerator.LoadWordsFreq(pathWords);

            if (wordFreqs.Count == 0)
            {
                Console.WriteLine("Список слов пуст.");
                return;
            }

            int totalFreq = wordFreqs.Sum(wf => wf.Frequency);
            foreach (var wf in wordFreqs)
            {
                wf.Probability = (double)wf.Frequency / totalFreq;
            }

            string generatedText = textGenerator.GenerateText(wordFreqs, 1000);
            Console.WriteLine("Сгенерированный текст:");
            Console.WriteLine(generatedText);

            string resultsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
            Directory.CreateDirectory(resultsPath);

            File.WriteAllText(Path.Combine(resultsPath, "gen-2.txt"), generatedText);

            textGenerator.SaveWordComparison(generatedText, wordFreqs, Path.Combine(resultsPath, "word_distribution.csv"));

        }
    }
}

