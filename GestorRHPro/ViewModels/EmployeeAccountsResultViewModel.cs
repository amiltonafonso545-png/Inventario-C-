namespace GestorRHPro.ViewModels;

public sealed class EmployeeAccountsResultViewModel
{
    public List<EmployeeAccountCredential> Created { get; } = [];
    public List<string> Skipped { get; } = [];
}

public sealed record EmployeeAccountCredential(string EmployeeName, string Email, string InitialPassword);
