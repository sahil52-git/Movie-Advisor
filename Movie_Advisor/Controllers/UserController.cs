using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movie_Advisor.Models;
using Movie_Advisor.ViewModels;
using Movie_Advisor.Data;
using Movie_Advisor.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using Claim = System.Security.Claims.Claim;
using ClaimsIdentity = System.Security.Claims.ClaimsIdentity;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Movie_Advisor.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public UserController(
            ILogger<UserController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("recaptcha");
            _context = context;
            _emailService = emailService;
        }

        #region Login

        [HttpGet]
        public IActionResult Login()
        {

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            Console.WriteLine(model);
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await ValidateUserAsync(model.Email, model.Password);
                if (user == null)
                {
                    ViewBag.Error = "Invalid username or password";
                    return View(model);
                }

                if (user.IsActive == false)
                {
                    ViewBag.Error = "Your account has been deactivated. Please contact support.";
                    return View(model);
                }

                await SignInUserAsync(user, model.RememberMe, "Manual");

                _logger.LogInformation("User {Username} logged in successfully with role {RoleName}",
                    user.Username, user.Role.ToString());

                return RedirectToDashboard(user.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View(model);
            }
        }

        #endregion

        #region Signup

        [HttpGet]
        public IActionResult Signup()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }

            ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            try
            {
                ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var existingUsername = await _context.Users
                    .Where(u => u.Username == model.Username)
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    ViewBag.Error = "Username already exists. Please choose a different username.";
                    return View(model);
                }

                var existingEmail = await _context.Users
                    .Where(u => u.Email == model.Email)
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    ViewBag.Error = "Email already registered. Please use a different email or try logging in.";
                    return View(model);
                }

                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    Role = (Role)model.Role,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered: {Username} ({Email})",
                    model.Username, model.Email);

                // Send welcome email (optional - don't block if it fails)
                _ = _emailService.SendWelcomeEmailAsync(model.Email, model.Username);

                TempData["Success"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
                ViewBag.Error = "An error occurred during registration. Please try again.";
                ViewBag.RecaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
                return View(model);
            }
        }

        #endregion

        #region Google OAuth

        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            try
            {
                _logger.LogInformation("=== GoogleLogin Action Called ===");

                if (User.Identity?.IsAuthenticated == true)
                {
                    return RedirectToDashboard();
                }

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    TempData["ReturnUrl"] = returnUrl;
                }

                var callbackUrl = Url.Action(
                    action: "GoogleCallback",
                    controller: "User",
                    values: null,
                    protocol: Request.Scheme,
                    host: Request.Host.Value);

                _logger.LogInformation("Callback URL: {CallbackUrl}", callbackUrl);

                var properties = new AuthenticationProperties
                {
                    RedirectUri = callbackUrl,
                    IsPersistent = true
                };

                return Challenge(properties, GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleLogin");
                TempData["Error"] = "Unable to initiate Google login. Please try again.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            try
            {
                _logger.LogInformation("=== GoogleCallback Action Called ===");

                var authenticateResult = await HttpContext.AuthenticateAsync(
                    GoogleDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded)
                {
                    _logger.LogError("Authentication Failed: {Failure}",
                        authenticateResult.Failure?.Message);
                    TempData["Error"] = "Google authentication failed. Please try again.";
                    return RedirectToAction("Login");
                }

                var claims = authenticateResult.Principal.Claims.ToList();
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("Google Auth Success - Email: {Email}, Name: {Name}",
                    email, name);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                {
                    _logger.LogError("Missing required claims");
                    TempData["Error"] = "Unable to retrieve your Google account information.";
                    return RedirectToAction("Login");
                }

                var user = await GetOrCreateGoogleUserAsync(email, name, googleId);

                if (user == null)
                {
                    _logger.LogError("Failed to get or create user");
                    TempData["Error"] = "An error occurred during authentication.";
                    return RedirectToAction("Login");
                }

                if (user.IsActive == false)
                {
                    _logger.LogWarning("User account is inactive: {Email}", email);
                    TempData["Error"] = "Your account has been deactivated. Please contact support.";
                    return RedirectToAction("Login");
                }

                await SignInUserAsync(user, isPersistent: true, "Google");

                _logger.LogInformation("User signed in successfully: {Email} with role {RoleName}",
                    email, user.Role.ToString());

                var savedReturnUrl = TempData["ReturnUrl"]?.ToString();
                if (!string.IsNullOrEmpty(savedReturnUrl) && Url.IsLocalUrl(savedReturnUrl))
                {
                    return Redirect(savedReturnUrl);
                }

                return RedirectToDashboard(user.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleCallback");
                TempData["Error"] = "An error occurred during Google authentication.";
                return RedirectToAction("Login");
            }
        }

        #endregion

        #region Forgot Password with OTP

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }
            return View(new Models.ForgotPasswordViewModel { Email = "sahilshrestha741@gmail.com" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(Models.ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

                if (user != null)
                {
                    var otp = GenerateOtp();

                    var existingOtps = await _context.OtpVerifications
                        .Where(o => o.Email == model.Email)
                        .ToListAsync();
                    _context.OtpVerifications.RemoveRange(existingOtps);

                    var otpVerification = new OtpVerification
                    {
                        Email = model.Email,
                        OtpCode = otp,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                        Attempts = 0,
                        IsUsed = false
                    };

                    _context.OtpVerifications.Add(otpVerification);
                    await _context.SaveChangesAsync();

                    var emailSent = await _emailService.SendOtpEmailAsync(model.Email, otp, user.Username);

                    if (emailSent)
                    {
                        _logger.LogInformation("OTP sent successfully to {Email}", model.Email);
                        TempData["Email"] = model.Email;
                        return RedirectToAction("VerifyOtp");
                    }
                    else
                    {
                        _logger.LogError("Failed to send OTP email to {Email}", model.Email);
                        TempData["Error"] = "Failed to send OTP email. Please try again.";
                        return View(model);
                    }
                }

                TempData["Email"] = model.Email;
                return RedirectToAction("VerifyOtp");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request");
                TempData["Error"] = "An error occurred. Please try again.";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            TempData.Keep("Email");
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var otpRecord = await _context.OtpVerifications
                    .Where(o => o.Email == model.Email && o.OtpCode == model.OtpCode)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    ViewBag.Error = "Invalid OTP code. Please try again.";
                    return View(model);
                }

                if (otpRecord.IsExpired)
                {
                    ViewBag.Error = "OTP has expired. Please request a new one.";
                    return View(model);
                }

                if (otpRecord.IsUsed)
                {
                    ViewBag.Error = "OTP has already been used. Please request a new one.";
                    return View(model);
                }

                if (otpRecord.Attempts >= 3)
                {
                    ViewBag.Error = "Too many attempts. Please request a new OTP.";
                    return View(model);
                }

                otpRecord.IsUsed = true;
                otpRecord.VerifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP verified successfully for {Email}", model.Email);

                TempData["VerifiedEmail"] = model.Email;
                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP");
                ViewBag.Error = "An error occurred. Please try again.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Email is required.";
                    return RedirectToAction("ForgotPassword");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

                if (user != null)
                {
                    var recentOtp = await _context.OtpVerifications
                        .Where(o => o.Email == email)
                        .OrderByDescending(o => o.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (recentOtp != null && DateTime.UtcNow.Subtract(recentOtp.CreatedAt).TotalSeconds < 60)
                    {
                        TempData["Error"] = "Please wait at least 60 seconds before requesting a new OTP.";
                        TempData["Email"] = email;
                        return RedirectToAction("VerifyOtp");
                    }

                    var oldOtps = await _context.OtpVerifications
                        .Where(o => o.Email == email)
                        .ToListAsync();
                    _context.OtpVerifications.RemoveRange(oldOtps);

                    var otp = GenerateOtp();
                    var otpVerification = new OtpVerification
                    {
                        Email = email,
                        OtpCode = otp,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                        Attempts = 0,
                        IsUsed = false
                    };

                    _context.OtpVerifications.Add(otpVerification);
                    await _context.SaveChangesAsync();

                    await _emailService.SendOtpEmailAsync(email, otp, user.Username);

                    _logger.LogInformation("New OTP sent to {Email}", email);
                    TempData["Success"] = "A new OTP has been sent to your email.";
                }

                TempData["Email"] = email;
                return RedirectToAction("VerifyOtp");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP");
                TempData["Error"] = "An error occurred. Please try again.";
                TempData["Email"] = email;
                return RedirectToAction("VerifyOtp");
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var verifiedEmail = TempData["VerifiedEmail"]?.ToString();
            if (string.IsNullOrEmpty(verifiedEmail))
            {
                TempData["Error"] = "Please verify your OTP first.";
                return RedirectToAction("ForgotPassword");
            }

            TempData.Keep("VerifiedEmail");
            return View(new ResetPasswordViewModel { Email = verifiedEmail });
        }
        #region ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var verifiedEmail = TempData["VerifiedEmail"]?.ToString();

                if (string.IsNullOrEmpty(verifiedEmail) || verifiedEmail != model.Email)
                {
                    TempData["Error"] = "Invalid session. Please start over.";
                    return RedirectToAction("ForgotPassword");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("ForgotPassword");
                }

                user.PasswordHash = HashPassword(model.Password);
                user.UpdatedAt = DateTime.UtcNow;

                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for user: {Email}", user.Email);

                _ = _emailService.SendPasswordResetConfirmationAsync(user.Email, user.Username);

                TempData["Success"] = "Your password has been reset successfully. You can now login with your new password.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                TempData["Error"] = "An error occurred. Please try again.";
                return View(model);
            }
        }
        #endregion

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            return await PerformLogoutAsync();
        }

        [HttpGet]
        public async Task<IActionResult> LogoutConfirm()
        {
            return await PerformLogoutAsync();
        }

        #endregion

        #region Helper Methods

        private async Task<User?> ValidateUserAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("=== LOGIN DEBUG START ===");
                _logger.LogInformation("Attempting login for username: {Username}", username);

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Username or password is empty");
                    return null;
                }

                // Step 1: Check if any users exist in database
                var totalUsers = await _context.Users.CountAsync();
                _logger.LogInformation("Total users in database: {Count}", totalUsers);

                // Step 2: Try to find users by username OR email
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Username == username || u.Email == username)
                    .ToListAsync();

                _logger.LogInformation("Users found with matching username/email: {Count}", users.Count);

                // Debug: Log all found users
                foreach (var u in users)
                {
                    _logger.LogInformation("Found user - ID: {Id}, Username: {Username}, Email: {Email}, IsActive: {IsActive}, Role: {Role}",
                        u.Id, u.Username, u.Email, u.IsActive, u.Role);
                }

                // Step 3: Get the first active user
                var user = users.FirstOrDefault(u => u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("No active user found for username: {Username}", username);

                    // Check if there's an inactive user
                    var inactiveUser = users.FirstOrDefault(u => !u.IsActive);
                    if (inactiveUser != null)
                    {
                        _logger.LogWarning("Found INACTIVE user: {Username}", inactiveUser.Username);
                    }

                    return null;
                }

                _logger.LogInformation("Active user found - ID: {Id}, Username: {Username}", user.Id, user.Username);

                // Step 4: Check password hash
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    _logger.LogWarning("User {Username} has empty password hash!", user.Username);
                    return null;
                }

                _logger.LogInformation("Password hash exists, length: {Length}", user.PasswordHash.Length);

                // Step 5: Verify password
                var passwordValid = VerifyPassword(password, user.PasswordHash);
                _logger.LogInformation("Password verification result: {Result}", passwordValid);

                if (!passwordValid)
                {
                    _logger.LogWarning("Invalid password for user: {Username}", username);
                    return null;
                }

                _logger.LogInformation("Login successful for user: {Username}", user.Username);
                _logger.LogInformation("=== LOGIN DEBUG END ===");

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user credentials for: {Username}", username);
                return null;
            }
        }

        // Also add logging to VerifyPassword
        private bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                _logger.LogInformation("Verifying password - Input length: {InputLength}, Hash length: {HashLength}",
                    password?.Length ?? 0, hashedPassword?.Length ?? 0);

                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                {
                    _logger.LogWarning("Password or hash is null/empty");
                    return false;
                }

                var hashedInput = HashPassword(password);

                // ⚠️ DETAILED DEBUG - REMOVE AFTER FIXING
                _logger.LogInformation("=== HASH COMPARISON ===");
                _logger.LogInformation("Input password: '{Password}'", password);
                _logger.LogInformation("Generated hash: '{Hash}'", hashedInput);
                _logger.LogInformation("Stored hash:    '{Hash}'", hashedPassword);
                _logger.LogInformation("Hashes equal: {Equal}", hashedInput == hashedPassword);
                _logger.LogInformation("Generated hash bytes: {Bytes}", BitConverter.ToString(Encoding.UTF8.GetBytes(hashedInput)));
                _logger.LogInformation("Stored hash bytes:    {Bytes}", BitConverter.ToString(Encoding.UTF8.GetBytes(hashedPassword)));

                var result = hashedInput == hashedPassword;

                _logger.LogInformation("Password comparison result: {Result}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        private async Task<User> GetOrCreateGoogleUserAsync(string email, string? name, string googleId)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.GoogleId == googleId);

                if (existingUser != null)
                {
                    existingUser.LastLogin = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(name))
                    {
                        existingUser.Name = name;
                    }
                    await _context.SaveChangesAsync();
                    return existingUser;
                }

                var emailUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (emailUser != null)
                {
                    emailUser.GoogleId = googleId;
                    emailUser.LastLogin = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(name))
                    {
                        emailUser.Name = name;
                    }
                    await _context.SaveChangesAsync();
                    return emailUser;
                }

                var username = await GenerateUniqueUsernameAsync(name ?? email.Split('@')[0]);

                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    Name = name,
                    GoogleId = googleId,
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    IsActive = true,
                    PasswordHash = string.Empty
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New Google user created: {Email} (Username: {Username})",
                    email, username);

                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating Google user");
                throw;
            }
        }

        private async Task<string> GenerateUniqueUsernameAsync(string baseUsername)
        {
            var username = baseUsername;
            var counter = 1;

            while (await _context.Users.CountAsync(u => u.Username == username) > 0)
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }

        private async Task SignInUserAsync(User user, bool isPersistent, string loginType)
        {
            try
            {
                string userEmail = user.Email ?? string.Empty;
                string userIdString = user.Id.ToString();
                string roleString = user.Role.ToString();

                _logger.LogInformation("=== SignInUserAsync for {Email} with role {RoleName} ===",
                    userEmail, roleString);

                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userIdString),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, userEmail),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleString),
                    new System.Security.Claims.Claim("LoginType", loginType)
                };

                if (!string.IsNullOrEmpty(user.GoogleId))
                {
                    claims.Add(new System.Security.Claims.Claim("GoogleId", user.GoogleId));
                }

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = isPersistent,
                    ExpiresUtc = isPersistent
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(12),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                    authProperties);

                user.LastLogin = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserName", user.Username);
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Role", user.Role.ToString());
                HttpContext.Session.SetString("LoginType", loginType);

                if (!string.IsNullOrEmpty(user.Email))
                {
                    HttpContext.Session.SetString("UserEmail", user.Email);
                }

                if (!string.IsNullOrEmpty(user.GoogleId))
                {
                    HttpContext.Session.SetString("GoogleId", user.GoogleId);
                }

                _logger.LogInformation("Session and cookie authentication completed for {Username}",
                    user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignInUserAsync");
                throw;
            }
        }

        private async Task<IActionResult> PerformLogoutAsync()
        {
            try
            {
                var username = HttpContext.Session.GetString("UserName");

                if (HttpContext.User?.Identity?.IsAuthenticated == true)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }

                HttpContext.Session.Clear();

                _logger.LogInformation("User {Username} logged out successfully",
                    username ?? "Unknown");

                TempData["Success"] = "You have been logged out successfully.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }
        }

        private IActionResult RedirectToDashboard(Role? role = null)
        {
            if (!role.HasValue)
            {
                var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                if (Enum.TryParse<Role>(roleClaim, out var parsedRole))
                {
                    role = parsedRole;
                }
            }

            _logger.LogInformation("Redirecting to dashboard for role: {RoleName}",
                role?.ToString() ?? "Unknown");

            if (role.HasValue && role.Value == Role.Admin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else
            {
                return RedirectToAction("UserDashboard", "Advisor");
            }
        }

        private string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            return (value % 1000000).ToString("D6");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = _configuration["Security:PasswordSalt"] ?? "DefaultSalt2024!";
            var saltedPassword = password + salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        //private bool VerifyPassword(string password, string hashedPassword)
        //{
        //    try
        //    {
        //        var hashedInput = HashPassword(password);
        //        Console.WriteLine("Password", hashedInput);
        //        Console.WriteLine("Password:", password);
        //        return hashedInput == hashedPassword;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error verifying password");
        //        return false;
        //    }
        //}

        #endregion
    }

    public class RecaptchaResponse
    {
        public bool Success { get; set; }
        public float Score { get; set; }
        public string Action { get; set; } = "";
        public DateTime ChallengeTs { get; set; }
        public string Hostname { get; set; } = "";
        public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }
}