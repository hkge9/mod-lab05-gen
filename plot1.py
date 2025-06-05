import csv
import matplotlib.pyplot as plt
import numpy as np

bigrams = []
actual_counts = []
expected_values = []

with open("bigram_comparison.csv", encoding="utf-8") as f:
    reader = csv.DictReader(f)
    for row in reader:
        try:
            rank = int(row["expected_rank"])
            count = int(row["actual_count"])
            bigram = row["bigram"]
            if rank == -1:
                continue
            bigrams.append(bigram)
            actual_counts.append(count)
            expected_values.append(1 / (rank + 1))
        except:
            continue

# Нормализация
total_actual = sum(actual_counts)
relative_actual = [count / total_actual for count in actual_counts]

total_expected = sum(expected_values)
relative_expected = [v / total_expected for v in expected_values]

# Топ N биграмм для читаемости
N = 20
bigrams = bigrams[:N]
relative_actual = relative_actual[:N]
relative_expected = relative_expected[:N]

ind = np.arange(N)  # позиции по X
width = 0.35        # ширина столбцов

plt.figure(figsize=(14, 8))

# Вертикальные столбцы
plt.bar(ind - width/2, relative_expected, width, label='Ожидаемая частота', color='red', alpha=0.7)
plt.bar(ind + width/2, relative_actual, width, label='Фактическая частота', color='blue', alpha=0.7)

plt.xticks(ind, bigrams, fontsize=12, rotation=45, ha='right')
plt.ylabel('Относительная частота')
plt.title('Сравнение ожидаемых и фактических частот биграмм')
plt.legend()
plt.grid(axis='y')
plt.tight_layout()
plt.show()
