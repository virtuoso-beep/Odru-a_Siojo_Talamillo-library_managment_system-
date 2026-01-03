using System.Collections.Generic;
using Library_Management_System.Service;

namespace Library_Management_System.Interfaces
{
    public interface ICirculationService
    {
        List<CirculationService.BorrowingInfo> GetBorrowings(string searchText = "", string statusFilter = "All Status");
        bool CheckoutBook(string memberId, string bookId, int loanPeriodDays = 14);
        bool ReturnBook(string borrowingId);
        bool RenewBook(string borrowingId, int extensionDays = 7);
        CirculationService.BorrowingInfo GetBorrowingById(string borrowingId);
    }
}
