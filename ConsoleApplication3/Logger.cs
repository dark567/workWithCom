using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{
    internal static class Logger
    {
        private static BlockingCollection<string> _blockingCollection;
        private static string _filename = $"logLogger-{DateTime.Now:dd.MM.yyy}.txt";
        private static Task _task;

        static Logger()
        {
            _blockingCollection = new BlockingCollection<string>();

            _task = Task.Factory.StartNew(() =>
            {
                using (var streamWriter = new StreamWriter(_filename, true, Encoding.UTF8))
                {
                    streamWriter.AutoFlush = true;

                    foreach (var s in _blockingCollection.GetConsumingEnumerable())
                        streamWriter.WriteLine(s);
                }
            },
            TaskCreationOptions.LongRunning);
        }

        public static void WriteLog(string action, int errorCode, string errorDiscription)
        {
            _blockingCollection.Add($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} действие: {action}, код: {errorCode.ToString()}, описание: { errorDiscription} ");
        }

        public static void Flush()
        {
            _blockingCollection.CompleteAdding();
            _task.Wait();
        }
    }
}

/*
 Чтобы не было проблем с занятым файлом, к нему должно быть обращение только из одного места.
Файловый поток при этом не должен постоянно открываться и закрываться, т. к. это медленные операции. 
Отрываем его один раз и дальше просто используем.
Чтобы запись в лог отрабатывала максимально быстро, можно поступить так: пишем не сразу в файл, 
а в промежуточную потокобезопасную очередь. Отдельная задача выгребает сообщения из очереди и пишет в файл (БД, посылает по сети).

Здесь в статическом конструкторе логгера создаётся потокобезопасная коллекция, в основе которой по умолчанию лежит очередь (что нам и нужно). Здесь же запускаем Task. Открываем файл на запись.
Метод WriteLog отрабатывает максимально быстро - он всего лишь кладёт сообщение в очередь.
Задача будет ожидать в цикле foreach на методе GetConsumingEnumerable: как только появляются новые данные - он будет выполняться. 
Выход из цикла произойдёт только после вызова метода CompleteAdding. Для этого предусмотрен метод Flush, который желательно вызвать при завершении программы. 
При этом блокирующая коллекция получит сигнал о завершении поступления данных. После чего закроется файл и завершится задача.
Но это не обязательно: обратите внимание на AutoFlush = true - это не даст потерять данные при неожиданном закрытии программы (краше кода).
     */
