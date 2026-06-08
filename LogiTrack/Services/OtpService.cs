using LogiTrack.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Services
{
 
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly Random _rng = new();

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<string> GenerateAndStoreOtpAsync(string email, TimeSpan? ttl = null)
        {
            var code = _rng.Next(100000, 1000000).ToString();
            var expiration = ttl ?? TimeSpan.FromMinutes(5);
            _cache.Set(GetCacheKey(email), code, expiration);
            return Task.FromResult(code);
        }

        public Task<bool> VerifyOtpAsync(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return Task.FromResult(false);

            var key = GetCacheKey(email);
            if (!_cache.TryGetValue(key, out string? stored))
                return Task.FromResult(false);

            if (stored == code)
            {
                _cache.Remove(key);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private static string GetCacheKey(string email) => $"otp:{email.ToLowerInvariant()}";

        public Task<bool> HasValidOtpAsync(string email)
        {
            var key = GetCacheKey(email);
            if (_cache.TryGetValue(key, out string? stored))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

}
