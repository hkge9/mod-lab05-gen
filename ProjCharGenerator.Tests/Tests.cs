using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using generator;

namespace AlgorithmTests
{
    [TestClass]
    public class Tests
    {
        private BigramTextGenerator PrepareBigramGen(Dictionary<char, List<(char, double)>> data)
        {
            var gen = new BigramTextGenerator();
            var field = typeof(BigramTextGenerator)
                .GetField("bigramTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(gen, data);
            return gen;
        }

        [TestMethod]
        public void GetNextChar_ReturnsExpectedNextChar_BasedOnWeights()
        {
            var data = new Dictionary<char, List<(char, double)>>()
            {
                ['а'] = new List<(char, double)> { ('б', 0.0), ('в', 1.0) }
            };

            var gen = PrepareBigramGen(data);

            // Since вес у 'б' 0, а у 'в' 1, всегда вернется 'в'
            string result = "";
            for (int i = 0; i < 10; i++)
            {
                var next = gen.GenerateText(2); // берем первый символ и получаем следующий
                result += next[1];
            }

            Assert.IsTrue(result.All(c => c == 'в'), "Следующий символ должен всегда быть 'в'");
        }

        [TestMethod]
        public void GenerateText_ReturnsTextOfCorrectLength()
        {
            var data = new Dictionary<char, List<(char, double)>>()
            {
                ['а'] = new List<(char, double)> { ('б', 1.0) },
                ['б'] = new List<(char, double)> { ('а', 1.0) }
            };
            var gen = PrepareBigramGen(data);

            int len = 100;
            var text = gen.GenerateText(len);
            Assert.AreEqual(len, text.Length);
        }

        [TestMethod]
        public void GenerateText_OnlyUsesKnownChars()
        {
            var data = new Dictionary<char, List<(char, double)>>()
            {
                ['x'] = new List<(char, double)> { ('y', 1.0) },
                ['y'] = new List<(char, double)> { ('z', 1.0) },
                ['z'] = new List<(char, double)> { ('x', 1.0) }
            };
            var gen = PrepareBigramGen(data);

            var text = gen.GenerateText(50);
            var allowed = new HashSet<char> { 'x', 'y', 'z' };

            foreach (var c in text)
                Assert.IsTrue(allowed.Contains(c), $"Недопустимый символ {c}");
        }

        [TestMethod]
        public void GenerateText_ThrowsIfNoData()
        {
            var gen = new BigramTextGenerator();

            Assert.ThrowsException<InvalidOperationException>(() => gen.GenerateText(10));
        }

        [TestMethod]
        public void GenerateText_ReturnsSingleCharIfLengthOne()
        {
            var data = new Dictionary<char, List<(char, double)>>()
            {
                ['а'] = new List<(char, double)> { ('б', 1.0) }
            };
            var gen = PrepareBigramGen(data);

            var text = gen.GenerateText(1);
            Assert.AreEqual(1, text.Length);
            Assert.IsTrue(data.ContainsKey(text[0]));
        }


    }

    [TestClass]
    public class WordGeneratorAlgorithmTests
    {
        [TestMethod]
        public void GenerateText_GeneratesCorrectNumberOfWords()
        {
            var wg = new WordGenerator();

            var words = new List<WordGenerator>()
            {
                new WordGenerator { Word = "apple", Frequency = 50, Probability = 0.5 },
                new WordGenerator { Word = "banana", Frequency = 50, Probability = 0.5 }
            };

            string text = wg.GenerateText(words, 100);
            var splitted = text.Split(' ');
            Assert.AreEqual(100, splitted.Length);
            foreach (var word in splitted)
            {
                Assert.IsTrue(words.Any(w => w.Word == word), $"Слово {word} отсутствует в списке");
            }
        }

        [TestMethod]
        public void GenerateText_ProbabilitiesSumToOne()
        {
            var words = new List<WordGenerator>()
            {
                new WordGenerator { Word = "apple", Frequency = 1, Probability = 0.6 },
                new WordGenerator { Word = "banana", Frequency = 1, Probability = 0.4 }
            };

            double sum = words.Sum(w => w.Probability);
            Assert.IsTrue(Math.Abs(sum - 1.0) < 1e-6, $"Сумма вероятностей должна быть 1, а не {sum}");
        }

        [TestMethod]
        public void GenerateText_HandlesSingleWord()
        {
            var wg = new WordGenerator();

            var words = new List<WordGenerator>()
            {
                new WordGenerator { Word = "onlyword", Frequency = 1, Probability = 1.0 }
            };

            string text = wg.GenerateText(words, 10);
            var splitted = text.Split(' ');
            Assert.AreEqual(10, splitted.Length);
            Assert.IsTrue(splitted.All(w => w == "onlyword"));
        }


        [TestMethod]
        public void GenerateText_CorrectlySelectsWordsByProbability()
        {
            var wg = new WordGenerator();

            var words = new List<WordGenerator>()
            {
                new WordGenerator { Word = "a", Frequency = 10, Probability = 0.1 },
                new WordGenerator { Word = "b", Frequency = 90, Probability = 0.9 }
            };

            string text = wg.GenerateText(words, 1000);
            var splitted = text.Split(' ');

            int countA = splitted.Count(w => w == "a");
            int countB = splitted.Count(w => w == "b");

            // Ожидаем, что 'b' встречается примерно в 9 раз чаще 'a'
            Assert.IsTrue(countB > countA * 5, $"b должно быть встречаться чаще, чем a, но a={countA}, b={countB}");
        }

        [TestMethod]
        public void GetNextChar_ReturnsNextCharFromOptions()
        {
            // Подготавливаем данные: для символа 'а' есть два варианта следующего символа с весами
            var data = new Dictionary<char, List<(char, double)>>()
            {
                ['а'] = new List<(char, double)>
        {
            ('б', 0.7),
            ('в', 0.3)
        }
            };

            var gen = PrepareBigramGen(data);

            var method = typeof(BigramTextGenerator).GetMethod("GetNextChar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Вызываем метод несколько раз, чтобы убедиться, что возвращаемый символ — один из ожидаемых
            var allowed = new HashSet<char> { 'б', 'в' };
            for (int i = 0; i < 10; i++)
            {
                var result = (char)method.Invoke(gen, new object[] { 'а' });
                Assert.IsTrue(allowed.Contains(result), $"Получен неожиданный символ: {result}");
            }
        }

        private BigramTextGenerator PrepareBigramGen(Dictionary<char, List<(char, double)>> data)
        {
            var gen = new BigramTextGenerator();

            // Через рефлексию задаём приватное поле bigramTable
            var field = typeof(BigramTextGenerator).GetField("bigramTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(gen, data);

            return gen;
        }


    }
}
