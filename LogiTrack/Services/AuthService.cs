using LogiTrack.Dto;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using Microsoft.AspNetCore.Identity;

namespace LogiTrack.Services
{
  

    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IJwtTokenService jwtTokenService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
        }

        public async Task<string> RegisterAsync(RegisterDto model)
        {
            if (!await _roleManager.RoleExistsAsync(model.Role))
                throw new Exception($"Role '{model.Role}' does not exist.");

            var user = new AppUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, model.Role);
            return "User registered successfully.";
        }

        public async Task<string> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                throw new UnauthorizedAccessException("Invalid email or password.");

            return await _jwtTokenService.GenerateTokenAsync(user);
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode the token safely for an HTTP URL string
            var encodedToken = System.Web.HttpUtility.UrlEncode(token);

            // Link pointing back to your client application setup
            var resetLink = $"https://yourapp.com/reset-password?token={encodedToken}&email={email}";

            var body = $@"
        <h3>Reset Your Password</h3>
        <p>Please reset your password by clicking the link below:</p>
        <a href='{resetLink}' style='background-color:#4CAF50; color:white; padding:10px 20px; text-decoration:none;'>Reset Password</a>";

            await _emailService.SendEmailAsync(email, "Reset Password - LogiTrack", body);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) throw new Exception("User not found.");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}