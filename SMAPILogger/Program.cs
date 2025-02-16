using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;

internal static class Program
{
    const string SMAPILogUrl = "https://smapi.io/log";

    // Extension method to post HTTP request asynchronously
    static async Task<HttpResponseMessage> PostHTTPRequestAsync(this HttpClient client,
        string url, Dictionary<string, string> data)
    {
        using HttpContent formContent = new FormUrlEncodedContent(data);
        return await client.PostAsync(url, formContent).ConfigureAwait(false);
    }

    private static async Task Main(string[] args)
    {
        try
        {
            const string logFilePath = "SMAPI-latest.txt";
            using HttpClient client = new();
            client.BaseAddress = new Uri(SMAPILogUrl);

            // Read the log file content
            var logStringContent = await File.ReadAllTextAsync(logFilePath);

            // Post the log content to the server
            var response = await client.PostHTTPRequestAsync(SMAPILogUrl, new()
            {
                { "input", logStringContent }
            });

            // Check the response status
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Link URL: " + response.RequestMessage.RequestUri);
            }
            else
            {
                Console.WriteLine("Error: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }

        Console.ReadKey();
    }
}