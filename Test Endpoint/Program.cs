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
            bool noWrite = false;
            int slow = 0;

            var options = new OptionSet()
            {
                { "h|?", v => DieUsage(0) },
                { "p=", portString => port = ParsePort(portString) },
                { "l=", listenersString => listeners = ParseListeners(listenersString) },
                { "f", flag => fail = (flag != null) },
                { "nowrite", flag => noWrite = (flag != null) },
                { "slow=", slowString => slow = ParseSlow(slowString) },
                { "perf", flag => perf = (flag != null) },
            };

            List<string> extraArgs = options.Parse(args);
            if (extraArgs.Count != 0)
            {
                DieUsage(1);
            }

            //required to make async work from a console app
            Task.WaitAll(MainAsync(port, listeners, fail, noWrite, slow, perf));
        }

        private static void DieUsage(int exitCode)
        {
            var stream = Console.Out;
            if (exitCode != 0)
            {
                stream = Console.Error;
            }
            
            stream.WriteLine("USAGE:");
            stream.WriteLine("endpoint.exe [/h] [/p:portnum] [/l:listeners] [/f] [/slow:secs] [/nowrite] [/perf]");
            stream.WriteLine();
            stream.WriteLine("/h          \t Prints this message.");
            stream.WriteLine("/p:portnum  \t Port number to listen on (default {0}).", DEFAULT_PORT);
            stream.WriteLine("/l:listeners\t Number of connection listeners (default {0} per CPU).", DEFAULT_LISTENERS_PER_CPU);
            stream.WriteLine("/f          \t Always return the 500 network response code.");
            stream.WriteLine("/slow:secs  \t Wait <secs> seconds before each response to sender.");
            stream.WriteLine("/nowrite    \t Don't save incoming envelopes (or check for duplicates).");
            stream.WriteLine("/perf       \t Various changes to allow high throughput.");

            Environment.Exit(exitCode);
        }

        static int ParsePort(string portString)
        {
            int port;
            if (!int.TryParse(portString, out port) || port < MIN_PORT || port > MAX_PORT)
            {
                Console.Error.WriteLine("Invalid port specified.");
                Environment.Exit(1);
            }

            return port;
        }

        static int ParseListeners(string listenersString)
        {
            int listeners;
            if (!int.TryParse(listenersString, out listeners) || listeners < 1)
            {
                Console.Error.WriteLine("Invalid number of listeners specified.");
                Environment.Exit(1);
            }

            return listeners;
        }

        private static int ParseSlow(string slowString)
        {
            int slow;
            if (!int.TryParse(slowString, out slow) || slow < 0)
            {
                Console.Error.WriteLine("Invalid slow value.");
                Environment.Exit(1);
            }

            return slow;
        }

        private async static Task<bool> MainAsync(int port, int listeners, bool fail, bool noWrite, int slow, bool perf)
        {
            try
            {
                //have to await so we can catch port-binding exceptions
                await new SimpleServer(port, listeners, fail, noWrite, slow, perf).Start();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Unable to start the server. Verify that the account has permissions to listen on the port, and that the port is not already in use.");
                Console.Error.WriteLine();
                Console.Error.WriteLine("For example, as an adminstrator, use this netsh command to allow non-adminstrators to listen on the port:");
                Console.Error.WriteLine( "\tnetsh http add urlacl url=http://+:[your port]/ user=[account domain]\\[account username]");
                Environment.Exit(1);
            }

            return true;
        }
    }
}
