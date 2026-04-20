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

        var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        // Save with original filename as requested
        var fileName = file.FileName;
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            FileName = file.FileName,
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
        // Bypass TenantId global filter - look up by ID directly
        var document = await _context.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id);
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