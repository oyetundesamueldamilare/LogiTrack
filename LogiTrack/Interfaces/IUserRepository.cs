using LogiTrack.Models;

namespace LogiTrack.Interfaces
{
    public interface IUserRepository
    {
        Task<AppUser?> GetByEmailAsync(string email);

        Task<AppUser?> UpdateAsync(AppUser user);
    }

}