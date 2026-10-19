using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ShowAsciiArt();

        while (true)
        {
            Console.WriteLine("\n[1] Выбрать папку и создать структуру из текста");
            Console.WriteLine("[2] Выход");
            Console.Write("\nВыберите действие (1-2): ");

            var key = Console.ReadKey(true);
            Console.WriteLine();

            if (key.KeyChar == '1')
            {
                CreateStructureWorkflow();
            }
            else if (key.KeyChar == '2')
            {
                break;
            }
        }
    }

    static void ShowAsciiArt()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
    ____  _      ______                __             
   / __ \(_)____/ ____/________ ___  __/ /_  u_  _____
  / / / / / ___/ /   / ___/ __ `/ / / / __ \/ / / / _ \
 / /_/ / / /  / /___/ /  / /_/ / /_/ / /_/ / /_/ /  __/
/_____/_/_/   \____/_/   \__,_/\__,_/_.___/\__, /\___/ 
                                          /____/       
        ");
        Console.ResetColor();
        Console.WriteLine(" добро пожаловать в генератор структуры проектов!\n");
    }

    static void CreateStructureWorkflow()
    {
        string? targetDir = SelectFolderDialog();

        if (string.IsNullOrEmpty(targetDir))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[-] Папка не выбрана.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[+] Выбрана папка: {targetDir}\n");
        Console.ResetColor();

        Console.WriteLine("Вставьте структуру проекта (для завершения ввода нажмите Enter, затем Ctrl+Z и Enter):");
        Console.WriteLine("--------------------------------------------------------------------------------");

        StringBuilder sb = new StringBuilder();
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            sb.AppendLine(line);
        }

        string rawStructure = sb.ToString();
        if (string.IsNullOrWhiteSpace(rawStructure))
        {
            Console.WriteLine("[-] Структура пуста.");
            return;
        }

        try
        {
            ParseAndCreateStructure(targetDir, rawStructure);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[✔] Структура успешно создана!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[✖] Ошибка при создании структуры: {ex.Message}");
            Console.ResetColor();
        }
    }

    static string? SelectFolderDialog()
    {
        using (var folderDialog = new FolderBrowserDialog())
        {
            folderDialog.Description = "Выберите корневую папку для проекта";
            DialogResult result = folderDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                return folderDialog.SelectedPath;
            }
        }
        return null;
    }

    static void ParseAndCreateStructure(string rootPath, string rawData)
    {
        // Разбиваем на строки и очищаем от нулевых символов \0 и несуществующих Unicode кракозябр
        string[] lines = rawData
            .Replace("\0", "")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var pathStack = new Stack<(int indent, string path)>();
        pathStack.Push((-1, rootPath));

        foreach (var rawLine in lines)
        {
            // Фильтрация непечатаемых символов и нулевых байт в строке
            string cleanedLine = new string(rawLine.Where(c => c != '\0').ToArray()).TrimEnd();
            if (string.IsNullOrWhiteSpace(cleanedLine)) continue;

            // Вычисляем глубину (индекс первого полезного символа названия файла/папки)
            int firstCharIndex = GetFirstValidCharIndex(cleanedLine);
            if (firstCharIndex == -1) continue;

            string itemName = CleanItemName(cleanedLine);
            if (string.IsNullOrEmpty(itemName)) continue;

            // Если это имя корневой папки (заканчивается на / и идет в самом начале), пропускаем
            if (firstCharIndex == 0 && itemName.EndsWith("/"))
            {
                continue;
            }

            // Корректируем стек в зависимости от отступа
            while (pathStack.Count > 1 && pathStack.Peek().indent >= firstCharIndex)
            {
                pathStack.Pop();
            }

            string parentPath = pathStack.Peek().path;
            bool isDirectory = itemName.EndsWith("/") || !itemName.Contains('.');

            string cleanPathName = itemName.TrimEnd('/', '\\');

            // Фильтруем запрещенные в Windows символы для путей
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                cleanPathName = cleanPathName.Replace(invalidChar.ToString(), "");
            }

            if (string.IsNullOrWhiteSpace(cleanPathName)) continue;

            string fullPath = Path.Combine(parentPath, cleanPathName);

            if (isDirectory)
            {
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Console.WriteLine($"[Папка]  {fullPath}");
                }
                pathStack.Push((firstCharIndex, fullPath));
            }
            else
            {
                string? parentDir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                if (!File.Exists(fullPath))
                {
                    File.WriteAllText(fullPath, string.Empty);
                    Console.WriteLine($"[Файл]   {fullPath}");
                }
            }
        }
    }

    static int GetFirstValidCharIndex(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            // Игнорируем спецсимволы дерева и любые пробелы
            if (c != '├' && c != '└' && c != '─' && c != '│' && c != ' ' && c != '\t' && c != '\u00A0')
            {
                return i;
            }
        }
        return -1;
    }

    static string CleanItemName(string line)
    {
        char[] treeChars = { '├', '└', '─', '│', ' ', '\t', '\u00A0' };
        string cleaned = line.TrimStart(treeChars).Trim();

        // Удаляем все нулевые байты
        cleaned = cleaned.Replace("\0", "");
        return cleaned;
    }
}