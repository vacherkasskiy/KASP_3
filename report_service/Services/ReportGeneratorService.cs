using System.Text.RegularExpressions;
using report_service.Models;

namespace report_service.Services;

public class ReportGeneratorService
{
    private record LogsInfo(string ServiceName, int RotationsAmount, IEnumerable<Log> Logs);
    
    private static async Task<LogsInfo> GetLogsInfoFromDirectory(string serviceName, string directoryPath)
    {
        var files = Directory.GetFiles(directoryPath);
        var pattern = $"{serviceName}\\.\\d+?\\.log";
        var newestLogPattern = $"{serviceName}\\.log";

        var logPaths = files
            .Where(file => Regex.IsMatch(file.Split('\\')[^1], pattern))
            .OrderBy(file => int.Parse(file.Split('\\')[^1].Split('.')[1]))
            .ToList();

        var newestLogPath = files
            .FirstOrDefault(file => Regex.IsMatch(file.Split('\\')[^1], newestLogPattern));
        
        if (newestLogPath != null) logPaths.Add(newestLogPath);

        var logs = new List<Log>();
        foreach (var logPath in logPaths)
        {
            var lines = await File.ReadAllLinesAsync(logPath);
            foreach (var line in lines)
            {
                var log = ParseLog(line);
                if (log != null) logs.Add(log);
            }
        }

        return new LogsInfo(serviceName, logs.Count, logs);
    }

    private static Log? ParseLog(string logString)
    {
        var pattern = @"\[(.*?)\]\[(.*?)\]\[(.*?)\](.*)";
        var match = Regex.Match(logString, pattern);

        if (!match.Success) return null;
        
        var createdAt = DateTime.Parse(match.Groups[1].Value);
        var severity = match.Groups[2].Value;
        var type = match.Groups[3].Value;
        var message = match.Groups[4].Value;
        
        return new Log(createdAt, severity, type, message);

    }

    // private async Task<string> FormReport(string serviceName, IEnumerable<Log> logs)
    // {
    //     var report = 
    //         "======= REPORT =======\n" +
    //         $"Service name: {serviceName}\n" +
    //         $"Earliest log time: {}\n" +
    //         $"Latest log time: {}\n" +
    //         $"Rotations amount: {}\n" +
    //         $"=========================\n";
    //
    //     return report;
    // }

    public async Task<IEnumerable<Log>> GenerateReport(string serviceName, string logsRelativePath)
    {
        string logsFullPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, logsRelativePath);

        if (!Directory.Exists(logsFullPath))
            throw new DirectoryNotFoundException("Directory does not exist");

        var logsInfo = await GetLogsInfoFromDirectory(serviceName, logsFullPath);

        return logsInfo.Logs;
    }
}