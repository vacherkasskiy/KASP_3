namespace report_service.Services.Interfaces;

public interface IReportGeneratorService
{
    public Task<string[]> GenerateReport(string serviceName, string logsRelativePath);
}