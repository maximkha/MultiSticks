using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;

namespace mSticks
{
    class Program
    {
        static string connectionString = "";
        const string jsonblob = "https://jsonblob.com/api/jsonBlob";
        static HttpClient httpClient = new HttpClient();
        static bool isHost = false;
        static bool isFirst = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Multiplayer Sticks");
            Console.WriteLine("Copyright (c) 2019 Maxim Khanov. All rights reserved.");
            char c = charMenu("Connect or Host? (c/h)", 'c', 'h');
            if (c == 'h')
            {
                //Host decides who goes first
                char fc = charMenu("You or Opponent gets first move? (y/o)", 'y', 'o');
                string str = "S 0 1";
                if (fc == 'y')
                {
                    //S (System) 0 (System message type: first player) <0 or 1, 0 = host, 1 = other> 
                    str = "S 0 0";
                }
                connectionString = createJsonBlob(jsonblob, str);
                Console.WriteLine("Link {0}", connectionString);
                isHost = true;
                game();
                return;
            }
            else
            {
                while(true)
                {
                    Console.WriteLine("Paste Game ID>");
                    connectionString = Console.ReadLine();
                    string s = getJsonBlob(jsonblob + "/" + connectionString);
                    if (s == "S 0 1")
                    {
                        isFirst = true;
                        break;
                    }
                    else if (s == "S 0 0")
                    {
                        isFirst = false;
                        break;
                    }
                    Console.WriteLine("Invalid Game ID");
                }
                game();
                return;
            }
            //string str = createJsonBlob(jsonblob, "test");
            //Console.WriteLine(str);
            //str = getJsonBlob(jsonblob + "/" + str);
            //Console.WriteLine(str);
            //Console.ReadLine();
        }

        static void game()
        {
            int a = 1, b = 1, p = 1, q = 1;
            if (isFirst)
            {
                userDisplay(a, b, p, q);
                int[] sr = localTurn();
                putJsonBlob(jsonblob + "/" + connectionString, "G " + (isHost ? "0" : "1") + " " + sr[0] + " " + sr[1]);
                int[] npq = doMove(a, b, p, q, sr[0], sr[1]);
                p = npq[0];
                q = npq[1];
            }

            while(true)
            {
                userDisplay(a, b, p, q);
                int[] sr = remoteTurn();
                int[] nab = doMove(p, q, a, b, sr[0], sr[1]);
                a = nab[0];
                b = nab[1];
                if (gameEnd(a, b, p, q) != 0) break;
                userDisplay(a, b, p, q);
                sr = localTurn();
                putJsonBlob(jsonblob + "/" + connectionString, "G " + (isHost ? "0" : "1") + " " + sr[0] + " " + sr[1]);
                int[] npq = doMove(a, b, p, q, sr[0], sr[1]);
                p = npq[0];
                q = npq[1];
                if (gameEnd(a, b, p, q) != 0) break;
            }

            if (gameEnd(a, b, p, q) == -1) Console.WriteLine("Player2 won");
            if (gameEnd(a, b, p, q) == 1) Console.WriteLine("You won");
            Console.ReadLine();
        }

        static int[] remoteTurn()
        {
            while (true)
            {
                Thread.Sleep(1000);
                string str = getJsonBlob(jsonblob + "/" + connectionString);
                if (str.Length != 7) continue;
                if (str[0] == 'G')
                {
                    if ((isHost ? '0' : '1') == str[2]) continue;
                    char[] options = new char[] { '0', '1' };
                    return new int[] { Array.IndexOf(options, str[4]), Array.IndexOf(options, str[6]) };
                }
            }
        }

        static int[] localTurn()
        {
            while(true)
            {
                Console.Write("move>");
                string str = Console.ReadLine();
                //string[] strs = str.Split(',');
                if (str.Length != 2)
                {
                    Console.WriteLine("Invalid move");
                    continue;
                }
                string[] opts = { "1", "2" };
                if (!opts.Contains(str[0] + ""))
                {
                    Console.WriteLine("Invalid move");
                    continue;
                }
                if (!opts.Contains(str[1] + ""))
                {
                    Console.WriteLine("Invalid move");
                    continue;
                }
                return new int[] { Array.IndexOf(opts, str[0] + ""), Array.IndexOf(opts, str[1] + "") };
            }
        }

        static int gameEnd(int a, int b, int p, int q)
        {
            if (a == 0 && b == 0) return -1;
            if (p == 0 && q == 0) return 1;
            return 0;
        }

        public static int add(int a, int b)
        {
            return (a + b) % 5;
        }

        static void userDisplay(int a, int b, int p, int q, bool swap = false)
        {
            if (swap)
            {
                Console.WriteLine("Player2: {0}|{1}", a, b);
                Console.WriteLine("Player:  {0}|{1}", p, q);
                return;
            }
            Console.WriteLine("Player2: {0}|{1}", p, q);
            Console.WriteLine("Player:  {0}|{1}", a, b);
        }

        static int[] getUserMove(int a, int b, int p, int q)
        {
            Console.WriteLine("Your Move");
            userDisplay(a, b, p, q);
            while (true)
            {
                try
                {
                    int[] op = getUserInput();
                    return doMove(a, b, p, q, op[0], op[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static int[] doMove(int a, int b, int p, int q, int s, int d)
        {
            if (s == 0 && a == 0) throw new Exception("Invalid move");
            if (s == 1 && b == 0) throw new Exception("Invalid move");
            if (d == 0 && p == 0) throw new Exception("Invalid move");
            if (d == 1 && q == 0) throw new Exception("Invalid move");

            if (s == 0 && d == 0) return new int[] { add(a, p), q };
            else if (s == 1 && d == 0) return new int[] { add(b, p), q };
            else if (s == 0 && d == 1) return new int[] { p, add(a, q) };
            else if (s == 1 && d == 1) return new int[] { p, add(b, q) };
            else throw new Exception("Invalid Move");
        }

        public static int[] getUserInput()
        {
            Console.Write("move>");
            string str = Console.ReadLine();
            //string[] strs = str.Split(',');
            if (str.Length != 2)
            {
                throw new Exception("Invalid move");
            }
            string[] opts = { "1", "2" };
            if (!opts.Contains(str[0] + ""))
            {
                throw new Exception("Invalid move");
            }
            if (!opts.Contains(str[1] + ""))
            {
                throw new Exception("Invalid move");
            }
            return new int[] { Array.IndexOf(opts, str[0] + ""), Array.IndexOf(opts, str[1] + "") };
        }

        static string getJsonBlob(string url)
        {
            HttpResponseMessage httpResponse = httpClient.GetAsync(url).Result;
            if (!httpResponse.IsSuccessStatusCode) return "";//throw new Exception("Connection returned " + httpResponse.StatusCode);
            return httpResponse.Content.ReadAsStringAsync().Result.Replace("\"", "");
        }

        static void putJsonBlob(string url, string data)
        {
            HttpResponseMessage httpResponseMessage = httpClient.PutAsJsonAsync(url, data).Result;
            if (!httpResponseMessage.IsSuccessStatusCode) return;//throw new Exception("Connection returned " + httpResponseMessage.StatusCode);
            //return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        static string createJsonBlob(string url, string data)
        {
            HttpResponseMessage httpResponseMessage = httpClient.PostAsJsonAsync(url, data).Result;
            if (!httpResponseMessage.IsSuccessStatusCode) return "";//throw new Exception("Connection returned " + httpResponseMessage.StatusCode);
            return httpResponseMessage.Headers.Location.AbsolutePath;
        }

        //static string deleteJsonBlob(string url, string data)
        //{
        //    HttpResponseMessage httpResponseMessage = httpClient.PostAsJsonAsync(url, "").Result;
        //    if (!httpResponseMessage.IsSuccessStatusCode) throw new Exception("Connection returned " + httpResponseMessage.StatusCode);
        //    return httpResponseMessage.Content.ReadAsStringAsync().Result;
        //}

        static char charMenu(string question, params char[] options)
        {
            while (true)
            {
                Console.WriteLine(question);
                char k = (Console.ReadKey().KeyChar + "").ToLower()[0];
                Console.WriteLine("");
                if (options.Contains(k)) return k;
                Console.WriteLine("Invalid choice!");
            }
        }

        //static void End()
        //{

        //}

        //// https://msdn.microsoft.com/fr-fr/library/windows/desktop/ms686016.aspx
        //[DllImport("Kernel32")]
        //private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

        //// https://msdn.microsoft.com/fr-fr/library/windows/desktop/ms683242.aspx
        //private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        //private enum CtrlType
        //{
        //    CTRL_C_EVENT = 0,
        //    CTRL_BREAK_EVENT = 1,
        //    CTRL_CLOSE_EVENT = 2,
        //    CTRL_LOGOFF_EVENT = 5,
        //    CTRL_SHUTDOWN_EVENT = 6
        //}

        //private static bool Handler(CtrlType signal)
        //{
        //    switch (signal)
        //    {
        //        case CtrlType.CTRL_BREAK_EVENT:
        //        case CtrlType.CTRL_C_EVENT:
        //        case CtrlType.CTRL_LOGOFF_EVENT:
        //        case CtrlType.CTRL_SHUTDOWN_EVENT:
        //        case CtrlType.CTRL_CLOSE_EVENT:
        //            End();
        //            Environment.Exit(0);
        //            return false;

        //        default:
        //            return false;
        //    }
        //}
    }
}
