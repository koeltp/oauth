using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth.Application.Interfaces;
using OAuth.Application.Mappers;
using OAuth.Contracts.Admin;
using OAuth.Contracts.Common;
using OAuth.Infrastructure.Options;
using Taipi.Core.RQRS;

namespace OAuth.Server.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/admin/profile")]
[ApiVersion("1.0")]
[Authorize(Roles = "SuperAdmin,Admin,Operator")]
public class ProfileController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly UploadOptions _uploadOptions;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(IAdminService adminService, IOptions<UploadOptions> uploadOptions, ICurrentUserService currentUserService)
    {
        _adminService = adminService;
        _uploadOptions = uploadOptions.Value;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ResponseResult<ProfileResponse>> GetProfile()
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
            return ResponseResult<ProfileResponse>.Unauthorized("无效的用户标识");

        var admin = await _adminService.GetByIdAsync(adminId.Value);

        if (admin == null)
            return ResponseResult<ProfileResponse>.NotFound("管理员不存在");

        return new ResponseResult<ProfileResponse>(admin.ToProfileResponse());
    }

    [HttpPut]
    public async Task<ResponseResult<ProfileResponse>> UpdateProfile([FromBody] AdminUpdateRequest request)
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
            return ResponseResult<ProfileResponse>.Unauthorized("无效的用户标识");

        var admin = await _adminService.GetByIdAsync(adminId.Value);

        if (admin == null)
            return ResponseResult<ProfileResponse>.NotFound("管理员不存在");

        if (!string.IsNullOrEmpty(request.Email))
            admin.Email = request.Email;

        admin.UpdatedAt = DateTime.UtcNow;
        await _adminService.UpdateAsync(admin);

        return new ResponseResult<ProfileResponse>(admin.ToProfileResponse());
    }

    [HttpPost("avatar")]
    public async Task<ResponseResult<AvatarUploadResponse>> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return ResponseResult<AvatarUploadResponse>.BadRequest("请选择图片文件");

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_uploadOptions.AllowedExtensions.Contains(extension))
            return ResponseResult<AvatarUploadResponse>.BadRequest("只支持 JPG、PNG、GIF 格式的图片");

        if (file.Length > _uploadOptions.MaxFileSizeBytes)
            return ResponseResult<AvatarUploadResponse>.BadRequest($"图片大小不能超过{_uploadOptions.MaxFileSizeMB}MB");

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        var fileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploadsPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var adminId = _currentUserService.GetUserId();
        if (adminId != null)
        {
            var admin = await _adminService.GetByIdAsync(adminId.Value);
            if (admin != null)
            {
                admin.AvatarUrl = $"/uploads/avatars/{fileName}";
                await _adminService.UpdateAsync(admin);
            }
        }

        return new ResponseResult<AvatarUploadResponse>(new AvatarUploadResponse
        {
            Message = "上传成功",
            AvatarUrl = $"/uploads/avatars/{fileName}"
        });
    }

    [HttpPut("password")]
    public async Task<ResponseResult<ProfileResponse>> ChangePassword([FromBody] AdminChangePasswordRequest request)
    {
        var adminId = _currentUserService.GetUserId();
        if (adminId == null)
            return ResponseResult<ProfileResponse>.Unauthorized("无效的用户标识");

        var admin = await _adminService.GetByIdAsync(adminId.Value);
        if (admin == null)
            return ResponseResult<ProfileResponse>.NotFound("管理员不存在");

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, admin.PasswordHash))
            return ResponseResult<ProfileResponse>.BadRequest("新密码不能与当前密码相同");

        var result = await _adminService.ChangePasswordAsync(adminId.Value, request.CurrentPassword, request.NewPassword);

        if (!result.Success)
            return ResponseResult<ProfileResponse>.BadRequest(result.Message);

        return new ResponseResult<ProfileResponse> { Message = "密码修改成功" };
    }
}