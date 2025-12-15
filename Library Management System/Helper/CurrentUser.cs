using Library_Management_System.Models;

namespace Library_Management_System.Helper
{
    public static class CurrentUser
    {
        private static User _currentUser;

        public static User User
        {
            get { return _currentUser; }
        }

        public static void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        public static void Clear()
        {
            _currentUser = null;
        }

        public static bool IsLoggedIn
        {
            get { return _currentUser != null; }
        }

        public static bool HasRole(UserRole role)
        {
            return IsLoggedIn && _currentUser.Role == role;
        }
    }
}

