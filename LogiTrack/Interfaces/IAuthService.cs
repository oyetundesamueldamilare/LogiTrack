using LogiTrack.Dto;

namespace LogiTrack.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto model);
        Task<string> LoginAsync(LoginDto model);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordDto model);
    }
}