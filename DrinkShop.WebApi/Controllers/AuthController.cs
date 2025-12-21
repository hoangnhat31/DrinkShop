using Microsoft.AspNetCore.Mvc;
using DrinkShop.Domain.Entities;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DrinkShop.WebApi.DTO.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DrinkShop.WebApi.Utilities;
using DrinkShop.WebApi.DTO.ApiResponse;
using Microsoft.AspNetCore.Authorization;
using DrinkShop.Application.constance;
using DrinkShop.WebApi.DTO;
using System.Security.Cryptography;
using DrinkShop.Application.Interfaces;

namespace DrinkShop.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IFileStorageService _fileStorageService;

        public AuthController(
            ApplicationDbContext context, 
            IConfiguration config,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _config = config;
            _fileStorageService = fileStorageService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var existingEmail = await _context.TaiKhoans.AnyAsync(u => u.Email == request.Email);
            if (existingEmail)
                return ResponseHelper.Error("Email này đã được sử dụng!", 400);

            var newUser = new TaiKhoan
            {
                HoTen = request.HoTen,
                Email = request.Email,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
                IDVaiTro = 3
            };

            try
            {
                _context.TaiKhoans.Add(newUser);
                await _context.SaveChangesAsync();
                return ResponseHelper.Success("Đăng ký thành công!");
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = msg });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.TaiKhoan) || string.IsNullOrEmpty(request.MatKhau))
                return ResponseHelper.Error("Vui lòng nhập tài khoản và mật khẩu!", 400);

            var user = await _context.TaiKhoans
                .Include(u => u.VaiTro)
                .SingleOrDefaultAsync(u => u.Email == request.TaiKhoan || u.HoTen == request.TaiKhoan);

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.MatKhau, user.MatKhau);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

            return await ProcessLoginSuccess(user, request.RefreshToken);
        }

        private async Task<IActionResult> ProcessLoginSuccess(TaiKhoan user, string? existingRefreshToken)
        {
            if (!string.IsNullOrEmpty(existingRefreshToken))
            {
                if (user.RefreshToken == existingRefreshToken && user.RefreshTokenExpire > DateTime.Now)
                {
                    var newAccToken = GenerateJwtToken(user);
                    return Ok(new { message = "Refresh token ok", accessToken = newAccToken, refreshToken = user.RefreshToken });
                }
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpire = DateTime.Now.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đăng nhập thành công.",
                accessToken,
                refreshToken,
                user = new { user.IDTaiKhoan, user.HoTen, user.Email }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
            {
                return Ok(new { message = "Nếu tài khoản tồn tại, mã xác thực đã được gửi" });
            }

            var token = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            user.ResetToken = token;
            user.ResetTokenExpire = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

#if DEBUG
            return Ok(new { message = "Mã xác thực mới đã được tạo (DEV)", token = token });
#else
            return Ok(new { message = "Nếu tài khoản tồn tại, mã xác thực đã được gửi" });
#endif
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Thiếu token hoặc mật khẩu mới" });

            if (request.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu phải ít nhất 6 ký tự" });

            var user = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.ResetToken == request.Token);

            if (user == null)
                return BadRequest(new { message = "Mã xác nhận không hợp lệ" });

            if (!user.ResetTokenExpire.HasValue || user.ResetTokenExpire.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Mã xác nhận đã hết hạn" });

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpire = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công" });
        }

        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            var userId = int.Parse(User.FindFirst("IDTaiKhoan")?.Value ?? "0");
            var user = await _context.TaiKhoans.FindAsync(userId);

            if (user == null) return ResponseHelper.Error("Không tìm thấy tài khoản.", 404);

            if (request.HoTen != null) user.HoTen = request.HoTen;
            if (request.SDT != null) user.SDT = request.SDT;
            if (request.DiaChi != null) user.DiaChi = request.DiaChi;

            await _context.SaveChangesAsync();

            return ResponseHelper.Success(new
            {
                user.IDTaiKhoan,
                user.HoTen,
                user.Email,
                user.SDT,
                user.DiaChi
            }, "Cập nhật thông tin thành công!");
        }

        [Authorize]
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0) 
                return BadRequest(new { message = "Vui lòng chọn ảnh." });

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) 
                return Unauthorized();

            try
            {
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"avatars/user_{userId}_{Guid.NewGuid()}{ext}";

                string fullUrl;
                using (var stream = file.OpenReadStream())
                {
                    fullUrl = await _fileStorageService.UploadFileAsync(stream, fileName, file.ContentType);
                }

                var user = await _context.TaiKhoans.FindAsync(userId);
                if (user == null) return NotFound();

                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    await _fileStorageService.DeleteFileAsync(user.Avatar);
                }

                user.Avatar = fullUrl;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật ảnh thành công", avatarUrl = fullUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong Token" });

            if (!int.TryParse(userIdString, out int userId))
                return BadRequest(new { message = "ID người dùng không hợp lệ" });

            var user = await _context.TaiKhoans
                .Where(x => x.IDTaiKhoan == userId)
                .Select(x => new { x.HoTen, x.SDT, x.DiaChi })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(user);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userId = int.Parse(User.FindFirst("IDTaiKhoan")?.Value ?? "0");
            var user = await _context.TaiKhoans.FindAsync(userId);

            if (user == null) return ResponseHelper.Error("Lỗi tài khoản.", 404);

            if (string.IsNullOrEmpty(user.MatKhau))
                return ResponseHelper.Error("Tài khoản Google không thể đổi mật khẩu tại đây.", 400);

            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.MatKhauCu, user.MatKhau);
            if (!isOldPasswordCorrect)
                return ResponseHelper.Error("Mật khẩu cũ không chính xác.", 400);

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);
            await _context.SaveChangesAsync();
            return ResponseHelper.Success("Đổi mật khẩu thành công!");
        }

        private string GenerateJwtToken(TaiKhoan user)
        {
            var jwtSecret = _config["JWT_SECRET"] ?? "default_secret_key_at_least_64_characters_long";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            string userRole = user.IDVaiTro switch
            {
                1 => "Quan ly",
                2 => "NhanVien",
                3 => "KhachHang",
                _ => "KhachHang"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IDTaiKhoan.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
                new Claim("IDTaiKhoan", user.IDTaiKhoan.ToString()),
                new Claim(ClaimTypes.Role, userRole),
                new Claim(ClaimTypes.Name, user.HoTen ?? "Unknown")
            };

            if (user.VaiTro != null && !string.IsNullOrEmpty(user.VaiTro.Permission))
            {
                if (user.VaiTro.Permission.Contains(Permissions.FullAccess))
                {
                    var allPermissions = Permissions.GetAllPermissions();
                    foreach (var p in allPermissions) claims.Add(new Claim("Permission", p));
                }
                else
                {
                    var permissionList = user.VaiTro.Permission.Split(',');
                    foreach (var p in permissionList)
                    {
                        if (!string.IsNullOrWhiteSpace(p)) claims.Add(new Claim("Permission", p.Trim()));
                    }
                }
            }

            var token = new JwtSecurityToken(
                issuer: "DrinkShop",
                audience: "DrinkShopClient",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class GoogleLoginRequest
        {
            public string Token { get; set; } = "";
        }
    }
}