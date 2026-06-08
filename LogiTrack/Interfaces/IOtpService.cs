namespace LogiTrack.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateAndStoreOtpAsync(string email, TimeSpan? ttl = null);
        Task<bool> VerifyOtpAsync(string email, string code);
        Task<bool> HasValidOtpAsync(string email);
    }
}