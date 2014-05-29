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

using Mono.Options;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test_Endpoint
{
    public class Program
    {
        private const int minPort = 0;
        private const int maxPort = 65535;
        private const int defaultPort = 8080;
        private const int defaultListenersPerCPU = 4;

        public static void Main(string[] args)
        {
            int port = defaultPort;
            int listeners = defaultListenersPerCPU * Environment.ProcessorCount; ;
            bool fail = false;
            bool perf = false;

            OptionSet options = new OptionSet()
            {
                { "h|?", v => dieUsage(0) },
                { "p=", portString => port = parsePort(portString) },
                { "l=", listenersString => listeners = parseListeners(listenersString) },
                { "f", flag => fail = (flag != null) },
                { "perf", flag => perf = (flag != null) },
            };

            List<string> extraArgs = options.Parse(args);
            if (extraArgs.Count != 0)
            {
                dieUsage(1);
            }

            //required to make async work from a console app
            AsyncContext.Run(() => mainAsync(port, listeners, fail, perf));
        }

        private static void dieUsage(int exitCode)
        {
            var stream = Console.Out;
            if (exitCode != 0)
            {
                stream = Console.Error;
            }
            
            stream.WriteLine("USAGE:");
            stream.WriteLine("endpoint.exe [/h] [/p:portnum] [/l:listeners] [/f]");
            stream.WriteLine();
            stream.WriteLine("/h          \t Prints this message.");
            stream.WriteLine("/p:portnum  \t Specifies the port number to use (default {0}).", defaultPort);
            stream.WriteLine("/l:listeners\t Specifies the number of connection listeners (default {0} per CPU)", defaultListenersPerCPU);
            stream.WriteLine("/f          \t Causes the endpoint to always return the 500 network response code.");

            System.Environment.Exit(exitCode);
        }

        static int parsePort(string portString)
        {
            int port = -1;
            if (!int.TryParse(portString, out port) || port < minPort || port > maxPort)
            {
                Console.WriteLine("Invalid port specified.");
                System.Environment.Exit(1);
            }
            return port;
        }

        static int parseListeners(string listenersString)
        {
            int listeners = -1;
            if (!int.TryParse(listenersString, out listeners) || listeners < 1)
            {
                Console.WriteLine("Invalid number of listeners specified.");
                System.Environment.Exit(1);
            }
            return listeners;
        }

        private async static void mainAsync(int port, int listeners, bool fail, bool perf)
        {
            try
            {
                //have to await so we can catch port-binding exceptions
                await new SimpleServer(port, listeners, fail, perf).Start();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Unable to start server. Is the port in use?");
                Environment.Exit(1);
            }
        }
    }
}
