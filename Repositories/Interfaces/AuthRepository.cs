using InframartAPI_New.Models;

namespace InframartAPI_New.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        User? GetUserByEmail(string email);
        void RegisterUser(User user);
    }
}