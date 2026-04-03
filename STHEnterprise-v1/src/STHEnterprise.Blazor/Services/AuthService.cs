using STHEnterprise.Blazor.Data;
using STHEnterprise.Blazor.Models;

namespace STHEnterprise.Blazor.Services
{
    public class AuthService
    {
        private readonly MockDatabase _db;
        private User _currentUser;

        public AuthService(MockDatabase db)
        {
            _db = db;
        }

        public User CurrentUser => _currentUser;

        public bool Login(string email, string password)
        {
            var user = _db.Users
                .FirstOrDefault(x => x.Email == email && x.Password == password);

            if (user != null)
            {
                _currentUser = user;
                return true;
            }

            return false;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public bool IsAuthenticated()
        {
            return _currentUser != null;
        }
    }
}