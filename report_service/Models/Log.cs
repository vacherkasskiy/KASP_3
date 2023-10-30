namespace report_service.Models;

public record Log (DateTime CreatedAt, string Severity, string Category, string Message);