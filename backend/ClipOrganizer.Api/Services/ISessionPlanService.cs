using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Services;

public interface ISessionPlanService
{
    Task<SessionPlanDto> GenerateSessionPlanAsync(GenerateSessionPlanDto request, List<Clip> availableClips);
}

