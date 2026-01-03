using System.Collections.Generic;
using Library_Management_System.Service;

namespace Library_Management_System.Interfaces
{
    public interface IFineService
    {
        List<FinesService.FineInfo> GetFines(string searchText = "", string statusFilter = "All Status");
        bool ProcessFinePayment(string fineId);
        bool WaiveFine(string fineId, string reason);
        decimal GetTotalFines(string memberId = null, string status = null);
        FinesService.FineInfo GetFineById(string fineId);
        void CalculateOverdueFines();
        void ProcessOverdueFines(); 
        bool AddFine(string memberId, decimal amount, string reason, string bookTitle = null, string notes = null);
    }
}
