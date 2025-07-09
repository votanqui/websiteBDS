using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using thuctap2025.Data;
using thuctap2025.Models;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(ApplicationDbContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GenerateJwtToken(Users user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role), // Thêm Role vào Claims
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("FullName", user.FullName) // Thêm FullName vào Claims
    };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(6),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void SetJwtToken(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, //ật lên là không đọc được cookie
            Secure = true, // Phải bật nếu dùng HTTPS
            SameSite = SameSiteMode.None, // Cho phép cross-site cookies
            Expires = DateTime.Now.AddHours(6),
        };

        _httpContextAccessor.HttpContext.Response.Cookies.Append("jwt", token, cookieOptions);
    }
    public ClaimsPrincipal GetUserFromToken()
    {
        var token = _httpContextAccessor.HttpContext.Request.Cookies["JwtToken"];
        if (token == null) return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                ValidateLifetime = true
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
