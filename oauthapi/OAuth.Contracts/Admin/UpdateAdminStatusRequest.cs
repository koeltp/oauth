using OAuth.Domain.Entities;

namespace OAuth.Contracts.Admin;

public class UpdateAdminStatusRequest
{
    public AdminStatus Status { get; set; }
}