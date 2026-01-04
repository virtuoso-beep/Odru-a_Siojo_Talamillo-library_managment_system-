using Library_Management_System.Service;
namespace Library_Management_System.Interfaces
{
    public interface IDashboardService
    {
        DashboardService.DashboardStatistics GetDashboardStatistics();
        decimal CalculatePercentageChange(decimal current, decimal previous);
    }
}
