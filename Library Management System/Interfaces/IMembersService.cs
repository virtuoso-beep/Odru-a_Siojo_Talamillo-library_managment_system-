using System;
using System.Collections.Generic;
using Library_Management_System.Service;
namespace Library_Management_System.Interfaces
{
    public interface IMembersService
    {
        List<MembersService.MemberInfo> GetMembers(string searchText = "", string statusFilter = "All Status", string typeFilter = "All Types");
        MembersService.MemberStatistics GetMemberStatistics();
        bool RegisterMember(string firstName, string lastName, string email, string phone, string address, string memberType, string status = "Active",
            string idNumber = "", DateTime? dateOfBirth = null, string gender = "", string department = "",
            string emergencyContactName = "", string emergencyContactPhone = "", DateTime? membershipExpiryDate = null);
        MembersService.MemberInfo GetMemberByMemberId(string memberId);
        bool UpdateMember(string memberId, string firstName, string lastName, string email, string memberType, string status, string phone = "", string address = "");
        bool DeleteMember(string memberId);
    }
}
