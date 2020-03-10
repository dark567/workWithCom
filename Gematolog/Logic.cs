using System;

namespace Gematolog
{
    public class Logic
    {
        private static void workWithDataComAsyncGematolog(string line)
        {
            bool pr = false;

            string lineAllG = null;
            int countLineG = 0;
            string BarCode = "n/a";

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

        private static void CreateModelG(string lineAllG)
        {
            throw new NotImplementedException();
        }
    }
}
