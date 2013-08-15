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
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Text;
using System.Threading;
using System.Xml;

namespace Test_Endpoint
{
    public class SimpleServer : IDisposable
    {
        private HttpServer Server;

        public string EndPoint
        {
            get { return string.Format("{0}:{1}", Host, Server.Port); }
        }

        private string Host;

        /// <summary>
        /// If ShouldFail = true, then server will return 500
        /// </summary>
        public bool ShouldFail { get; private set; }

        public string RecievedData
        {
            get
            {
                lock (Received)
                {
                    return Received.ToString();
                }
            }
        }

        private StringBuilder Received = new StringBuilder(500);

        /// <summary>
        /// Logs the given string to a file.
        /// </summary>
        /// <param name="toLog">The message to log.</param>
        /// <param name="id"></param>
        private void Log(string toLog, string id)
        {
            try
            {
                using (var writer = new StreamWriter(string.Format("{0}\\{1}.txt",AppDomain.CurrentDomain.BaseDirectory, id), true))
                {
                    Console.WriteLine("Recieved message {0}", id);
                    writer.WriteLine(toLog);
                }
            }
            catch
            {
            }
        }

        public List<KeyValuePair<string, string>> Headers { get; private set; }

        /// <summary>
        /// Constructs an HTTP server and binds it to the given
        /// endpoint.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="shouldFail"></param>
        public SimpleServer(string host, int port, bool shouldFail)
        {
            try
            {
                Host = host;

                Server = new HttpServer(host, port);
                Headers = new List<KeyValuePair<string, string>>();
                ShouldFail = shouldFail;

                if (!Server.Running)
                {
                    lock (Server)
                    {
                        if (!Server.Running)
                        {
                            var t = new Thread(() => Server.Start());
                            t.Start();
                            Server.Started.WaitOne();

                            if (!Server.Running)
                            {
                                Console.WriteLine("Unable to start server.");
                                Environment.Exit(1);
                            }
                        }
                    }
                }

                Server.Handlers.TryAdd("/", r =>
                {
                    try
                    {
                        const int GUIDLength = 36;

                        // Parses the JSON data sent by the 1.0.1 Javascript API
                        if (r.Data.StartsWith("data="))
                        {
                            var result = HttpUtility.UrlDecode(r.Data);
                            result = result.Replace(" ", "");
                            Log(r.FullPost, result.Substring(result.IndexOf("id\":\"") + 5, GUIDLength));
                        }
                        else
                        {
                            var doc = new XmlDocument();
                            doc.LoadXml(r.Data);

                            string id = "";

                            // looks for a message tag with an id attribute sent by the API (e.g. the .NET API)
                            var node = doc.SelectSingleNode("messages[@id]");

                            if (node != null)
                            {
                                id = node.Attributes["id"].Value;
                            }
                            else
                            {
                                // looks for an Id tag sent by the API (e.g. the SOAP message sent by the injected API)
                                var nodes = doc.SelectNodes("//*");

                                if (nodes != null)
                                {
                                    foreach (var n in nodes)
                                    {
                                        var no = n as XmlNode;
                                        if (no != null)
                                        {
                                            if (no.Name == "Id")
                                            {
                                                id = no.InnerText;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            Log(r.FullPost, id);
                        }
                    }
                    catch
                    {
                        
                    }
                    
                    return ShouldFail ? 500 : 204;
                });
            }
            catch
            {
                Console.WriteLine("Unable to start server.");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Terminates this HTTP server.
        /// </summary>
        public void Dispose()
        {
            Func<HttpRequest, int> trash;
            Server.Handlers.TryRemove("/", out trash);
        }
    }
}
