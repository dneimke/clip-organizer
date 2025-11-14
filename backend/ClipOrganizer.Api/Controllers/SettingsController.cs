using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ClipDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsController> _logger;
    private const string RootFolderKey = "VideoLibrary.RootFolder";

    public SettingsController(
        ClipDbContext context,
        IConfiguration configuration,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("root-folder")]
    public async Task<ActionResult<RootFolderSettingDto>> GetRootFolder()
    {
        try
        {
            // First check database for user-configured root folder
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == RootFolderKey);

            if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                return Ok(new RootFolderSettingDto { RootFolderPath = setting.Value });
            }

            // Fall back to appsettings.json
            var appSettingsPath = _configuration["VideoLibrary:RootFolder"];
            if (!string.IsNullOrWhiteSpace(appSettingsPath))
            {
                return Ok(new RootFolderSettingDto { RootFolderPath = appSettingsPath });
            }

            // Return empty if not configured
            return Ok(new RootFolderSettingDto { RootFolderPath = string.Empty });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root folder setting");
            return StatusCode(500, "An error occurred while getting root folder setting");
        }
    }

    [HttpPut("root-folder")]
    public async Task<ActionResult<RootFolderSettingDto>> SetRootFolder([FromBody] RootFolderSettingDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.RootFolderPath))
        {
            return BadRequest("RootFolderPath is required");
        }

        try
        {
            // Validate path if provided
            if (!string.IsNullOrWhiteSpace(dto.RootFolderPath))
            {
                if (!System.IO.Path.IsPathRooted(dto.RootFolderPath))
                {
                    return BadRequest("Root folder path must be an absolute path");
                }

                if (!System.IO.Directory.Exists(dto.RootFolderPath))
                {
                    return BadRequest("Root folder does not exist");
                }
            }

            // Get or create setting
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == RootFolderKey);

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = RootFolderKey,
                    Value = dto.RootFolderPath
                };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.Value = dto.RootFolderPath;
            }

            await _context.SaveChangesAsync();

            return Ok(new RootFolderSettingDto { RootFolderPath = setting.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting root folder");
            return StatusCode(500, "An error occurred while setting root folder");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SettingDto>>> GetSettings()
    {
        try
        {
            var settings = await _context.Settings
                .Select(s => new SettingDto
                {
                    Key = s.Key,
                    Value = s.Value
                })
                .ToListAsync();

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings");
            return StatusCode(500, "An error occurred while getting settings");
        }
    }
}

