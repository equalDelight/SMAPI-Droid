using System.Diagnostics;
using System.Text;

internal class Program
{
    private static readonly StringBuilder sbLog = new();

    private static void Main(string[] args)
    {
        const int refreshTime = 100;

        while (true)
        {
            Thread.Sleep(refreshTime);

            // Update current time
            DateTime now = DateTime.Now;
            Log($"Current Time: {now:HH:mm:ss:fff}");

            // Execute adb command to get memory info
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "shell cat /proc/meminfo",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Process memory info output
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 3)
            {
                PrintLineDataFromMemInfo(lines[0]); // MemTotal
                PrintLineDataFromMemInfo(lines[1]); // MemFree
                PrintLineDataFromMemInfo(lines[2]); // MemAvailable
            }

            // Render log to console
            RenderLog();
        }
    }

    // Log message to StringBuilder
    static void Log(string msg) => sbLog.AppendLine(msg);

    // Render log to console and clear StringBuilder
    private static void RenderLog()
    {
        Console.Clear();
        Console.WriteLine(sbLog.ToString());
        sbLog.Clear();
    }

    // Parse and print memory info line data
    static void PrintLineDataFromMemInfo(string lineData)
    {
        var data = lineData.Split(':', StringSplitOptions.TrimEntries);
        if (data.Length == 2 && int.TryParse(data[1].Replace("kB", "").Trim(), out int kb))
        {
            sbLog.AppendLine($"{data[0]}: {kb / 1024f:F3} MB");
        }
    }
}