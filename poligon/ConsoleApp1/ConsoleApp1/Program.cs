using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{

    public static class Extensions
    {
        public static List<string> listGoods = new List<string> { "WBC", /*"LYM", "NEU", "MON", "EOS", "BAS", */"LY%", "NE%", "MO%", "EO%", "BA%", "RBC", "HGB", /*"HCT", "MCV", "MCH", "MCHC", "MCV", */"PLT" };
        public static bool InCont<T>(this T item, params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            return items.Contains(item);
        }

        public static bool InCont_(string x)
        {
            if (listGoods.Contains(x)) return true;
            return false;

            //if (new[] { 1, 2, 3, 99 }.Contains(x))
            //{
            //    // do something
            //}
        }
    }

    class Program
    {
        public static string VerApp = $"ver: {typeof(Program).Assembly.GetName().Version.ToString()}";
        public static string TypeAnaliz = "G";
        public static string path_db;
        public static int countLine = 100;
        public static string lineAllG = null;
        public static string BarCode = null;
        public static int countLineG = 0;
        // public static string lineAll = null;

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

        static void Main(string[] args)
        {


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


        public void Start()
        {
            // start a thread and start doing some processing
            Console.WriteLine("Thread started, processing..");
            Log.Write($"Thread started, processing..");

            Console.Title = "ComRead:" + VerApp;
            First_Load();


            // ComWorking();

            //Console.ReadKey();
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

            string line = "Serial No.:     02100365\n" +
                    "RecNo: 4023\n" +
                    "Sample ID:      12214945\n" +
                    "Patient ID:     1\n" +
                    "Patient Name:\n" +
                    "Mode: Human\n" +
                    "Doctor:\n" +
                    "Birth(ymd):\n" +
                    "Sex: Male/n" +
                    "Test date(ymd): 20191206\n" +
                    "Test time(hm):  101841/n" +
                    "Param Flags   Value Unit[min - max]\n" +
                    "WBC             4.16    10 ^ 9 / L[4.00 - 9.00]\n" +
                    "LYM             1.34    10 ^ 9 / L[1.30 - 4.00]\n" +
                    "NEU             2.13    10 ^ 9 / L[2.00 - 7.50]\n" +
                    "MON             0.42    10 ^ 9 / L[0.15 - 0.70]\n" +
                    "EOS             0.15    10 ^ 9 / L[0.00 - 0.50]\n" +
                    "BAS             0.12    10 ^ 9 / L[0.00 - 0.15]\n" +
                    "LY% 32.3 %       [19.0 - 37.0]\n" +
                    "NE% 51.1 %       [40.0 - 75.0]\n" +
                    "MO% 10.1 %       [3.0 - 11.0]\n" +
                    "EO% 3.5 %       [0.0 - 5.0]\n" +
                    "BA% +3.0 %       [0.0 - 1.5]\n" +
                    "RBC + 5.91    10 ^ 12 / L[3.90 - 5.00]\n" +
                    "HGB + 213     g / L[120 - 160]\n" +
                    "HCT * 61.4 %       [36.0 - 52.0]\n" +
                    "MCV + 103.9   fL[76.0 - 96.0]\n" +
                    "MCH + 36.0    pg[27.0 - 32.0]\n" +
                    "MCHC            346     g / L[300 - 350]\n" +
                    "RDWc            15.4 %       [0.0 - 16.0]\n" +
                    "RDWs            57.2    fL[46.0 - 59.0]\n" +
                    "PLT - 111     10 ^ 9 / L[180 - 320]\n" +
                    "PLCR            25.22 %       [0.00 - 0.00]\n" +
                    "PLCC            28      10 ^ 9 / L[0 - 0]\n" +
                    "PDWc            40.3 %       [0.0 - 0.0]\n" +
                    "PDWs            17.1    fL[0.0 - 0.0]\n" +
                    "MPV - 6.4     fL[8.0 - 15.0]\n" +
                    "PCT             0.07 %       [0.00 - 0.00]\n" +
                    "Warnings:";

            String[] substringsChar = line.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int j = 0; j < substringsChar.Length; j++)
            {
                // Console.WriteLine($"[{j}] {substringsChar[j]}");

                if (substringsChar[j].IndexOf("Serial No.:") != 0)
                {
                    //lineAllG = null;
                    countLineG++;
                }
                else
                {
                    countLineG = 0;
                }

                if (!string.IsNullOrEmpty(substringsChar[j]) && (substringsChar[j].Length > 5))
                {
                    char[] delimiterChars = { ' ', '\t' };

                    String[] substringsCharG = substringsChar[j].Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                    lineAllG = substringsChar[j];

                    if (countLineG == 2) BarCode = substringsCharG[2] != null ? substringsCharG[2] : "n/a";

                    //Console.ForegroundColor = ConsoleColor.Yellow;
                    //Console.WriteLine($"{DateTime.Now} [{countLineG}] - {substringsChar[j]}");
                    //Console.ForegroundColor = ConsoleColor.White;
                    if (BarCode != null)
                    if (IsExistBarCode(BarCode)) CreateModelG(lineAllG); //!!!
                }
            }

            Console.ReadLine();
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

                for (int i = 0; i < substringsCharAll.Length; i++)
                {
                    //if (substringsCharAll[0].InCont("WBC", "LYM", "NEU", "MON", "EOS", "BAS", "LY%", "NE%", "MO%", "EO%", "BA%", "RBC", "HGB", "HCT", "MCV", "MCH", "MCHC", "RDWc", "MCV", "PLT"))
                    if (Extensions.InCont_(substringsCharAll[0]))/*varCheck 2*/
                    {
                        Console.Write($"[{i}] {substringsCharAll[i]}, ");

                        model = new Model(type: substringsCharAll[0], code: BarCode, goods: substringsCharAll[0], typeGoods: substringsCharAll[3], value01: substringsCharAll[1], value02: substringsCharAll[2]/*, value03: substringsCharAll[6], value04: substringsCharAll[7]*/);
                    }
                }
                Console.WriteLine();

                if (model.Type != null)/*varCheck 2*/
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"\n[nah&&&& - 0]{model.Type} [barCode1]{model.Code} [type ? - 2]{model.Goods} [nah??????3]{model.TypeGoods} [value01 - 4]{model.Value01} [value02 - 5]{model.Value02}");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                // string query = QueryGetGem(model)?.Query; //!!!
                string query = QueryGet(model)?.Query; //!!!

                if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBd_(model); //!!! ver2 потом включить

                if (!string.IsNullOrEmpty(query) && query != "Query NULL")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(query);
                    //   Log.Write(query);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("query NULL - " + query);
                    //Log.Write(query);
                    Console.ForegroundColor = ConsoleColor.White;
                }

                //if (!string.IsNullOrEmpty(query)) UpdateRowBd_(model); //!!! ver2

            }
        }

        /// <summary>
        /// асинхронный метод запускающий асинхронно метод изменения данных в бд
        /// </summary>
        /// <param name="query"></param>
        static async void UpdateRowBd_(Model query)
        {
            //Console.WriteLine("Начало метода async");
            await Task.Run(() => UpdateRowBd(query)); // выполняется асинхронно
                                                      // Console.WriteLine("Конец метода async");
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

            return conn;
        }

        /// <summary>
        /// внесение данных в бд
        /// </summary>
        /// <param name="query"></param>
        private static void UpdateRowBd(Model query)
        {
            //Console.WriteLine($"UpdateRowBd");
            //Log.Write($"UpdateRowBd)");

            //Console.WriteLine($"UpdateRowBd - {QueryGet(query).Query}");
            //Log.Write($"UpdateRowBd - {QueryGet(query).Query}");

            Thread.Sleep(100);

            FbConnection conn = GetConnection();
            FbTransaction fbt = conn.BeginTransaction();
            try
            {
                //Console.WriteLine(query.Query);

                //string queryBd = QueryGetGem(query).Query;
                string queryBd = query.Query;



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
                    Log.Write($"\n{DateTime.Now} Update count: {res}, BarCode: {query.Code}, TypeGoods: {query.Goods}, TypeGoods: {query.TypeGoods}, Value1: {query.Value01}, Value2: {query.Value02}");

                    fbt.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Log.Write_ex(ex);
                Console.ForegroundColor = ConsoleColor.White;
                fbt.Rollback();
            }
            finally
            {
                // fbt.Commit();
                conn.Close();
            }
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
                //  Console.WriteLine($"BarCode:{getBarCodeCorrect(query.Code)}, Value {query.Value01}");
                Console.Beep(frequency, duration);
            }
            Console.WriteLine();
        }

        public static string RetLine(string line)
        {
            bool pr = false;
            string lineAll = null;


            if (line.IndexOf("Serial No.:") != 0)
            {

                // lineAll = null;
                countLine++;
            }
            else
            {
                countLine = 0;
            }

            if (!string.IsNullOrEmpty(line) && (line.Length > 5))
            {
                char[] delimiterChars = { ' ', '\t' };

                String[] substringsChar = line.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < substringsChar.Length; j++)
                {
                    lineAll += $"-[{j}][{substringsChar[j]}]";
                }

                Console.WriteLine($"[{countLine}] - lineAll: {lineAll}");
            }

            return null;
        }


        private static QueryModel QueryGet(Model model)
        {
            QueryModel query = null;

            if (Extensions.InCont_(model.Goods))/*varCheck 2*/
            {


                if (IsFloat(model.Value01))
                {
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                     "d.result = '" + model.Value01 + "', \n" +
              "d.result_text = '" + model.Value01 + "', \n" +
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
              "('" + model.Goods + "') as MIDDLE_NAME))  \n" +
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
                     "d.result = '" + model.Value02 + "', \n" +
              "d.result_text = '" + model.Value02 + "', \n" +
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
              "('" + model.Goods + "') as MIDDLE_NAME))  \n" +
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

        //private static QueryModel QueryGetGem(Model model)
        //{
        //    QueryModel query = null;

        //    switch (model.Goods)
        //    {
        //        case "WBC": //WBC
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + model.Value01 + "', \n" +
        //          "d.result_text = '" + model.Value01 + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('WBC') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + model.Value02 + "', \n" +
        //          "d.result_text = '" + model.Value02 + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('WBC') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "LY%": //LY%
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.result_text = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('LYM%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });
        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                "d.result = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //         "d.result_text = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //         "d.hardware_date_updated = current_timestamp, " +
        //         "d.hardware_info = ('GEMATOL') \n" +
        //         "where ID = (select R.ID  \n" +
        //         "from JOR_CHECKS_DT D  \n" +
        //         "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //         "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //         "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //         "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //          "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //         "and (R.CODE_NAME = cast(  \n" +
        //         "('LYM%') as MIDDLE_NAME))  \n" +
        //         "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "NE%": //NE%
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.result_text = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('NE%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //          "d.result_text = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('NE%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "MO%": //MO%
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.result_text = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('MON%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                     "d.result = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //              "d.result_text = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //              "d.hardware_date_updated = current_timestamp, " +
        //              "d.hardware_info = ('GEMATOL') \n" +
        //              "where ID = (select R.ID  \n" +
        //              "from JOR_CHECKS_DT D  \n" +
        //              "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //              "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //              "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //              "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //               "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //              "and (R.CODE_NAME = cast(  \n" +
        //              "('MON%') as MIDDLE_NAME))  \n" +
        //              "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "EO%": //EO%
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.result_text = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('EOZ%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }

        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //             "d.result = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //           "d.result_text = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //           "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //           "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //       "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('EOZ%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "BA%": //BA%
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.result_text = '" + Math.Round(ParseFloat(model.Value01)) + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('BAZ%') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                    "d.result = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //             "d.result_text = '" + Math.Round(ParseFloat(model.Value02)) + "', \n" +
        //             "d.hardware_date_updated = current_timestamp, " +
        //             "d.hardware_info = ('GEMATOL') \n" +
        //             "where ID = (select R.ID  \n" +
        //             "from JOR_CHECKS_DT D  \n" +
        //             "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //             "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //             "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //             "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //              "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //             "and (R.CODE_NAME = cast(  \n" +
        //             "('BAZ%') as MIDDLE_NAME))  \n" +
        //             "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "RBC": //RBC
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                     "d.result = '" + model.Value01 + "', \n" +
        //              "d.result_text = '" + model.Value01 + "', \n" +
        //              "d.hardware_date_updated = current_timestamp, " +
        //              "d.hardware_info = ('GEMATOL') \n" +
        //              "where ID = (select R.ID  \n" +
        //              "from JOR_CHECKS_DT D  \n" +
        //              "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //              "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //              "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //              "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //               "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //              "and (R.CODE_NAME = cast(  \n" +
        //              "('RBC') as MIDDLE_NAME))  \n" +
        //              "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                     "d.result = '" + model.Value02 + "', \n" +
        //              "d.result_text = '" + model.Value02 + "', \n" +
        //              "d.hardware_date_updated = current_timestamp, " +
        //              "d.hardware_info = ('GEMATOL') \n" +
        //              "where ID = (select R.ID  \n" +
        //              "from JOR_CHECKS_DT D  \n" +
        //              "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //              "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //              "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //              "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //               "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //              "and (R.CODE_NAME = cast(  \n" +
        //              "('RBC') as MIDDLE_NAME))  \n" +
        //              "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        case "PLT": //PLT
        //            if (IsFloat(model.Value01))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                 "d.result = '" + model.Value01 + "', \n" +
        //          "d.result_text = '" + model.Value01 + "', \n" +
        //          "d.hardware_date_updated = current_timestamp, " +
        //          "d.hardware_info = ('GEMATOL') \n" +
        //          "where ID = (select R.ID  \n" +
        //          "from JOR_CHECKS_DT D  \n" +
        //          "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //          "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //          "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //          "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //           "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //          "and (R.CODE_NAME = cast(  \n" +
        //          "('PLT') as MIDDLE_NAME))  \n" +
        //          "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });

        //                model.Query = query.Query;
        //            }
        //            else if (IsFloat(model.Value02))
        //            {
        //                query = (new QueryModel()
        //                {
        //                    Type = model.Type,
        //                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
        //                                         "d.result = '" + model.Value02 + "', \n" +
        //                                  "d.result_text = '" + model.Value02 + "', \n" +
        //                                  "d.hardware_date_updated = current_timestamp, " +
        //                                  "d.hardware_info = ('GEMATOL') \n" +
        //                                  "where ID = (select R.ID  \n" +
        //                                  "from JOR_CHECKS_DT D  \n" +
        //                                  "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
        //                                  "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
        //                                  "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
        //                                  "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
        //                                   "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
        //                                  "and (R.CODE_NAME = cast(  \n" +
        //                                  "('PLT') as MIDDLE_NAME))  \n" +
        //                                  "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
        //                });
        //                model.Query = query.Query;
        //            }
        //            else
        //            {
        //                query = (new QueryModel()
        //                { Type = "Query NULL", Query = "Query NULL" });
        //            }
        //            break;
        //        default:
        //            query = (new QueryModel()
        //            { Type = "Query NULL", Query = "Query NULL" });
        //            break;
        //    }
        //    return query;
        //}

        private static bool IsFloat(string val)
        {
            //return Double.TryParse(val, out _);
            return float.TryParse(val, NumberStyles.Any, new CultureInfo("en-US"), out _);

        }

        private static bool IsExistBarCode(string barCode)
        {
            Boolean pr = false;
            string id = null;
            //Console.WriteLine($"UpdateRowBd");
            //Log.Write($"UpdateRowBd)");

            //Console.WriteLine($"UpdateRowBd - {QueryGet(query).Query}");
            //Log.Write($"UpdateRowBd - {QueryGet(query).Query}");

            Thread.Sleep(100);

            FbConnection conn = GetConnection();
            FbTransaction fbt = conn.BeginTransaction();
            try
            {
                //Console.WriteLine(query.Query);

                //string queryBd = QueryGetGem(query).Query;
                string queryBd = "select first 1 R.ID " +
                "from JOR_CHECKS_DT D " +
                "inner join JOR_CHECKS C on C.ID = D.HD_ID " +
                "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID " +
                "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID " +
                "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0) " +
                "and(D.BULB_NUM_CODE = cast('" + barCode + "' as NAME)) " +
                //"and(R.CODE_NAME = cast(('PLT') as MIDDLE_NAME)) " +
                "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1))";



                using (FbCommand cmd = new FbCommand(queryBd, conn))
                {
                    cmd.Transaction = fbt;

                    FbDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows) // если есть данные
                    {
                        // выводим названия столбцов
                        // Console.WriteLine("{0}\t{1}\t{2}", reader.GetName(0), reader.GetName(1), reader.GetName(2));

                        while (reader.Read()) // построчно считываем данные
                        {
                            id = reader.GetString(0);
                        }


                        if (!String.IsNullOrWhiteSpace(id))
                        {
                            pr = true;
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"Exists BarCode - {id}");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else pr = false;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Not exixsts BarCode");
                        Console.ForegroundColor = ConsoleColor.White;
                        pr = false;

                    }


                    //fbt.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Log.Write_ex(ex);
                Console.ForegroundColor = ConsoleColor.White;
                fbt.Rollback();
            }
            finally
            {
                fbt.Commit();
                conn.Close();
            }

            return pr;

        }

        private static float ParseFloat(string val)
        {
            //return Double.TryParse(val, out _);
            return float.Parse(val, NumberStyles.Any, new CultureInfo("en-US"));

        }

    }
}
