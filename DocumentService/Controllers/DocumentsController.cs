using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Models;

namespace DocumentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly LicenseDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(LicenseDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > 10 * 1024 * 1024) return BadRequest("File too large (max 10MB).");

        var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx" };
        if (!allowed.Contains(ext)) return BadRequest("Only PDF/DOC/DOCX files are allowed.");

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsFolder, storedName);

        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            FileName = originalName,
            FilePath = filePath,
            TenantId = _context.TenantId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return Ok(new { documentId = document.Id, fileName = file.FileName });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null) return NotFound();

        if (!System.IO.File.Exists(document.FilePath)) return NotFound("File not found on server.");

        var memory = new MemoryStream();
        using (var stream = System.IO.File.OpenRead(document.FilePath))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        return File(memory, "application/octet-stream", document.FileName);
    }
}
