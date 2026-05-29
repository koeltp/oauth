using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Contracts.Admin;
using OAuth.Infrastructure.Options;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin/profile")]
[ApiVersion("1.0")]
[Authorize(Roles = "SuperAdmin,Admin,Operator")]
public class ProfileController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly UploadOptions _uploadOptions;

    public ProfileController(IAdminService adminService, IOptions<UploadOptions> uploadOptions)
    {
        _adminService = adminService;
        _uploadOptions = uploadOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        var subClaim = User.FindFirst("sub")?.Value 
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var adminId))
        {
            return Unauthorized(new { message = "无效的用户标识" });
        }
        
        var admin = await _adminService.GetByIdAsync(adminId);
        
        if (admin == null)
            return NotFound(new { message = "管理员不存在" });

        return Ok(new ProfileResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Email = admin.Email,
            AvatarUrl = admin.AvatarUrl,
            Role = admin.Role.ToString(),
            CreatedAt = admin.CreatedAt,
            LastLoginAt = admin.LastLoginAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] AdminUpdateRequest request)
    {
        var subClaim = User.FindFirst("sub")?.Value 
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var adminId))
        {
            return Unauthorized(new { message = "无效的用户标识" });
        }
        
        var admin = await _adminService.GetByIdAsync(adminId);
        
        if (admin == null)
            return NotFound(new { message = "管理员不存在" });

        if (!string.IsNullOrEmpty(request.Email))
            admin.Email = request.Email;

        admin.UpdatedAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        return Ok(new { message = "更新成功" });
    }

    [HttpPost("avatar")]
    public async Task<ActionResult<AvatarUploadResponse>> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "请选择图片文件" });

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_uploadOptions.AllowedExtensions.Contains(extension))
            return BadRequest(new { message = "只支持 JPG、PNG、GIF 格式的图片" });

        if (file.Length > _uploadOptions.MaxFileSizeBytes)
            return BadRequest(new { message = $"图片大小不能超过{_uploadOptions.MaxFileSizeMB}MB" });

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        var fileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploadsPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var subClaim = User.FindFirst("sub")?.Value 
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var adminId))
        {
            var admin = await _adminService.GetByIdAsync(adminId);
            if (admin != null)
            {
                admin.AvatarUrl = $"/uploads/avatars/{fileName}";
                await _adminService.UpdateAsync(admin);
            }
        }

        return Ok(new AvatarUploadResponse
        {
            Message = "上传成功",
            AvatarUrl = $"/uploads/avatars/{fileName}"
        });
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] AdminChangePasswordRequest request)
    {
        var subClaim = User.FindFirst("sub")?.Value 
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var adminId))
        {
            return Unauthorized(new { message = "无效的用户标识" });
        }

        var admin = await _adminService.GetByIdAsync(adminId);
        if (admin == null)
            return NotFound(new { message = "管理员不存在" });

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, admin.PasswordHash))
        {
            return BadRequest(new { message = "新密码不能与当前密码相同" });
        }
        
        var result = await _adminService.ChangePasswordAsync(adminId, request.CurrentPassword, request.NewPassword);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = "密码修改成功" });
    }
}
