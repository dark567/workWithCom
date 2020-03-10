using System;

namespace Coagulometr
{
    public class LogicC
    {
        private static void workWithDataComAsyncCoagulometr(string line)
        {
            bool pr = false;
            int countLineC = 0;
            string lineAllC = null;
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

                lineAllC = line;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{DateTime.Now} [{countLineC}] - {lineAllC}");
                Console.ForegroundColor = ConsoleColor.White;

                Log.Write_res($"[{countLineC}] - {lineAllC}");

                CreateModelC(lineAllC); //!!!
            }

        }

        private static void CreateModelC(string lineAllC)
        {
            throw new NotImplementedException();
        }
    }
}
