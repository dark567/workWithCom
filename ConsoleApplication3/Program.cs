using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace ExchangeDataCom
{

    class Program
    {
        #region Trap application termination

        static bool exitSystem = false;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");
            Log.Write("Exiting system due to external CTRL-C, or process kill, or shutdown");

            //do your cleanup here
            Thread.Sleep(5000); //simulate some cleanup delay

            Console.WriteLine("Cleanup complete");

            //allow main to run off
            exitSystem = true;

            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);

            return true;
        }

        #endregion


        //public static string TypeAnaliz = "G";
        public static string TypeAnaliz = GetValueIni("MainSettings", "type") ?? "G";
        public static string VerApp = $"ver{TypeAnaliz}: {typeof(Program).Assembly.GetName().Version.ToString()}";

        static SerialPort mySerialPort = new SerialPort(GetValueIni("CC-4000", "COM_Port") ?? "COM1");
        public static string path_db;
        public static int countLineC = 0;
        public static int countLineG = 0;

        public static ArrayList listGoods = new ArrayList();
        public static string lineAllC = null;
        public static string lineAllG = null;
        public static string BarCode = null;

        public static Thread myThreadCheckCom = new Thread(new ThreadStart(CheckOpeningCom));


        public static void Main(string[] args)
        {

            Form1_Load();

            // Some biolerplate to react to close window event, CTRL-C, kill, etc
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            //start your multi threaded program here
            Program p = new Program();
            p.Start();

            //hold the console so it doesn’t run off the end
            while (!exitSystem)
            {
                Thread.Sleep(150);
            }
        }

        private static void Form1_Load()
        {


            try
            {
                CryptoClass crypto = new CryptoClass();
                //if (!crypto.Form_LoadTrue()) Close();

                string date = crypto.GetDecodeKey("keyfile.dat").Substring(crypto.GetDecodeKey("keyfile.dat").IndexOf("|") + 1);

                Logger.WriteLog(date, 0, "res == 0");

                if (DateTime.Parse(date).AddDays(1) <= DateTime.Now) Close();
                Console.Title = "ComRead:" + VerApp + "......." + date;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message, 0, "res == 0");
            }

        }

        private static void Close()
        {
            //allow main to run off
            Environment.Exit(0);
        }

        public void Start()
        {
            // start a thread and start doing some processing
            Console.WriteLine("Thread started, processing..");
            Log.Write($"Thread started, processing..");
            First_Load();
            Console.WriteLine($"{DateTime.Now} Connection bd - {(CheckDbConnection() == true ? "Open" : "Closed")}");
            Log.Write($"Connection bd - { (CheckDbConnection() == true ? "Open" : "Closed")}");

            ComWorking();

            Console.ReadKey();
        }

        private static bool CheckDbConnection()
        {
            //return true;
            try
            {
                using (var connection = GetConnection())
                {
                    if (connection.State == ConnectionState.Open) return true;
                    else return false;
                }
            }
            catch (FbException ex)
            {
                //logger.Warn(LogTopicEnum.Agent, "Error in DB connection test on CheckDBConnection", ex);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
                Log.Write_ex(ex);
                Beep(count: 3);
                return false; // any error is considered as db connection error for now
            }
        }

        private void UpdateRow(string queryString)
        {
            //  string queryString =
            //           "INSERT INTO Customers (CustomerID, CompanyName) Values('NWIND', 'Northwind Traders')";
            FbCommand command = new FbCommand(queryString);

            using (FbConnection connection = GetConnection())
            {
                command.Connection = connection;
                try
                {
                    connection.Open();
                    int res = command.ExecuteNonQuery();
                    Console.WriteLine($"UpdateRow: SUCCESS - {res.ToString()}\n", "res");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UpdateRow: update fail: {ex.Message}\n", "ex");
                }
                // The connection is automatically closed at 
                // the end of the Using block.
            }
        }


        private static void First_Load()
        {
            try
            {
                var path = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;

                // Console.WriteLine(path + @"\set.ini");
                //Создание объекта, для работы с файлом
                INIManager manager = new INIManager(path + @"\set.ini");
                //Получить значение по ключу name из секции main
                path_db = manager.GetPrivateString("Connection", "db");

                Log.Write("Start program");

                // File.AppendAllText(Application.StartupPath + @"\program.log", "ConnectionIni:" + path_db + ", \t" + DateTime.Now +"\n");
                Log.Write("ConnectionIni:" + path_db);
                //Записать значение по ключу age в секции main
                // manager.WritePrivateString("main", "age", "21");
            }
            catch (Exception ex)
            {
                // File.AppendAllText(Application.StartupPath + @"\error.log", "Ini file not found, \t" + DateTime.Now + "\n");
                Log.Write_ex(ex);
            }
        }


        /// <summary>
        /// сделать проверку на доступность com во время работы
        /// </summary>
        private static void ComWorking()
        {
            //  p = new SerialPort(GetValueIni("CC-400", "COM_Port"));
            //p.BaudRate = int.Parse(GetValueIni("CC-400", "COM_BaudRate"));

            mySerialPort.BaudRate = int.Parse(GetValueIni("CC-4000", "COM_BaudRate"));
            mySerialPort.Parity = Parity.None;
            mySerialPort.WriteTimeout = 500;
            mySerialPort.ReadTimeout = 500;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.DataBits = 8;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.NewLine = Environment.NewLine;

            try
            {
                if (mySerialPort.IsOpen)
                {
                    Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - already Open");
                    Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - already Open");
                }
                else
                {
                    try
                    {
                        mySerialPort.Open();
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - Closed");
                        Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - Closed");
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    if (!mySerialPort.IsOpen)
                    {
                        Thread.Sleep(250);
                        ComWorking();
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - already Open");
                        Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - already Open");

                        // создаем новый поток
                        if (!myThreadCheckCom.IsAlive)
                        {
                            Console.WriteLine("myThreadCheckCom start");
                            //myThreadCheckCom = new Thread(new ThreadStart(CheckOpeningCom));
                            myThreadCheckCom.Start(); // запускаем поток
                        }
                        // else myThreadCheckCom.Abort();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now} {ex.Message}");
                Log.Write_ex(ex);
                Console.ForegroundColor = ConsoleColor.White;
            }
            System.Threading.Thread.Sleep(500);

            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            Console.WriteLine(new String('-', 10));
            Console.WriteLine();

            Console.ReadKey();

            mySerialPort.Close();
        }

        private static void CheckOpeningCom()
        {
            bool prOpen = false;
            bool prClose = false;

            do
            {
                Thread.Sleep(2000);

                //if (mySerialPort.IsOpen)
                //{
                //    Console.ForegroundColor = ConsoleColor.Yellow;
                //    Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - already Open");
                //    Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - already Open");
                //    Console.ForegroundColor = ConsoleColor.White;
                //}
                //else
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - Closed");
                //    Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - Closed");
                //    Console.ForegroundColor = ConsoleColor.White;
                //    ComWorking();
                //}

                if (mySerialPort.IsOpen)
                {

                    if (!prClose)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - Open");
                        Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - Open");
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    prClose = true;
                    prOpen = false;
                }

                if (!mySerialPort.IsOpen)
                {
                    try
                    {
                        mySerialPort.Open();
                    }
                    catch
                    {
                        // prOpen = false;
                    }

                    // ComWorking();

                    if (!prOpen)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - Closed");
                        Log.Write($"Port {GetValueIni("CC-4000", "COM_Port")} - Closed");
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    prClose = false;
                    prOpen = true;
                    // ComWorking();
                }


                //else Console.WriteLine($"{DateTime.Now} Port {GetValueIni("CC-4000", "COM_Port")} - already Open");

            } while (true);
        }


        private static void DataReceivedHandler(
                            object sender,
                            SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            // Console.Write($"{indata}");
            Log.Write(indata);


            // Console.ForegroundColor = ConsoleColor.Red;
            // Console.WriteLine($"[{indata.Length}], [{indata}]");
            //Console.Write($"[{indata.Length}],{indata}");
            //if (indata.IndexOf("CC-3003") > -1) workWithDataComAsync(indata); //!!!
            // workWithDataComAsync(indata);
            // Console.WriteLine(mySerialPort.ReadLine());
            // Console.ForegroundColor = ConsoleColor.White;



            //if (indata.IndexOf("HumaClot") > -1)
            //{
            //mySerialPort.WriteLine("AT+CMGL=\"ALL\"");
            //mySerialPort.WriteLine("---");
            System.Threading.Thread.Sleep(1000);

            while (mySerialPort.BytesToRead > 0)
            {
                try
                {
                    string line = mySerialPort.ReadLine();
                    Console.WriteLine($"{DateTime.Now} {line}");

                    lineAllG = null; //обнуление

                    Log.Write(line);
                    workWithDataComAsync(line);
                    //  Console.ForegroundColor = ConsoleColor.Red;
                    // workWithDataCom(mySerialPort.ReadLine());
                    // Console.ForegroundColor = ConsoleColor.White;

                    //Beep(3); // потом вернуть
                }
                catch { }
            }
            //}


        }

        private static void workWithDataComAsync(string line)
        {
            // Console.WriteLine($"[{line.Length}], [{line}]");
            //Console.WriteLine($"{line}");

            bool pr = false;

            // Console.WriteLine();

            #region Когуалометр раскомментировать потом
            if (TypeAnaliz == "C" || TypeAnaliz == "c")
            {
                if (line.IndexOf("CC-3003") == 0)
                {
                    countLineC = 0;
                    lineAllC = null;
                    //  Console.WriteLine(line);
                }
                else
                {
                    countLineC++;
                    //Console.WriteLine("++");
                }

                if (!string.IsNullOrEmpty(line) && (line.Length > 5))
                {
                    String[] substringsCharC = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < substringsCharC.Length; j++)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        //  Console.Write ($" [{substringsChar.Length}] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        if (substringsCharC.Length > 1)
                            if (substringsCharC[1] != "BEGIN") pr = true;
                        //  else pr = false;
                    }

                    //if (pr) lineAll += line;
                    //else lineAll = null;
                    lineAllC = line;

                    Log.Write_res($"[{countLineC}] - {lineAllC}");
                    CreateModelC(lineAllC); //!!!
                }
            }
            #endregion

            #region Гематологический
            if (TypeAnaliz == "G" || TypeAnaliz == "g")
            {
                lineAllG = null;

                if (line.IndexOf("Serial No.:") != 0)
                {
                    //lineAllG = null;
                    countLineG++;


                }
                else
                {
                    countLineG = 0;
                    BarCode = "n/a";
                }

                if (!string.IsNullOrEmpty(line) && (line.Length > 5))
                {
                    char[] delimiterChars = { ' ', '\t' };

                    String[] substringsCharG = line.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                    //for (int j = 0; j < substringsChar.Length; j++)
                    //{
                    //    lineAllG += $" -[{j}][{substringsChar[j]}]";
                    //}

                    lineAllG = line;

                    if (countLineG == 2) BarCode = substringsCharG[2] != null ? substringsCharG[2] : "n/a";

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{DateTime.Now} [{countLineG}] - {lineAllG}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Log.Write_res($"[{countLineG}] - {lineAllG}");

                    CreateModelG(lineAllG); //!!!
                }
            }
            #endregion
        }

        private static void CreateModelG(string lineAllG)
        {
            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine($"{DateTime.Now} CreateModelG - {lineAllG}");
            //Console.ForegroundColor = ConsoleColor.White;

            if (!string.IsNullOrEmpty(lineAllG) /*&& (countLine != 0)*/)
            {
                //String[] substringsCharAll = lineAllG.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                char[] delimiterChars = { ' ', '\t' };

                String[] substringsCharAll = lineAllG.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                //создание и передача модели в бд
                Model model = new Model();

                // если гематологический
                //if (substringsCharAll[0].InCont("WBC", "LYM", "NEU", "MON", "EOS", "BAS", "LY%", "NE%", "MO%", "EO%", "BA%", "RBC", "HGB", "HCT", "MCV", "MCH", "MCHC", "RDWc", "MCV", "PLT"))
                if (Extensions.InContVerTwo(substringsCharAll[0]))/*varCheck 2*/
                {
                    model = new Model(type: substringsCharAll[0], code: BarCode, goods: substringsCharAll[0], typeGoods: substringsCharAll[3], value01: substringsCharAll[1], value02: substringsCharAll[2]/*, value03: substringsCharAll[6], value04: substringsCharAll[7]*/);

                    Console.WriteLine($"Extensions.InContVerTwo - type[0]: {substringsCharAll[0]}, code: {BarCode}, goods[0]: {substringsCharAll[0]}, typeGoods[3]: {substringsCharAll[3]}, value01[1]: {substringsCharAll[1]}, value02[1]: {substringsCharAll[2]}");
                }

                if (model.Type != null)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"\nCreateModelG - [model.Type]{model.Type} [barCode1]{model.Code} [model.Goods]{model.Goods} [model.TypeGoods]{model.TypeGoods} [value01 - 4]{model.Value01} [value02 - 5]{model.Value02}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nCreateModelG(null) - [model.Type]{model.Type} [barCode1]{model.Code} [model.Goods]{model.Goods} [model.TypeGoods]{model.TypeGoods} [value01 - 4]{model.Value01} [value02 - 5]{model.Value02}");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                //string query = QueryGetGem(model)?.Query; //!!!

                string query = QueryGetG(model)?.Query; //!!!

                // if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBd_(model); //!!! ver2 
                if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBdThread(model); //!!! ver3 потом включить

                if (!string.IsNullOrEmpty(query) && query != "Query NULL")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(query);
                    Log.Write(query);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                //UpdateRowBd_(model);

                //if (!string.IsNullOrEmpty(query)) UpdateRowBd_(model); //!!! ver2
            }
        }

        private static void CreateModelC(string lineAll)
        {
            if (!string.IsNullOrEmpty(lineAll) /*&& (countLine != 0)*/)
            {
                String[] substringsCharAll = lineAll.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                //for (int i = 0; i < substringsCharAll.Length; i++)
                //{
                //    Console.ForegroundColor = ConsoleColor.Green;
                //    Console.Write($"[{i}] {substringsCharAll[i]}  ");
                //    Console.ForegroundColor = ConsoleColor.White;
                //}

                //создание и передача модели в бд
                Model model;
                if (substringsCharAll[2] == "1FIBRYNOGEN")
                {
                    model = new Model(type: substringsCharAll[0], code: getBarCodeCorrect(substringsCharAll[1]), goods: substringsCharAll[2], typeGoods: "n/a", value01: substringsCharAll[3]/*, value02: substringsCharAll[5], value03: substringsCharAll[6], value04: substringsCharAll[7]*/);
                }
                else
                {
                    model = new Model(type: substringsCharAll[0], code: getBarCodeCorrect(substringsCharAll[1]), goods: substringsCharAll[2], typeGoods: substringsCharAll[3], value01: substringsCharAll[4]/*, value02: substringsCharAll[5], value03: substringsCharAll[6], value04: substringsCharAll[7]*/);
                }

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"\n{DateTime.Now} [0]{model.Type} [1]{model.Code} [2]{model.Goods} [3]{model.TypeGoods} [4]{model.Value01}");
                Console.ForegroundColor = ConsoleColor.White;


                string query = QueryGetC(model)?.Query; //!!!
                                                        //Console.WriteLine(query);
                                                        //if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBd_(model); //!!! ver2
                if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBdThread(model); //!!! ver3
                //if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBdThread(query); //!!! ver3

                if (!string.IsNullOrEmpty(query) && query != "Query NULL")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{DateTime.Now} CreateModelC: {query} ");
                    Log.Write(query);
                    Console.ForegroundColor = ConsoleColor.White;
                }

                //UpdateRowBd_(model);
            }
        }


        /// <summary>
        /// асинхронный метод запускающий асинхронно метод изменения данных в бд
        /// </summary>
        /// <param name="query"></param>
        static async void UpdateRowBd_(Model query)
        {
            Console.WriteLine("Начало метода async");
            await Task.Run(() => UpdateRowBd(query)); // выполняется асинхронно
            Console.WriteLine("Конец метода async");
        }

        /// <summary>
        ///  метод запускающий метод изменения данных в бд
        /// </summary>
        /// <param name="query"></param>
        static void UpdateRowBdThread(Model query)
        {
            //string queryBd = query.Query; // ver2
            //string queryBd = QueryGetC(query)?.Query;

            //Console.ForegroundColor = ConsoleColor.DarkGreen;
            //Console.WriteLine($"\n{DateTime.Now} UpdateRowBdThread: {queryBd}");
            //Console.ForegroundColor = ConsoleColor.White;

            //Console.WriteLine("Начало метода myThread");
            Thread myThread = new Thread(new ParameterizedThreadStart(UpdateRowBdThread));
            myThread.Start(query);
            // Console.WriteLine("Конец метода myThread");
        }

        /// <summary>
        /// внесение данных в бд
        /// </summary>
        /// <param name="query"></param>
        private static void UpdateRowBd(Model query)
        {
            //Console.WriteLine($"UpdateRowBd");
            //Log.Write($"UpdateRowBd)");
            //Console.ForegroundColor = ConsoleColor.DarkGreen;
            //Console.WriteLine($"UpdateRowBd: {QueryGetC(query).Query}");
            //Console.ForegroundColor = ConsoleColor.White;
            //Beep(1, query);
            //Log.Write($"UpdateRowBd - {QueryGet(query).Query}");

            //Thread.Sleep(250);

            FbConnection conn = GetConnection();
            FbTransaction fbt = conn.BeginTransaction();
            try
            {
                // string queryBd = TypeAnaliz == "G" ? QueryGetGem(query).Query : QueryGetC(query).Query;

                string queryBd = query.Query; // ver2

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"\n{DateTime.Now} queryBd: {queryBd}");
                Console.ForegroundColor = ConsoleColor.White;
                Beep(1, query);


                using (FbCommand cmd = new FbCommand(queryBd, conn))
                {
                    cmd.Transaction = fbt;
                    int res = cmd.ExecuteNonQuery();

                    // Console.ForegroundColor = ConsoleColor.Yellow;
                    //if (res == 0)
                    //{
                    //    Beep(1, query);
                    //    Console.ForegroundColor = ConsoleColor.Red;
                    //}

                    if (res == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
                        Beep(1, query);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
                    }

                    Console.ForegroundColor = ConsoleColor.White;

                    //Log.Write($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");


                    fbt.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                //Log.Write_ex(ex);
                // Log.WriteLog(ex.Message);
                Logger.WriteLog(ex.Message, 0, "res == 0");
                Console.ForegroundColor = ConsoleColor.White;
                fbt.Rollback();
            }
            finally
            {
                // fbt.Commit();
                conn.Close();
            }

            //OdbcCommand command = new OdbcCommand(query);


            //using (OdbcConnection connection = new OdbcConnection(GetValueIni("Connection", "dbname")))
            //{
            //    command.Connection = connection;
            //    try
            //    {
            //        connection.Open();
            //        int res = command.ExecuteNonQuery();
            //        Console.WriteLine($"UpdateRow: SUCCESS - {res.ToString()}\n", "res");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"UpdateRow: update fail: {ex.Message}\n", "ex");
            //    }
            //    // The connection is automatically closed at 
            //    // the end of the Using block.
            //}

            // Console.WriteLine($"{query}");
        }

        /// <summary>
        /// внесение данных в бд
        /// </summary>
        /// <param name="query"></param>
        private static void UpdateRowBdThread(object queryObj)
        {
            Model query = (Model)queryObj;

            #region
            //Console.WriteLine($"UpdateRowBd");
            //Log.Write($"UpdateRowBd)");

            // Console.WriteLine($"UpdateRowBd - {QueryGet(query).Query}");
            //Log.Write($"UpdateRowBd - {QueryGet(query).Query}");

            //Thread.Sleep(250);

            //FbConnection conn = GetConnection();
            //FbTransaction fbt = conn.BeginTransaction();
            //try
            //{
            //    // string queryBd = TypeAnaliz == "G" ? QueryGetGem(query).Query : QueryGetC(query).Query;

            //    string queryBd = query.Query; // ver2

            //    using (FbCommand cmd = new FbCommand(queryBd, conn))
            //    {
            //        cmd.Transaction = fbt;
            //        int res = cmd.ExecuteNonQuery();

            //        // Console.ForegroundColor = ConsoleColor.Yellow;
            //        //if (res == 0)
            //        //{
            //        //    Beep(1, query);
            //        //    Console.ForegroundColor = ConsoleColor.Red;
            //        //}

            //        if (res == 0)
            //        {
            //            Console.ForegroundColor = ConsoleColor.Red;
            //            Console.WriteLine($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
            //            Beep(1, query);
            //        }
            //        else
            //        {
            //            Console.ForegroundColor = ConsoleColor.Yellow;
            //            Console.WriteLine($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
            //        }

            //        Console.ForegroundColor = ConsoleColor.White;

            //        Log.Write($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
            //        //Log.WriteLog($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");

            //        fbt.Commit();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine(ex.Message);
            //    Log.Write_ex(ex);
            //    //Log.WriteLog(ex.Message);
            //    Console.ForegroundColor = ConsoleColor.White;
            //    fbt.Rollback();
            //}
            //finally
            //{
            //    // fbt.Commit();
            //    conn.Close();
            //}
            #endregion

            string queryBd = null;

            if (TypeAnaliz == "C")
                queryBd = QueryGetC(query)?.Query;
            else if (TypeAnaliz == "G")
                queryBd = QueryGetG(query)?.Query;

            if (!string.IsNullOrEmpty(queryBd))
            {
                OdbcCommand command = new OdbcCommand(queryBd);

                using (OdbcConnection connection = new OdbcConnection(GetValueIni("Connection", "dbname")))
                {
                    command.Connection = connection;
                    try
                    {
                        connection.Open();
                        int res = command.ExecuteNonQuery();

                        if (res == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
                            Logger.WriteLog($"{queryBd}", 0, "res == 0");
                            Logger.WriteLog($"Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}", 0, "res == 0");
                            Beep(1, query);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");
                            Logger.WriteLog($"{queryBd}", 1, "res == 1");
                            Logger.WriteLog($"Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}", 1, "res == 1");
                        }

                        Console.ForegroundColor = ConsoleColor.White;

                        //Log.WriteLog($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");

                        //Console.WriteLine($"UpdateRow: SUCCESS - {res.ToString()}\n", "res");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UpdateRow: update fail: {ex.Message}\n", "ex");
                        //Log.WriteLog(ex.Message);
                        Logger.WriteLog(ex.Message, 0, "res == 0");
                    }
                    // The connection is automatically closed at 
                    // the end of the Using block.
                }
            }
        }

        private static string getBarCodeCorrect(string barCode)
        {
            return barCode.Substring(3); ;
        }

        private static QueryModel QueryGetG(Model model)
        {
            QueryModel query = null;

            if (Extensions.InContVerTwo(model.Goods))/*varCheck 2*/
            {

                if (IsFloat(model.Value01))
                {
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                     "d.result = '" + GetValue(ParseFloat(model.Value01), model.Goods) + "', \n" +
              "d.result_text = '" + GetValue(ParseFloat(model.Value01), model.Goods) + "', \n" +
              "d.hardware_date_updated = current_timestamp, " +
              "d.hardware_info = ('GEMATOL') \n" +
              "where ID = (select R.ID  \n" +
              "from JOR_CHECKS_DT D  \n" +
              "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
              "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
              "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
              "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
               "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
              "and (R.CODE_NAME = cast(  \n" +
              "('" + GetGoods(model.Goods) + "') as MIDDLE_NAME))  \n" +
              "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });

                    model.Query = query.Query;
                }
                else if (IsFloat(model.Value02))
                {
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                     "d.result = '" + GetValue(ParseFloat(model.Value02), model.Goods) + "', \n" +
              "d.result_text = '" + GetValue(ParseFloat(model.Value02), model.Goods) + "', \n" +
              "d.hardware_date_updated = current_timestamp, " +
              "d.hardware_info = ('GEMATOL') \n" +
              "where ID = (select R.ID  \n" +
              "from JOR_CHECKS_DT D  \n" +
              "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
              "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
              "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
              "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
               "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
              "and (R.CODE_NAME = cast(  \n" +
              "('" + GetGoods(model.Goods) + "') as MIDDLE_NAME))  \n" +
              "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });

                    model.Query = query.Query;
                }
                else
                {
                    query = (new QueryModel()
                    { Type = "Query NULL", Query = "Query NULL" });
                }
            }
            return query;

        }

        private static double GetValue(float v, string goods)
        {
            List<string> Goods = new List<string> { "WBC", /*"LYM", "NEU", "MON", "EOS", "BAS", */"LY%", "NE%", "MO%", "EO%", "BA%", "RBC", "HGB", "HCT", "MCV", "MCH", "MCHC", "MPV", "PLT" };
            if (Goods.Contains(goods))
            {
                if (goods == "LY%") return Math.Round(v);
                if (goods == "NE%") return Math.Round(v);
                if (goods == "MO%") return Math.Round(v);
                if (goods == "EO%") return Math.Round(v);
                if (goods == "BA%") return Math.Round(v);
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                return Double.Parse(v.ToString());
            }
            else return 0;
        }

        private static string GetGoods(string goods)
        {
            List<string> Goods = new List<string> { "WBC", /*"LYM", "NEU", "MON", "EOS", "BAS", */"LY%", "NE%", "MO%", "EO%", "BA%", "RBC", "HGB", "HCT", "MCV", "MCH", "MCHC", "MPV", "PLT" };
            if (Goods.Contains(goods))
            {
                if (goods == "LY%") goods = "LYM%";
                if (goods == "NE%") goods = "GRA%";
                if (goods == "MO%") goods = "MON%";
                if (goods == "EO%") goods = "EOZ%";
                if (goods == "BA%") goods = "BAZ%";
                return goods;
            }
            else return "n/a";
        }



        private static QueryModel QueryGetC(Model model)
        {
            QueryModel query = null;

            switch (model.Goods)
            {
                case "1APTT":
                    if (model.TypeGoods == "TIME")
                        query = (new QueryModel()
                        {
                            Type = model.Type,
                            Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                             "d.result = '" + model.Value01 + "', \n" +
                         "d.result_text = '" + model.Value01 + "', \n" +
                        //"d.result_text = '777', \n" +
                        "d.hardware_date_updated = current_timestamp, " +
                        "d.hardware_info = ('CC-4000') \n" +
                        "where ID = (select R.ID  \n" +
                        "from JOR_CHECKS_DT D  \n" +
                        "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                        "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                        "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                        "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                         "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                        //"and (D.BULB_NUM_CODE = cast('11545026' as NAME))  \n" +
                        "and (R.CODE_NAME = cast(  \n" +
                        "('APTT_t') as MIDDLE_NAME))  \n" +
                        "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                        });

                    break;
                case "1PT":
                    if (model.TypeGoods == "TIME")
                        query = (new QueryModel()
                        {
                            Type = model.Type,
                            Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                            "d.result = '" + model.Value01 + "', \n" +
                            "d.result_text = '" + model.Value01 + "', \n" +
                      "d.hardware_date_updated = current_timestamp, " +
                      "d.hardware_info = ('CC-4000') \n" +
                      "where ID = (select R.ID  \n" +
                      "from JOR_CHECKS_DT D  \n" +
                      "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                      "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                      "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                      "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                      "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                      "and (R.CODE_NAME = cast(  \n" +
                      "('PT_t') as MIDDLE_NAME))  \n" +
                      "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                        });
                    else if (model.TypeGoods == "INDEX")
                        query = (new QueryModel()
                        {
                            Type = model.Type,
                            Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                         "d.result = '" + model.Value01 + "', \n" +
                         "d.result_text = '" + model.Value01 + "', \n" +
                        "d.hardware_date_updated = current_timestamp, " +
                        "d.hardware_info = ('CC-4000') \n" +
                        "where ID = (select R.ID  \n" +
                        "from JOR_CHECKS_DT D  \n" +
                        "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                        "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                        "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                        "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                         "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                        "and (R.CODE_NAME = cast(  \n" +
                        "('PT_%') as MIDDLE_NAME))  \n" +
                        "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                        });
                    else if (model.TypeGoods == "INR")
                        query = (new QueryModel()
                        {
                            Type = model.Type,
                            Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                         "d.result = '" + model.Value01 + "', \n" +
                         "d.result_text = '" + model.Value01 + "', \n" +
                        "d.hardware_date_updated = current_timestamp, " +
                        "d.hardware_info = ('CC-4000') \n" +
                        "where ID = (select R.ID  \n" +
                        "from JOR_CHECKS_DT D  \n" +
                        "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                        "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                        "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                        "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                         "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                        "and (R.CODE_NAME = cast(  \n" +
                        "('PT_i') as MIDDLE_NAME))  \n" +
                        "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                        });
                    break;
                case "1TT":
                    if (model.TypeGoods == "TIME")
                        query = (new QueryModel()
                        {
                            Type = model.Type,
                            Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                             "d.result = '" + model.Value01 + "', \n" +
                      "d.result_text = '" + model.Value01 + "', \n" +
                      "d.hardware_date_updated = current_timestamp, " +
                      "d.hardware_info = ('CC-4000') \n" +
                      "where ID = (select R.ID  \n" +
                      "from JOR_CHECKS_DT D  \n" +
                      "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                      "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                      "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                      "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                       "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                      "and (R.CODE_NAME = cast(  \n" +
                      "('TT_t') as MIDDLE_NAME))  \n" +
                      "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                        });
                    break;
                case "1FIBRYNOGEN": //FIBRYNOGEN
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                         "d.result = '" + model.Value01 + "', \n" +
                  "d.result_text = '" + model.Value01 + "', \n" +
                  "d.hardware_date_updated = current_timestamp, " +
                  "d.hardware_info = ('CC-4000') \n" +
                  "where ID = (select R.ID  \n" +
                  "from JOR_CHECKS_DT D  \n" +
                  "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                  "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                  "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                  "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                   "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                  "and (R.CODE_NAME = cast(  \n" +
                  "('FIB_m') as MIDDLE_NAME))  \n" +
                  "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });
                    break;
                default:
                    query = (new QueryModel()
                    { Type = "Query NULL", Query = "Query NULL" });
                    break;
            }
            return query;
        }

        private static bool IsFloat(string val)
        {

            //return Double.TryParse(val, out _);
            return float.TryParse(val, NumberStyles.Any, new CultureInfo("en-US"), out _);

        }

        private static float ParseFloat(string val)
        {
            return float.Parse(val, NumberStyles.Any, new CultureInfo("en-US"));
        }


        private static void Beep(int count, Model query = null)
        {
            int x = count;

            // Set the Frequency 
            int frequency = 800;

            // Set the Duration 
            int duration = 200;
            for (int i = 1; i <= x; i++)
            {
                // Console.WriteLine();
                // Console.Write("Beep number {0}. ", i);
                // Console.WriteLine($"BarCode:{getBarCodeCorrect(query.Code)}, Value {query.Value01}");
                Console.Beep(frequency, duration);
            }
            Console.WriteLine();
        }


        private static FbConnection GetConnection()
        {
            string connectionString =
                "User=SYSDBA;" +
                "Password=masterkey;" +
                @"Database=" + path_db + ";" +
                "Charset=UTF8;" +
                "Pooling=true;" +
                "ServerType=0;";

            FbConnection conn = new FbConnection(connectionString.ToString());

            conn.Open();

            //if (conn.State == ConnectionState.Closed) Console.WriteLine(false);
            //else Console.WriteLine(true);

            return conn;
        }

        private static OdbcConnection GetConnectionODBC()
        {
            string connectionString =
                GetValueIni("Connection", "dbname");

            OdbcConnection conn = new OdbcConnection(connectionString.ToString());

            conn.Open();

            //if (conn.State == ConnectionState.Closed) Console.WriteLine(false);
            //else Console.WriteLine(true);

            return conn;
        }

        private static string GetValueIni(string Section, string Key) //Получить значение по секции и ключу
        {
            try
            {
                var path = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;

                //Console.WriteLine(path + @"\set.ini");

                //Создание объекта, для работы с файлом
                INIManager manager = new INIManager(path + @"\set.ini");

                //Console.WriteLine(manager.GetPrivateString(Section, Key));
                //Получить значение по секции и ключу
                return manager.GetPrivateString(Section, Key);
            }
            catch (Exception ex)
            {
                Log.Write_ex(ex);
                return "";
            }
        }
    }
}