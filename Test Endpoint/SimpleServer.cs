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
using System.Net;
using System.Threading.Tasks;

#pragma warning disable 4014
namespace Test_Endpoint
{
    public class SimpleServer
    {
        private string Subdir = "received";

        private HttpListener Listener = new HttpListener();
        private int Port;
        private int ListenerCount;

        private bool AlwaysFail;
        private bool PerfMode;

        public SimpleServer(int port, int listenerCount, bool alwaysFail, bool perfMode)
        {
            Port = port;
            ListenerCount = listenerCount;
            Listener.Prefixes.Add(string.Format("http://+:{0}/", Port));

            AlwaysFail = alwaysFail;
            PerfMode = perfMode;

            Directory.CreateDirectory(Subdir);
        }

        public async Task Start()
        {
            Listener.Start();
            Console.WriteLine("Listening on port {0}", Port);

            var semaphore = new Semaphore(ListenerCount, ListenerCount);
            while (true)
            {
                semaphore.WaitOne();

                var context = await Listener.GetContextAsync();
                semaphore.Release();

                Task.Factory.StartNew(() => handler(context));
            }
        }

        private async void handler(HttpListenerContext listenerContext)
        {
            string error = null;
            try
            {
                HttpListenerRequest request = listenerContext.Request;
                if (request.HasEntityBody)
                {
                    using (var inputStream = request.InputStream)
                    {
                        using (StreamReader reader = new StreamReader(inputStream, request.ContentEncoding))
                        {
                            error = await logRequest(request, await reader.ReadToEndAsync());
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLineAsync("Warning: Request has no entity body");
                    error = await logRequest(request, "");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLineAsync(e.ToString());
                error = e.Message;
            }
            
            HttpListenerResponse response = listenerContext.Response;
            if (AlwaysFail || error != null)
            {
                if (error != null)
                {
                    Console.Error.WriteLineAsync(error);
                }
                response.StatusCode = 500;
                response.StatusDescription = string.Format("Internal Server Error: {0}", error);
            }
            else
            {
                response.StatusCode = 204;
                response.StatusDescription = "No Content";
            }
            response.Headers.Set("Connection", "close");
            response.Close();
        }

        private async Task<string> logRequest(HttpListenerRequest request, String body)
        {
            string subdirToUse = Subdir;
            string id;
            if (PerfMode)
            {
                id = Guid.NewGuid().ToString();
                subdirToUse = string.Format("{0}\\{1}", subdirToUse, id.Substring(0, 3));
                Directory.CreateDirectory(subdirToUse);
            }
            else
            {
                id = findId(body);
            }
            
            var filename = string.Format("{0}\\{1}.txt", subdirToUse, id);
            if (!PerfMode)
            {
                int dupCount = 0;
                while (File.Exists(filename))
                {
                    Console.Error.WriteLineAsync(string.Format("Warning: file already exists with id: {0}", id));
                    filename = string.Format("{0}\\{1}.{2}.txt", subdirToUse, id, ++dupCount);
                }
            }

            using (var writer = new StreamWriter(filename))
            {
                await writer.WriteLineAsync(string.Format("{0} {1} HTTP/{2}", request.HttpMethod, request.RawUrl, request.ProtocolVersion));

                for (int i = 0; i < request.Headers.Count; i++)
                {
                    string header = request.Headers.GetKey(i);
                    foreach (string value in request.Headers.GetValues(i))
                    {
                        await writer.WriteLineAsync(string.Format("{0}: {1}", header, value));
                    }
                }

                await writer.WriteLineAsync();
                await writer.WriteLineAsync(body.TrimEnd(new char[] { '\n' }));
            }

            Console.WriteLine(string.Format("Received batch / envelope: {0}", id));

            if (id == null)
            {
                return "Unable to find message ID; file will be named \".txt\"";
            }
            else
            {
                return null;
            }
        }

        private static string findId(String body)
        {
            // Parses the JSON data sent by the 1.0.1 Javascript API
            if (body.StartsWith("data="))
            {
                var result = HttpUtility.UrlDecode(body);
                result = result.Replace(" ", "");

                const int GUIDLength = 36;
                return result.Substring(result.IndexOf("id\":\"") + 5, GUIDLength);
            }
            else
            {
                var doc = new XmlDocument();
                doc.LoadXml(body);

                // looks for a message tag with an id attribute sent by the API (e.g. the .NET API)
                var idNode = doc.SelectSingleNode("messages[@id]");
                if (idNode != null)
                {
                    return idNode.Attributes["id"].Value;
                }
                else
                {
                    // looks for an Id tag sent by the API (e.g. the SOAP message sent by the injected API)
                    var nodes = doc.SelectNodes("//*");

                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            var xmlNode = node as XmlNode;
                            if (xmlNode != null && xmlNode.Name == "Id")
                            {
                                return xmlNode.InnerText;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
