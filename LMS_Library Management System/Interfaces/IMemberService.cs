using System.Collections.Generic;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Interfaces
{
    /// <summary>
    /// Interface for member management operations
    /// </summary>
    public interface IMemberService
    {
        MemberData GetMemberByNumber(string memberNumber);
        
        bool UpdateMember(string memberNumber, string firstName, string lastName, string email, 
            string phone, string address, string department, string memberType, int status);
        
        bool DeleteMember(string memberNumber);
        
        int CreateMember(string email, string firstName, string lastName, string memberType, 
            string phone = null, string address = null, string department = null);
        
        bool IsValidMemberEmail(string email, string memberType);
        
        bool IsValidPhoneNumber(string phone);
        
        List<MemberData> GetAllMembers();
        
        List<Borrowing> GetMemberBorrowingHistory(int memberId);
        
        MemberStatistics GetMemberStatistics(int memberId, string memberType);
        
        int GetBooksBorrowedCount(string memberNumber);
        
        decimal GetUnpaidFinesAmount(string memberNumber);
        
        Dictionary<string, (int BooksBorrowed, decimal UnpaidFines)> GetAllMembersStatistics();
    }
}

