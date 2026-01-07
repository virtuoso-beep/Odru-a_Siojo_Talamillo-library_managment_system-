USE LibraryManagementDB;
CALL SP_CreateStaffUser(
    'staff.user.654321.tc@umindanao.edu.ph',
    'Staff123!',
    'Staff',
    'User'
);
SELECT 
    UserId,
    Email,
    FirstName,
    LastName,
    Role,
    CASE Role
        WHEN 1 THEN 'Administrator'
        WHEN 2 THEN 'Staff'
        WHEN 3 THEN 'Member'
        ELSE 'Unknown'
    END AS RoleName,
    IsActive,
    CreatedDate
FROM Users
WHERE Role = 2
ORDER BY CreatedDate DESC;
