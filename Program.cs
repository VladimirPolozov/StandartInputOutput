using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
/* 
 * Использование BinaryFormatter в коде не рекомендуется.
 * Вместо этого предлагается использование JsonSerializer или XmlSerializer.
 * Источник: https://learn.microsoft.com/ru-ru/dotnet/core/compatibility/serialization/5.0/binaryformatter-serialization-obsolete
 */

namespace StandartInputOutput {
  public class Memento {
    public string TextContent { get; private set; }

    public Memento(string textContent) {
      this.TextContent = textContent;
    }
  }

  public class TextFile {
    private string OriginalTextContent { get; set; }
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string FileExtension { get; set; }
    public string TextContent { get; set; }

    public TextFile(string filePath) {
      this.FilePath = filePath;
      this.FileName = Path.GetFileName(filePath);
      this.FileExtension = Path.GetExtension(filePath);
      this.TextContent = this.OriginalTextContent = File.ReadAllText(filePath);
    }

    // Update text content of the file
    public void UpdateText(string newText) {
      this.TextContent = newText;
      File.WriteAllText(this.FilePath, newText);
    }

    // Get text content of the file
    public string GetText() {
      return this.TextContent;
    }

    public Memento Save() {
      return new Memento(this.TextContent);
    }

    public void Restore(Memento memento) {
      this.TextContent = memento.TextContent;
      File.WriteAllText(this.FilePath, this.TextContent);
    }

    public void Undo() {
      this.Restore(new Memento(this.OriginalTextContent));
    }

    // Serialize the TextFile object to XML
    public void SerializeToXml(string outputPath) {
      XmlSerializer serializer = new XmlSerializer(typeof(TextFile));
      using (StreamWriter writer = new StreamWriter(outputPath)) {
        serializer.Serialize(writer, this);
      }
    }

    // Serialize the TextFile object to JSON
    public void SerializeToJson(string outputPath) {
      string jsonText = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
      File.WriteAllText(outputPath, jsonText);
    }

    // Deserialize the TextFile object from XML
    public static TextFile DeserializeFromXml(string filePath) {
      using (StreamReader reader = new StreamReader(filePath)) {
        XmlSerializer serializer = new XmlSerializer(typeof(TextFile));
        return (TextFile)serializer.Deserialize(reader);
      }
    }

    // Deserialize the TextFile object from JSON
    public static TextFile DeserializeFromJson(string filePath) {
      string jsonText = File.ReadAllText(filePath);
      return JsonConvert.DeserializeObject<TextFile>(jsonText);
    }
  }

  public class SeachTextFilesByKeywords {
    private static string[] keywords;
    public static string[] Keywords { get; set; }
    private static string currentDirectoryPath = Directory.GetCurrentDirectory();
    public static string CurrentDirectoryPath {
      get {
        return currentDirectoryPath;
      }
      set {
        if (Directory.Exists(value)) {
          currentDirectoryPath = value;
        } else {
          Console.WriteLine("Данная директория не найдена");
        }
      }
    }

    public static List<string> Seach() {
      List<string> allFoundFiles = new List<string>();

      for (int keywordIndex = 0; keywordIndex < Keywords.Length; ++keywordIndex) {
        string[] foundFiles = Directory.GetFiles(CurrentDirectoryPath + "\\", $"*{Keywords[keywordIndex]}*");

        for (int foundFileIndex = 0; foundFileIndex < foundFiles.Length; ++foundFileIndex) {
          if (!allFoundFiles.Contains(foundFiles[foundFileIndex])) {
            allFoundFiles.Add(foundFiles[foundFileIndex]);
          }
        }

      }

      return allFoundFiles;
    }
  }

  internal class Program {
    static void Main(string[] args) {
      bool isProgramWorking = true;
      int selectedFileIndex;
      string userKeywords;
      string userDirectory;
      string newContent;
      string selectedFileExtension;
      int userChoose;

      while (isProgramWorking) {
        Console.Write("Введите ключевые слова для поиска файлов (разделять через пробел): ");
        userKeywords = Console.ReadLine();
        SeachTextFilesByKeywords.Keywords = userKeywords.Split(new char[] { ' ' });

        do {
          Console.Write("Введите абсолютный путь директории, в которой будет осуществлен поиск (пропустите, чтобы оставить по умолчанию): ");
          userDirectory = Console.ReadLine();
          if (userDirectory == "") {
            break;
          }
          SeachTextFilesByKeywords.CurrentDirectoryPath = userDirectory;
        } while (!Directory.Exists(userKeywords));

        List<string> files = SeachTextFilesByKeywords.Seach();
        if (files.Count == 0) {
          Console.WriteLine("В данной директории не было найдено ни одного файла по данным ключевым словам :(");
        } else {
          Console.WriteLine($"Надено файлов: {files.Count}");
          for (int fileIndex = 0; fileIndex < files.Count; ++fileIndex) {
            Console.WriteLine((fileIndex + 1) + " - " + files[fileIndex].Substring(SeachTextFilesByKeywords.CurrentDirectoryPath.Length + 1));
          }
          Console.Write("Выберите файл (введите число-индекс): ");
          selectedFileIndex = Int32.Parse(Console.ReadLine()) - 1;

          TextFile textFile = new TextFile(files[selectedFileIndex]);
          selectedFileExtension = textFile.FileExtension;
          Memento memento = textFile.Save(); // Save the stat
          Console.WriteLine($"Содержимое файла {files[selectedFileIndex]}:");
          Console.WriteLine(textFile.GetText());

          Console.WriteLine("Введите новое содержимое файла (нажмите Enter, чтобы закончить):");
          newContent = Console.ReadLine();

          Console.Write("Какие действия выполнить с файлом?\n1 - Не сохранять изменения\n2 - Сохранить изменения\n3 - Сохранить и сериализовать в JSON\n4 - Сохранить и сериализовать в XML\nВведите число: ");
          userChoose = Int32.Parse(Console.ReadLine());

          switch (userChoose) {
            case 1:
              Console.WriteLine("Файл закрыт без изменений");
              break;
            case 2:
              textFile.UpdateText(newContent);
              Console.WriteLine("Файл сохранен. Новое содержимое:");
              Console.WriteLine(textFile.GetText());
              Console.Write("Отменить изменения? (1 - да/2 - нет): ");
              userChoose = Int32.Parse(Console.ReadLine());
              if (userChoose == 1) {
                textFile.Undo();
              }
              break;
            case 3:
              textFile.UpdateText(newContent);
              textFile.SerializeToJson(SeachTextFilesByKeywords.CurrentDirectoryPath + "\\" + textFile.FileName + ".json");
              break;
            case 4:
              textFile.UpdateText(newContent);
              textFile.SerializeToXml(SeachTextFilesByKeywords.CurrentDirectoryPath + "\\" + textFile.FileName + ".xml");
              break;
          }

          Console.Write("Продолжить? (1 - да/2 - нет): ");
          userChoose = Int32.Parse(Console.ReadLine());
          if (userChoose == 2) {
            isProgramWorking = false;
          }
        }
      }

      Console.WriteLine("Работа программы завершена. Нажмите любую клавишу, чтобы закрыть");
      // Ожидание нажатия клавиши (чтобы окно не закрывалось сразу после выполнения программы)
      Console.ReadKey();
    }
  }
}
