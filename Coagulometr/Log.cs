using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coagulometr
{
    public class Log
    {
        public static readonly string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        private static object sync = new object();

        public static void Write(string text)
        {
            try
            {
                // Путь .\\Log
                // string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                    Directory.CreateDirectory(pathToLog); // Создаем директорию, если нужно
                string filename = Path.Combine(pathToLog, string.Format("{0}_{1:dd.MM.yyy}.log",
                AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss}] [{1}] \r\n",
                DateTime.Now, text);
                lock (sync)
                {
                    File.AppendAllText(filename, fullText, Encoding.UTF8);
                }
            }
            catch
            {
                // Перехватываем все и ничего не делаем
            }
        }
        public static void Write_e(string ex)
        {
            try
            {
                // Путь .\\Log
                // string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                    Directory.CreateDirectory(pathToLog); // Создаем директорию, если нужно
                string filename = Path.Combine(pathToLog, string.Format("{0}_Error_{1:dd.MM.yyy}.log",
                AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss}] [{1}]\r\n",
                DateTime.Now, ex);
                lock (sync)
                {
                    File.AppendAllText(filename, fullText, Encoding.UTF8);
                }
            }
            catch
            {
                // Перехватываем все и ничего не делаем
            }
        }

        public static void Write_ex(Exception ex)
        {
            try
            {
                // Путь .\\Log
                string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                    Directory.CreateDirectory(pathToLog); // Создаем директорию, если нужно
                string filename = Path.Combine(pathToLog, string.Format("{0}_Error_{1:dd.MM.yyy}.log",
                AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss}] [{1}.{2}()] {3}\r\n",
                DateTime.Now, ex.TargetSite.DeclaringType, ex.TargetSite.Name, ex.Message);
                lock (sync)
                {
                    File.AppendAllText(filename, fullText, Encoding.UTF8);
                }
            }
            catch
            {
                // Перехватываем все и ничего не делаем
            }
        }

        public static void Write_res(string text)
        {
            try
            {
                // Путь .\\Log
                string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                    Directory.CreateDirectory(pathToLog); // Создаем директорию, если нужно
                string filename = Path.Combine(pathToLog, string.Format("{0}_Results_{1:dd.MM.yyy}.log",
                AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss}] [{1}] \r\n",
                                DateTime.Now, text);
                lock (sync)
                {
                    File.AppendAllText(filename, fullText, Encoding.UTF8);
                }
            }
            catch
            {
                // Перехватываем все и ничего не делаем
            }
        }


        public static async void WriteLog(string message)
        {
            using (StreamWriter writer = new StreamWriter(pathToLog, false))
            {
                await writer.WriteAsync(message);
            }
        }

        public static async Task<string> ReadLog()
        {
            using (StreamReader reader = new StreamReader(pathToLog, false))
            {
                string message = await reader.ReadToEndAsync();
                return message;
            }
        }
    }
}
