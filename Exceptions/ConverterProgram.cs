﻿using System;
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
                /*if (e.InnerException?.GetType() == typeof(FileNotFoundException))
                    log.Error()*/
                log.Error(e.Message);
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
            IEnumerable<string> lines;
            try
            {
                lines = PrepareLines(filename);
            }
            catch
            {
                log.Error($"File {filename} not found"); 
                return;
            }
            var convertedLines = lines
                .Select(ConvertLine)
                .Select(s => s.Length + " " + s);
            File.WriteAllLines(filename + ".out", convertedLines);
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
            
            return TryConvertAsDouble(arg, out result) ? result : ConvertAsCharIndexInstruction(arg);
        }

        private static string ConvertAsCharIndexInstruction(string s)
        {
            var parts = s.Split();
            if (parts.Length < 2) return null;
            var charIndex = int.Parse(parts[0]);
            if ((charIndex < 0) || (charIndex >= parts[1].Length))
                return null;
            var text = parts[1];
            return text[charIndex].ToString();
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