// Copyright (c) 2014 PreEmptive Solutions; All Right Reserved, http://www.preemptive.com/
//
// This source is subject to the Microsoft Public License (MS-PL).
// Please see the License.txt file for more information.
// All other rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using System.Threading.Tasks;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace Test_Endpoint
{
    public class Program
    {
        private const int MIN_PORT = 0;
        private const int MAX_PORT = 65535;
        private const int DEFAULT_PORT = 8080;
        private const int DEFAULT_LISTENERS_PER_CPU = 4;

        public static void Main(string[] args)
        {
            int port = DEFAULT_PORT;
            int listeners = DEFAULT_LISTENERS_PER_CPU * Environment.ProcessorCount;
            bool fail = false;
            bool perf = false;

            var options = new OptionSet()
            {
                { "h|?", v => DieUsage(0) },
                { "p=", portString => port = ParsePort(portString) },
                { "l=", listenersString => listeners = ParseListeners(listenersString) },
                { "f", flag => fail = (flag != null) },
                { "perf", flag => perf = (flag != null) },
            };

            List<string> extraArgs = options.Parse(args);
            if (extraArgs.Count != 0)
            {
                DieUsage(1);
            }

            //required to make async work from a console app
            Task.WaitAll(MainAsync(port, listeners, fail, perf));
        }

        private static void DieUsage(int exitCode)
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
            stream.WriteLine("/p:portnum  \t Specifies the port number to use (default {0}).", DEFAULT_PORT);
            stream.WriteLine("/l:listeners\t Specifies the number of connection listeners (default {0} per CPU)", DEFAULT_LISTENERS_PER_CPU);
            stream.WriteLine("/f          \t Causes the endpoint to always return the 500 network response code.");

            Environment.Exit(exitCode);
        }

        static int ParsePort(string portString)
        {
            int port;
            if (!int.TryParse(portString, out port) || port < MIN_PORT || port > MAX_PORT)
            {
                Console.WriteLine("Invalid port specified.");
                Environment.Exit(1);
            }

            return port;
        }

        static int ParseListeners(string listenersString)
        {
            int listeners;
            if (!int.TryParse(listenersString, out listeners) || listeners < 1)
            {
                Console.WriteLine("Invalid number of listeners specified.");
                Environment.Exit(1);
            }

            return listeners;
        }

        private async static Task<bool> MainAsync(int port, int listeners, bool fail, bool perf)
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

            return true;
        }
    }
}
