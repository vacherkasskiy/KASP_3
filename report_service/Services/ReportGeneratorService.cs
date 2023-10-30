using System.Text.RegularExpressions;
using report_service.Models;

namespace report_service.Services;

public class ReportGeneratorService
{
    private record LogsInfo(
        string ServiceName,
        int RotationsAmount,
        Log[] Logs);
    
    private static async Task<LogsInfo> GetLogsInfoFromDirectory(string serviceName, string directoryPath)
    {
        var files = Directory.GetFiles(directoryPath);
        var pattern = $"{serviceName}\\.\\d+?\\.log";
        var newestLogPattern = $"{serviceName}\\.log";

        var logPaths = files
            .Where(file => Regex.IsMatch(file.Split('\\')[^1], pattern))
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

        return new LogsInfo(
            serviceName,
            logPaths.Count - 1,
            logs.ToArray());
    }

    private static Log? ParseLog(string logString)
    {
        var pattern = @"\[(.*?)\]\[(.*?)\]\[(.*?)\](.*)";
        var match = Regex.Match(logString, pattern);

        if (!match.Success) return null;
        
        var createdAt = DateTime.Parse(match.Groups[1].Value);
        var severity = match.Groups[2].Value;
        var category = match.Groups[3].Value;
        var message = match.Groups[4].Value;
        
        return new Log(createdAt, severity, category, message);
    }

    private static string FormReport(LogsInfo info)
    {
        var logs = info.Logs;
        var earliestTime = logs[0].CreatedAt;
        var latestTime = logs[^1].CreatedAt;
        var severitySlice = new Dictionary<string, int>();
        var categorySlice = new Dictionary<string, int>();

        foreach (var log in logs)
        {
            if (!severitySlice.ContainsKey(log.Severity)) severitySlice.Add(log.Severity, 1);
            else severitySlice[log.Severity]++;
            
            if (!categorySlice.ContainsKey(log.Category)) categorySlice.Add(log.Category, 1);
            else categorySlice[log.Category]++;

            if (earliestTime > log.CreatedAt) earliestTime = log.CreatedAt;
            if (latestTime < log.CreatedAt) latestTime = log.CreatedAt;
        }

        string severitySliceInfo = "", categorySliceInfo = "";
        severitySlice
            .ToList()
            .ForEach(severity =>
                severitySliceInfo += $"{severity.Key}: {severity.Value} ({severity.Value * 100 / logs.Length}%); ");
        categorySlice
            .ToList()
            .ForEach(category =>
                categorySliceInfo += $"{category.Key}: {category.Value} ({category.Value * 100 / logs.Length}%); ");
        
        var report = 
            "======= REPORT =======\n" +
            $"Service name: {info.ServiceName}\n" +
            $"Earliest log time: {earliestTime}\n" +
            $"Latest log time: {latestTime}\n" +
            $"Severity slice info: {severitySliceInfo}\n" +
            $"Category slice info: {categorySliceInfo}\n" +
            $"Rotations amount: {info.RotationsAmount}\n" +
            $"======================\n";
    
        return report;
    }

    public async Task<string> GenerateReport(string serviceName, string logsRelativePath)
    {
        var logsFullPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, logsRelativePath);

        if (!Directory.Exists(logsFullPath))
            throw new DirectoryNotFoundException("Directory does not exist");

        var logsInfo = await GetLogsInfoFromDirectory(serviceName, logsFullPath);

        return FormReport(logsInfo);
    }
}