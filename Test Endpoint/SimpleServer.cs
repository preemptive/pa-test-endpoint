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

using System;
using System.IO;
using System.Web;
using System.Threading;
using System.Xml;
using System.Net;
using System.Threading.Tasks;

namespace Test_Endpoint
{
    public class SimpleServer
    {
        private const string SUBDIR = "received";

        private readonly int port;
        private readonly int listenerCount;

        private readonly bool alwaysFail;
        private readonly bool perfMode;

        public SimpleServer(int port, int listenerCount, bool alwaysFail, bool perfMode)
        {
            this.port = port;
            this.listenerCount = listenerCount;

            this.alwaysFail = alwaysFail;
            this.perfMode = perfMode;

            Directory.CreateDirectory(SUBDIR);
        }

        public async Task Start()
        {
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(string.Format("http://+:{0}/", this.port));
                listener.Start();
                Console.WriteLine("Listening on port {0}", port);

                var semaphore = new Semaphore(listenerCount, listenerCount);
                while (true)
                {
                    semaphore.WaitOne();

                    var context = await listener.GetContextAsync();
                    semaphore.Release();

                    await Task.Factory.StartNew(() => Handler(context));
                }
            }
        }

        private async void Handler(HttpListenerContext listenerContext)
        {
            string error = null;
            int responseStatus = 0;
            string responseDescription = null;

            try
            {
                HttpListenerRequest request = listenerContext.Request;
                if (request.HttpMethod == "POST") 
                {
                    if (request.HasEntityBody)
                    {
                        using (var inputStream = request.InputStream)
                        {
                            using (var reader = new StreamReader(inputStream, request.ContentEncoding))
                            {
                                error = await LogRequest(request, await reader.ReadToEndAsync());
                            }
                        }
                    }
                    else
                    {
                        await Console.Error.WriteLineAsync("Warning: POST has no entity body");
                        error = await LogRequest(request, "");
                    }
                    responseStatus = 204;
                    responseDescription = "No Content";

                }
                else
                {
                    responseStatus = 405;
                    responseDescription = "Method Not Supported";
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());                
                error = e.Message;
            }

            HttpListenerResponse response = listenerContext.Response;
            
            if (alwaysFail || error != null)
            {
                if (error != null)
                {
                    await Console.Error.WriteLineAsync(error);
                }

                response.StatusCode = 500;
                response.StatusDescription = string.Format("Internal Server Error: {0}", error);
            }
            else
            {
                response.StatusCode = responseStatus;
                response.StatusDescription = responseDescription;
            }

            // CORS support (for a "Simple Cross-Origin Request", allowing all origins).  See http://www.w3.org/TR/cors/
            if (listenerContext.Request.Headers["Origin"] != null)
            {
                response.Headers.Set("Access-Control-Allow-Origin", "*");
            }

            response.Headers.Set("Connection", "close");

            try
            {
                response.Close();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        private async Task<string> LogRequest(HttpListenerRequest request, String body)
        {
            string subdirToUse = SUBDIR;
            string id;

            if (perfMode)
            {
                id = Guid.NewGuid().ToString();
                subdirToUse = string.Format("{0}\\{1}", subdirToUse, id.Substring(0, 3));
                Directory.CreateDirectory(subdirToUse);
            }
            else
            {
                id = FindId(body);
                if (id == null)
                {
                    id = Guid.NewGuid().ToString();
                    await Console.Error.WriteLineAsync(string.Format("Warning: Unable to find message ID; setting file ID to {0}", id));
                }
            }
            
            
            var filename = string.Format("{0}\\{1}.txt", subdirToUse, id);
            if (!perfMode)
            {
                int dupCount = 0;
                while (File.Exists(filename))
                {
                    await Console.Error.WriteLineAsync(string.Format("Warning: file already exists with id: {0}", id));
                    filename = string.Format("{0}\\{1}.{2}.txt", subdirToUse, id, ++dupCount);
                }
            }

            using (var writer = new StreamWriter(filename))
            {
                await writer.WriteLineAsync(string.Format("{0} {1} HTTP/{2}", request.HttpMethod, request.RawUrl, request.ProtocolVersion));

                for (int i = 0; i < request.Headers.Count; i++)
                {
                    string header = request.Headers.GetKey(i);
                    string[] headers = request.Headers.GetValues(i);
                    if (headers != null)
                    {
                        foreach (string value in headers)
                        {
                            await writer.WriteLineAsync(string.Format("{0}: {1}", header, value));
                        }
                    }
                }

                await writer.WriteLineAsync();
                await writer.WriteLineAsync(body.TrimEnd(new[] { '\n' }));
            }

            Console.WriteLine("Received batch / envelope: {0}", id);


            return null;
        }
     
        private static string FindId(String body)
        {
            // Parses the urlencoded JSON data sent by the 1.0.1 Javascript API
            if (body.StartsWith("data="))
            {
                string result = HttpUtility.UrlDecode(body);
                if (result != null)
                {
                    return FindIdFromJSON(result);
                }

            }
            // Parses the JSON data sent by later versions of the JavaScript API
            else if (body.StartsWith("{"))
            {
                return FindIdFromJSON(body);
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(body);

                // looks for a message tag with an id attribute sent by the API (e.g. the .NET API)
                var idNode = doc.SelectSingleNode("messages[@id]");
                if (idNode != null && idNode.Attributes != null)
                {
                    return idNode.Attributes["id"].Value;
                }

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
            catch (XmlException)
            {
                // Swallow XML parse errors and return null
            }

            return null;
        }

        private static string FindIdFromJSON(string body)
        {
            
            string result = body.Replace(" ", "");

            const int GUIDLength = 36;

            return result.Substring(result.IndexOf("id\":\"") + 5, GUIDLength);

        }
    }
}
