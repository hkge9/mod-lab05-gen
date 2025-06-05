import matplotlib.pyplot as plt
import csv

# Путь к CSV файлу


words = []
expected = []
actual = []

# Чтение данных
with open("word_distribution.csv", encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        word = row["word"]
        exp = float(row["expected"])
        act = float(row["actual"])
        
        words.append(word)
        expected.append(exp)
        actual.append(act)

# Выбрать топ-30 по ожидаемой частоте
top_n = 30
sorted_indices = sorted(range(len(expected)), key=lambda i: expected[i], reverse=True)[:top_n]

words_top = [words[i] for i in sorted_indices]
expected_top = [expected[i] for i in sorted_indices]
actual_top = [actual[i] for i in sorted_indices]

# Построение графика
x = range(len(words_top))
width = 0.4

plt.figure(figsize=(14, 6))
plt.bar([i - width/2 for i in x], expected_top, width=width, label='Ожидаемая частота')
plt.bar([i + width/2 for i in x], actual_top, width=width, label='Фактическая частота')
plt.xticks(x, words_top, rotation=45, ha='right')
plt.xlabel("Слова")
plt.ylabel("Частота (доля)")
plt.title("Сравнение ожидаемых и фактических частот слов")
plt.legend()
plt.tight_layout()
plt.grid(axis='y', linestyle='--', alpha=0.5)
plt.show()
