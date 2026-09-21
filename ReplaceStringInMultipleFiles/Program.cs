/// <summary>
/// FileReplacer.exe "*.txt" "Hello" "Привет"
/// Или с указанием директории:
/// FileReplacer.exe "*.cs" "OldName" "NewName" "C:\Projects\MyProject"
/// </summary>
using System;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3 || args.Length > 4)
        {
            Console.WriteLine("Использование:");
            Console.WriteLine("  FileReplacer.exe <маска> <что_искать> <на_что_заменить> [директория]");
            Console.WriteLine();
            Console.WriteLine("Пример:");
            Console.WriteLine("  FileReplacer.exe \"*.txt\" \"старый текст\" \"новый текст\" \"C:\\Files\"");
            return;
        }

        string fileMask = args[0];
        string searchText = args[1];
        string replaceText = args[2];

        string directory = args.Length == 4
            ? args[3]
            : Directory.GetCurrentDirectory();

        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Директория не найдена: {directory}");
            return;
        }

        Console.WriteLine($"Маска:        {fileMask}");
        Console.WriteLine($"Искать:       {searchText}");
        Console.WriteLine($"Заменить на:  {replaceText}");
        Console.WriteLine($"Директория:   {directory}");
        Console.WriteLine();

        int processed = 0;
        int changedFiles = 0;
        int totalReplacements = 0;
        int errors = 0;

        try
        {
            foreach (string file in Directory.EnumerateFiles(
                         directory,
                         fileMask,
                         SearchOption.AllDirectories))
            {
                processed++;

                try
                {
                    int replacements = ReplaceInFile(
                        file,
                        searchText,
                        replaceText);

                    if (replacements > 0)
                    {
                        changedFiles++;
                        totalReplacements += replacements;

                        Console.WriteLine(
                            $"Изменён: {file} | Замен: {replacements}");
                    }
                } catch (Exception ex)
                {
                    errors++;
                    Console.WriteLine($"Ошибка: {file}");
                    Console.WriteLine($"  {ex.Message}");
                }
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Ошибка поиска файлов: {ex.Message}");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Готово.");
        Console.WriteLine($"Обработано файлов: {processed}");
        Console.WriteLine($"Изменено файлов:   {changedFiles}");
        Console.WriteLine($"Всего замен:       {totalReplacements}");
        Console.WriteLine($"Ошибок:            {errors}");
    }

    private static int ReplaceInFile(
        string filePath,
        string searchText,
        string replaceText)
    {
        byte[] bytes = File.ReadAllBytes(filePath);

        if (bytes.Length == 0)
            return 0;

        // UTF-16 LE BOM = FF FE
        bool hasBom =
            bytes.Length >= 2 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xFE;

        byte[] contentBytes;

        if (hasBom)
        {
            contentBytes = new byte[bytes.Length - 2];

            Buffer.BlockCopy(
                bytes,
                2,
                contentBytes,
                0,
                contentBytes.Length);
        } else
        {
            contentBytes = bytes;
        }

        Encoding encoding = new UnicodeEncoding(
            bigEndian: false,
            byteOrderMark: false,
            throwOnInvalidBytes: true);

        string content;

        try
        {
            content = encoding.GetString(contentBytes);
        } catch (DecoderFallbackException)
        {
            Console.WriteLine(
                $"Пропущен файл (не UTF-16 LE): {filePath}");

            return 0;
        }

        if (string.IsNullOrEmpty(searchText))
            return 0;

        // Считаем количество вхождений.
        int replacements = CountOccurrences(
            content,
            searchText);

        if (replacements == 0)
            return 0;

        string newContent = content.Replace(
            searchText,
            replaceText,
            StringComparison.Ordinal);

        byte[] newContentBytes = encoding.GetBytes(newContent);

        byte[] result;

        if (hasBom)
        {
            result = new byte[newContentBytes.Length + 2];

            result[0] = 0xFF;
            result[1] = 0xFE;

            Buffer.BlockCopy(
                newContentBytes,
                0,
                result,
                2,
                newContentBytes.Length);
        } else
        {
            result = newContentBytes;
        }

        File.WriteAllBytes(filePath, result);

        return replacements;
    }

    private static int CountOccurrences(
        string text,
        string searchText)
    {
        int count = 0;
        int position = 0;

        while ((position = text.IndexOf(
                   searchText,
                   position,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            position += searchText.Length;
        }

        return count;
    }
}

