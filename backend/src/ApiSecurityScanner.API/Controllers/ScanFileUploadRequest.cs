using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ApiSecurityScanner.API.Controllers;

public class ScanFileUploadRequest
{
    [Required]
    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }

    [StringLength(120, MinimumLength = 2)]
    [FromForm(Name = "targetName")]
    public string? TargetName { get; set; }
}
