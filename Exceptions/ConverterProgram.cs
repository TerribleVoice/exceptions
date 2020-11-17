using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NLog;

namespace Exceptions
{
    public class ConverterProgram
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static void Main(params string[] args)
        {
            try
            {
                var filenames = args.Any() ? args : new[] {"text.txt"};
                var settings = LoadSettings();
                ConvertFiles(filenames, settings);
            }
            catch (AggregateException e)
            {
                log.Error(e.InnerException);
            }
            catch (InvalidOperationException)
            {
                log.Error("XmlException Не удалось прочитать файл настроек");
            }
        }

        private static void ConvertFiles(string[] filenames, Settings settings)
        {
            var tasks = filenames
                .Select(fn => Task.Run(() => ConvertFile(fn, settings))) 
                .ToArray();
            Task.WaitAll(tasks); 
        }

        private static Settings LoadSettings() 
        {
            var serializer = new XmlSerializer(typeof(Settings));
            var content = File.ReadAllText("settings.xml");
            return (Settings) serializer.Deserialize(new StringReader(content));
        }

        private static void ConvertFile(string filename, Settings settings)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.SourceCultureName);
            if (settings.Verbose)
            {
                log.Info("Processing file " + filename);
                log.Info("Source Culture " + Thread.CurrentThread.CurrentCulture.Name);
            }

            var lines = PrepareLines(filename);
            try
            {
                var convertedLines = lines
                    .Select(ConvertLine)
                    .Select(s => s.Length + " " + s);
                File.WriteAllLines(filename + ".out", convertedLines);
            }
            catch (Exception e)
            {
                throw new Exception($"Не удалось сконвертировать {filename}", e);
            }
        }

        private static IEnumerable<string> PrepareLines(string filename)
        {
            var lineIndex = 0;
            foreach (var line in File.ReadLines(filename))
            {
                if (line == "") continue;
                yield return line.Trim();
                lineIndex++;
            }
            yield return lineIndex.ToString();
        }

        public static string ConvertLine(string arg)
        {
            if (TryConvertAsDateTime(arg, out var result))
                return result;
            if (TryConvertAsDouble(arg, out result))
                return result;
            if (TryConvertAsCharIndexInstruction(arg, out result))
                return result;
            throw new Exception("Некорректная строка");
        }

        private static bool TryConvertAsCharIndexInstruction(string s, out string result)
        {
            result = default;
            var parts = s.Split();
            if (parts.Length < 2) return false;
            var charIndex = int.Parse(parts[0]);
            if (charIndex < 0 || charIndex >= parts[1].Length)
                return false;
            var text = parts[1];
            result = text[charIndex].ToString();
            return true;
        }

        private static bool TryConvertAsDateTime(string arg, out string result)
        {
            var isDone = DateTime.TryParse(arg, out var dateTimeResult);
            result = isDone ? dateTimeResult.ToString(CultureInfo.InvariantCulture) : default;
            
            return isDone;
        }

        private static bool TryConvertAsDouble(string arg, out string result)
        {
            var peaceDone = double.TryParse(arg, out var doubleResult);
            result = peaceDone ? doubleResult.ToString(CultureInfo.InvariantCulture) : default;
            
            return peaceDone;
        }
    }
}