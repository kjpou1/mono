﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebAssembly;
using System.Threading;
using System.IO;

namespace TestSuite
{
    public class Program
    {
        static CancellationTokenSource cts = null;

        public static bool IsStreamingSupported()
        {
            using (HttpClient httpClient = CreateHttpClient())
                return WebAssemblyHttpHandler.StreamingSupported;
        }

        public static bool IsStreamingEnabled()
        {
            using (HttpClient httpClient = CreateHttpClient())
                return WebAssemblyHttpHandler.StreamingEnabled;
        }

        public static string BasePath()
        {
            using (HttpClient httpClient = CreateHttpClient())
                return httpClient.BaseAddress.ToString();
        }

        public static async Task<object> RequestStream(bool streamingEnabled, string url)
        {
            var requestTcs = new TaskCompletionSource<object>();
            cts = new CancellationTokenSource();

            try
            {
                using (HttpClient httpClient = CreateHttpClient())
                {
                    Console.WriteLine($"streaming supported: { WebAssemblyHttpHandler.StreamingSupported}");
                    WebAssemblyHttpHandler.StreamingEnabled = streamingEnabled;
                    Console.WriteLine($"streaming enabled: {WebAssemblyHttpHandler.StreamingEnabled}");
                    using (var rspMsg = await httpClient.GetAsync(url, cts.Token))
                    {
                        requestTcs.SetResult((int)rspMsg.Content?.ReadAsStreamAsync().Result.Length);
                    }
                }
            }
            catch (Exception exc2)
            {
                requestTcs.SetException(exc2);
            }

            return requestTcs.Task;
        }

        public static async Task<object> RequestByteArray(bool streamingEnabled, string url)
        {
            var requestTcs = new TaskCompletionSource<object>();
            cts = new CancellationTokenSource();

            try
            {
                using (HttpClient httpClient = CreateHttpClient())
                {
                    Console.WriteLine($"streaming supported: { WebAssemblyHttpHandler.StreamingSupported}");
                    WebAssemblyHttpHandler.StreamingEnabled = streamingEnabled;
                    Console.WriteLine($"streaming enabled: {WebAssemblyHttpHandler.StreamingEnabled}");
                    Console.WriteLine($"url: {url}");

                    using (var rspMsg = await httpClient.GetAsync(url, cts.Token))
                    {
                        requestTcs.SetResult(rspMsg.Content?.ReadAsByteArrayAsync().Result.Length);
                    }
                }
            }
            catch (Exception exc2)
            {
                requestTcs.SetException(exc2);
            }
            return requestTcs.Task;
        }

        public static async Task<object> GetStreamAsync_ReadZeroBytes_Success()
        {
            var requestTcs = new TaskCompletionSource<object>();

            using (HttpClient client = CreateHttpClient())
            using (Stream stream = await client.GetStreamAsync("base/publish/netstandard2.0/NowIsTheTime.txt"))
            {
                requestTcs.SetResult(await stream.ReadAsync(new byte[1], 0, 0));
            }

            return requestTcs.Task;
        }

        static HttpClient CreateHttpClient()
        {
            //Console.WriteLine("Create  HttpClient");
            string BaseApiUrl = string.Empty;
            var window = (JSObject)WebAssembly.Runtime.GetGlobalObject("window");
            using (var location = (JSObject)window.GetObjectProperty("location"))
            {
                BaseApiUrl = (string)location.GetObjectProperty("origin");
            }
            WebAssemblyHttpHandler.StreamingEnabled = true;
            return new HttpClient() { BaseAddress = new Uri(BaseApiUrl) };
        }
    }
}