using System.ComponentModel.DataAnnotations;

namespace OAuth.Contracts.Requests;

public class PagedQueryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; set; } = 1;

    [Range(1, 100000, ErrorMessage = "每页数量必须在1-100000之间")]
    public int PageSize { get; set; } = 10;

    public string? Keyword { get; set; }
}