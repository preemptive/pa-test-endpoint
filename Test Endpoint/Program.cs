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

using System;
using System.Linq;

namespace Test_Endpoint
{
    public class Program
    {
        private const int minPort = 0;
        private const int maxPort = 65535;
        public static void Main(string[] args)
        {
            if (args.Any(x => x == "/?"))
            {
                Console.WriteLine("Starts up a test endpoint for debugging. Defaults to localhost:8080.");
                Console.WriteLine();
                Console.WriteLine("ENDPOINT [/p:portnum] [/h:hostname] [/f]");
                Console.WriteLine();
                Console.WriteLine("/h:hostname \t Specifies the hostname for the endpoint to use.");
                Console.WriteLine("/p:portnum  \t Specifies the port number for the endpoint to use.");
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
            
            string host = args.Any(x => x.StartsWith("/h:")) ? args.First(x => x.StartsWith("/h:")).Split(':')[1] : "localhost";
            bool fail = args.Contains("/f");

            var ss = new SimpleServer(host, port, fail);
            Console.WriteLine("Listening on {0}", ss.EndPoint);
        }
    }
}
