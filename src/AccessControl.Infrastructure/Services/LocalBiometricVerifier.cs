using AccessControl.Application.Access;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Services;

public class LocalBiometricVerifier : IBiometricVerifier
{
    private readonly AccessControlDbContext _db;
    private readonly LocalBiometricTemplateService _templateService;
    private readonly IOptions<BiometricOptions> _options;
    private readonly ILogger<LocalBiometricVerifier> _logger;

    public LocalBiometricVerifier(
        AccessControlDbContext db,
        LocalBiometricTemplateService templateService,
        IOptions<BiometricOptions> options,
        ILogger<LocalBiometricVerifier> logger)
    {
        _db = db;
        _templateService = templateService;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(Guid employeeId, byte[]? faceImage, CancellationToken cancellationToken = default)
    {
        if (faceImage is null || faceImage.Length == 0)
        {
            return false;
        }

        var employee = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new
            {
                e.FaceImage,
                e.FaceEmbedding
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null || employee.FaceImage is null || employee.FaceImage.Length == 0 || employee.FaceEmbedding is null || employee.FaceEmbedding.Length == 0)
        {
            return false;
        }

        try
        {
            var actualEmbedding = _templateService.CreateEmbedding(faceImage);
            if (actualEmbedding is null || actualEmbedding.Length == 0)
            {
                return false;
            }

            var score = _templateService.Compare(employee.FaceEmbedding, actualEmbedding);
            var threshold = _options.Value.Local.MatchThreshold;

            _logger.LogInformation("Local neural biometric verification score for employee {EmployeeId}: {Score}", employeeId, score);
            return score >= threshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local biometric verification failed for employee {EmployeeId}", employeeId);
            return false;
        }
    }
}
