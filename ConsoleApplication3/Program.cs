using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO.Ports;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{

    class Program
    {
        static SerialPort mySerialPort = new SerialPort(GetValueIni("CC-4000", "COM_Port") ?? "COM3");
        public static string path_db;
        public static int countLine = 0;

        public static ArrayList listGoods = new ArrayList();
        public static string lineAll = null;

        public static void Main(string[] args)
        {
            First_Load();

            Console.WriteLine($"connection bd - {(CheckDbConnection() == true ? "open" : "closed")}");
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

                Console.WriteLine(ex.Message);
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

                Log.Write("start program");

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
            //mySerialPort.Handshake = Handshake.RequestToSend;
            //mySerialPort.DtrEnable = true;
            //mySerialPort.RtsEnable = true;
            mySerialPort.NewLine = System.Environment.NewLine;
            // mySerialPort.Open();
            try { 
                mySerialPort.Open(); 
            }
            catch {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Port closed");
                Console.ForegroundColor = ConsoleColor.White;
            }
            System.Threading.Thread.Sleep(500);


            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            Console.WriteLine("Press any key to continue...");
            Console.WriteLine();

            Console.ReadKey();

            mySerialPort.Close();
        }

        private static void DataReceivedHandler(
                            object sender,
                            SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            Console.Write($"{indata}");
            Log.Write(indata);


            Console.ForegroundColor = ConsoleColor.Red;
            workWithDataCom(indata);
            Console.ForegroundColor = ConsoleColor.White;



            //if (indata.IndexOf("HumaClot") > -1)
            //{
            //mySerialPort.WriteLine("AT+CMGL=\"ALL\"");
            //mySerialPort.WriteLine("---");
            //System.Threading.Thread.Sleep(500);
            //while (mySerialPort.BytesToRead > 0)
            //{
            //    try
            //    {
            //        Console.WriteLine(mySerialPort.ReadLine());
            //        //  Console.ForegroundColor = ConsoleColor.Red;
            //        // workWithDataCom(mySerialPort.ReadLine());
            //        // Console.ForegroundColor = ConsoleColor.White;
            //        Log.Write(mySerialPort.ReadLine());
            //        //Beep(3); // потом вернуть
            //    }
            //    catch { }
            //}
            //}


        }

        private static void workWithDataCom(string line)
        {
            //String[] substringsLine = line.Split('\n');

            //for (int i = 0; i < substringsLine.Length; i++)
            //{
            //    Console.WriteLine($"{i}:{substringsLine[i]}");
            //}
            bool pr = false;

            Console.WriteLine();

            if (line.IndexOf("CC-3003") == 0)
            {
                countLine = 0;
                lineAll = null;
            }
            else countLine++;

            String[] substringsChar = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < substringsChar.Length; j++)
            {
                if (substringsChar[1] != "BEGIN") pr = true;
                else pr = false;
            }

            if (pr) lineAll += line;
            else lineAll = null;

            if (!string.IsNullOrEmpty(lineAll) && (countLine != 0))
            {
                String[] substringsCharAll = lineAll.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < substringsCharAll.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"[{i}] {substringsCharAll[i]}  ");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                
                //создание и передача модели в бд
                Model model = new Model(type: substringsCharAll[0], code: substringsCharAll[1], goods: substringsCharAll[2], typeGoods: substringsCharAll[3], value01: substringsCharAll[4], value02: substringsCharAll[5], value03: substringsCharAll[6], value04: substringsCharAll[7]);
               // Console.WriteLine(QueryGet(model).Query);
                UpdateRowBd(QueryGet(model).Query);
            }
            Console.WriteLine();

        }

        private static void UpdateRowBd(string query)
        {
            FbConnection conn = GetConnection();
            FbTransaction fbt = conn.BeginTransaction();
            try
            {
                using (FbCommand cmd = new FbCommand(query, conn))
                {
                    cmd.Transaction = fbt;
                    int res = cmd.ExecuteNonQuery();

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Update {res}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Log.Write(query);
                    //fbt.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
                fbt.Rollback();
            }
            finally
            {
                fbt.Commit();
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

            Console.WriteLine($"{query}");
        }

        private static QueryModel QueryGet(Model model)
        {
            QueryModel query;

            switch (model.Goods)
            {
                case "1APTT":
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
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
                      // "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                      "and (D.BULB_NUM_CODE = cast('11545026' as NAME))  \n" +
                      "and (R.CODE_NAME = cast(  \n" +
                      "('APTT_t') as MIDDLE_NAME))  \n" +
                      "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });
                    break;
                case "Thrombin_T.":
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                      "d.result_text = '" + model.Value01 + "', \n" +
                      "d.hardware_date_updated = current_timestamp, " +
                      "d.hardware_info = ('CC-4000') \n" +
                      "where ID = (select R.ID  \n" +
                      "from JOR_CHECKS_DT D  \n" +
                      "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                      "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                      "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                      "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                      "and (D.BULB_NUM_CODE_ID = cast('" + model.Code + "' as NAME))  \n" +
                      "and (R.CODE_NAME = cast(  \n" +
                      "('TT@s') as MIDDLE_NAME))  \n" +
                      "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });
                    break;
                case "PTT":
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                      "d.result_text = '" + model.Value01 + "', \n" +
                      "d.hardware_date_updated = current_timestamp, " +
                      "d.hardware_info = ('CC-4000') \n" +
                      "where ID = (select R.ID  \n" +
                      "from JOR_CHECKS_DT D  \n" +
                      "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                      "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                      "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                      "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                      "and (D.BULB_NUM_CODE_ID = cast('" + model.Code + "' as NAME))  \n" +
                      "and (R.CODE_NAME = cast(  \n" +
                      "('APTT@s') as MIDDLE_NAME))  \n" +
                      "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });
                    break;
                case "Fib._g/l":
                    query = (new QueryModel()
                    {
                        Type = model.Type,
                        Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                      "d.result_text = '" + model.Value01 + "', \n" +
                      "d.hardware_date_updated = current_timestamp, " +
                      "d.hardware_info = ('CC-4000') \n" +
                      "where ID = (select R.ID  \n" +
                      "from JOR_CHECKS_DT D  \n" +
                      "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                      "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                      "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                      "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                      "and (D.BULB_NUM_CODE_ID = cast('" + model.Code + "' as NAME))  \n" +
                      "and (R.CODE_NAME = cast(  \n" +
                      "('FIBRINOGEN@G') as MIDDLE_NAME))  \n" +
                      "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                    });
                    break;
                default:
                    query = (new QueryModel()
                    { Type = "", Query = "" });
                    break;
            }
            return query;
        }

        private static void Beep(int count)
        {
            int x = count;

            // Set the Frequency 
            int frequency = 800;

            // Set the Duration 
            int duration = 200;
            for (int i = 1; i <= x; i++)
            {
                Console.Write("Beep number {0}. ", i);
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