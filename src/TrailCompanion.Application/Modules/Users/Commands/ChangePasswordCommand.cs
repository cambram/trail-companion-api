using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using Microsoft.FeatureManagement;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class ChangePasswordCommand : IRequest
{
    public ChangePasswordCommand(Guid appUserId, ChangePasswordRequest request, string? currentRefreshToken)
    {
        AppUserId = appUserId;
        Request = request;
        CurrentRefreshToken = currentRefreshToken;
    }

    public Guid AppUserId { get; }
    public ChangePasswordRequest Request { get; }
    public string? CurrentRefreshToken { get; }
}

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenIssuer _refreshTokenIssuer;
    private readonly IVariantFeatureManager _featureManager;
    private readonly IBreachedPasswordChecker _breachedPasswords;

    public ChangePasswordCommandHandler(
        IAppUserRepository appUserRepository,
        IRefreshTokensRepository refreshTokensRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenIssuer refreshTokenIssuer,
        IVariantFeatureManager featureManager,
        IBreachedPasswordChecker breachedPasswords)
    {
        _appUserRepository = appUserRepository;
        _refreshTokensRepository = refreshTokensRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenIssuer = refreshTokenIssuer;
        _featureManager = featureManager;
        _breachedPasswords = breachedPasswords;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var currentPassword = command.Request.CurrentPassword ?? string.Empty;
        var newPassword = command.Request.NewPassword ?? string.Empty;

        var appUser = await _appUserRepository.GetByIdAsync(command.AppUserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(AppUser), command.AppUserId);

        if (!_passwordHasher.Verify(currentPassword, appUser.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        var failures = PasswordRules.Check(newPassword).ToList();

        if (newPassword == currentPassword)
        {
            failures.Add(new ValidationFailure("PasswordUnchanged", "New password must differ from the current one."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The password change request is invalid.", failures);
        }

        await PasswordRules.RequireNotBreachedAsync(
            newPassword, _featureManager, _breachedPasswords, "The password change request is invalid.", cancellationToken);

        appUser.PasswordHash = _passwordHasher.Hash(newPassword);
        appUser.PasswordChangedAtUtc = DateTime.UtcNow;
        await _appUserRepository.SaveChangesAsync(cancellationToken);

        var keptTokenHash = string.IsNullOrEmpty(command.CurrentRefreshToken)
            ? null
            : _refreshTokenIssuer.Hash(command.CurrentRefreshToken);
        await _refreshTokensRepository.RevokeAllAsync(command.AppUserId, keptTokenHash, cancellationToken);
    }
}
