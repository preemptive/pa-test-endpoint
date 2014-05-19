// Copyright (c) 2013 PreEmptive Solutions; All Right Reserved, http://www.preemptive.com/
//
// This source is subject to the Microsoft Public License (MS-PL).
// Please see the License.txt file for more information.
// All other rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using Nito.AsyncEx;
using System;
using System.Linq;

namespace Test_Endpoint
{
    public class Program
    {
        private const int minPort = 0;
        private const int maxPort = 65535;
        private const int defaultListenersPerCPU = 4;

        public static void Main(string[] args)
        {
            //required to make async work from a console app
            AsyncContext.Run(() => mainAsync(args));
        }

        private static void mainAsync(string[] args)
        {
            if (args.Any(x => x == "/?"))
            {
                Console.WriteLine("Starts up a test endpoint for debugging. Defaults to localhost:8080.");
                Console.WriteLine();
                Console.WriteLine("ENDPOINT [/p:portnum] [/l:listeners] [/f]");
                Console.WriteLine();
                Console.WriteLine("/p:portnum  \t Specifies the port number to use.");
                Console.WriteLine("/l:listeners\t Specifies the number of connection listeners (default {0} per CPU)", defaultListenersPerCPU);
                Console.WriteLine("/f          \t Causes the endpoint to always return the 500 network response code.");
                
                return;
            }

            int port = 8080;

            if (args.Any(x => x.StartsWith("/p:")))
            {
                if (!int.TryParse(args.First(x => x.StartsWith("/p:")).Split(':')[1], out port) || port < minPort || port > maxPort)
                {
                    Console.WriteLine("Invalid port specified.");
                    return;
                }
            }

            int listeners = defaultListenersPerCPU * Environment.ProcessorCount;
            if (args.Any(x => x.StartsWith("/l:")))
            {
                if (!int.TryParse(args.First(x => x.StartsWith("/l:")).Split(':')[1], out listeners) || listeners < 1)
                {
                    Console.WriteLine("Invalid number of listeners specified.");
                    return;
                }
            }

            bool fail = args.Contains("/f");

            try
            {
                new SimpleServer(port, listeners, fail).Start();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to start server. (Is the port in use?)");
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }
            Console.WriteLine("Listening on port {0}", port);
        }
    }
}
