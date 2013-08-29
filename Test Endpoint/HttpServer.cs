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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Test_Endpoint
{
    public class HttpServer
    {
        public int Port
        {
            get
            {
                return ((IPEndPoint)Listener.LocalEndpoint).Port;
            }
        }

        public ManualResetEvent Started = new ManualResetEvent(false);
        public bool Running;
        TcpListener Listener;

        public HttpServer(string host, int port)
        {
            var ep = new IPEndPoint(Dns.GetHostAddresses(host).Last(), port);
            Listener = new TcpListener(ep);
        }

        public void Start()
        {
            try
            {
                Listener.Start();
            }
            catch
            {
                Started.Set();
                return;
            }
            
            Running = true;

            while (true)
            {
                Started.Set();
                Listener.BeginAcceptTcpClient(HandleConnection, Listener);
                GoOn.WaitOne();
            }
        }

        AutoResetEvent GoOn = new AutoResetEvent(false);
        public ConcurrentDictionary<string, Func<HttpRequest, int>> Handlers = new ConcurrentDictionary<string, Func<HttpRequest, int>>();

        public void HandleConnection(IAsyncResult ar)
        {
            GoOn.Set();
            var listener = (TcpListener)ar.AsyncState;
            var client = listener.EndAcceptTcpClient(ar);
            try
            {
                using (var stream = client.GetStream())
                {
                    var reader = new StreamReader(stream);
                    var writer = new StreamWriter(stream);
                    var request = DoRequest(reader, writer);

                    if (request == null)
                    {
                        return;
                    }

                    request.Url = "/";
                    Func<HttpRequest, int> tmp;

                    if (!Handlers.TryGetValue(request.Url, out tmp))
                    {
                        throw new ApplicationException("Couldn't find handler for URL: " + request.Url);
                    }
                    if (tmp(request) != 500)
                    {
                        writer.WriteLine("HTTP/1.1 204 No Content");
                    }
                    else
                    {
                        writer.WriteLine("HTTP/1.1 500 Internal Server Error");
                    }
                    writer.WriteLine("Connection: close");
                    writer.WriteLine("");
                    writer.Flush();

                    writer.Close();
                    reader.Close();
                    stream.Close();
                }
            }
            catch
            {
                
            }
            finally
            {
                client.Close();
            }
        }

        public HttpRequest DoRequest(StreamReader reader, StreamWriter writer)
        {
            string line = reader.ReadLine();

            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var parts = line.Split(' ');
            var request = new HttpRequest { Url = Uri.UnescapeDataString(parts[1]), Headers = GetHeaders(reader) };
            //parts[2] should be HTTP/1.1

            int length;
            bool hasContent = int.TryParse(request.Headers.GetValue("content-length"), out length);

            var expect = request.Headers.GetValue("expect");

            if (!string.IsNullOrEmpty(expect) && expect.ToLower() == "100-continue")
            {
                writer.WriteLine("HTTP/1.1 100 Continue");
                writer.WriteLine("Connection: close");
                writer.WriteLine("");
                writer.Flush();
            }



            request.Data = hasContent ? GetContent(reader, length) : "";

            var sb = new StringBuilder();
            sb.Append(line).Append(Environment.NewLine);

            foreach (var header in request.Headers)
            {
                sb.Append(string.Join(": ", new[] { header.Key, header.Value })).Append(Environment.NewLine);
            }

            sb.Append(request.Data);
            request.FullPost = sb.ToString();

            return request;
        }

        string GetContent(StreamReader reader, int length)
        {
            var buf = new char[length]; //not unicode safe probably, but oh well
            int actual = reader.ReadBlock(buf, 0, length);

            if (actual != length)
            {
                throw new ApplicationException("content length and amount of data actually read does not match!");
            }

            return new string(buf.Take(actual).ToArray());
        }

        List<KeyValuePair<string, string>> GetHeaders(StreamReader reader)
        {
            var headers = new List<KeyValuePair<string, string>>();

            while (true)
            {
                string line = reader.ReadLine();

                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                var parts = line.Split(':');
                headers.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
            }

            return headers;
        }
    }
}
