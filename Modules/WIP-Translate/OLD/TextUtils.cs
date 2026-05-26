using System.Collections.Generic;
using UnityEngine;

public static class TextUtils
{
    public static List<string> ChunkText(string input, int maxLength)
    {
        List<string> chunks = new();
        if (string.IsNullOrWhiteSpace(input) || maxLength <= 0)
            return chunks;

        int index = 0;
        while (index < input.Length)
        {
            // Остаток строки
            int remaining = input.Length - index;
            int length = Mathf.Min(maxLength, remaining);
            int splitPos = length;

            // Если не конец строки — ищем последний пробел перед splitPos
            if (index + length < input.Length)
            {
                int lastSpace = input.LastIndexOf(' ', index + length, length);
                if (lastSpace > index)
                    splitPos = lastSpace - index;
            }

            chunks.Add(input.Substring(index, splitPos).Trim());
            index += splitPos;

            // Пропускаем пробелы между словами
            while (index < input.Length && char.IsWhiteSpace(input[index]))
                index++;
        }

        return chunks;
    }
}
