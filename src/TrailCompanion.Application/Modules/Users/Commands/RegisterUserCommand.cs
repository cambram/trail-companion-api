using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Application.Common.Constants;
using Microsoft.FeatureManagement;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class RegisterUserCommand : IRequest<AppUserDto>
{
    public RegisterUserCommand(RegisterUserRequest request)
    {
        Request = request;
    }

    public RegisterUserRequest Request { get; }
}

internal sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AppUserDto>
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IVariantFeatureManager _featureManager;
    private readonly IBreachedPasswordChecker _breachedPasswords;

    public RegisterUserCommandHandler(
        IAppUserRepository appUserRepository,
        IPasswordHasher passwordHasher,
        IVariantFeatureManager featureManager,
        IBreachedPasswordChecker breachedPasswords)
    {
        _appUserRepository = appUserRepository;
        _passwordHasher = passwordHasher;
        _featureManager = featureManager;
        _breachedPasswords = breachedPasswords;
    }

    public async Task<AppUserDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.RegistrationEnabled, cancellationToken))
        {
            throw new ForbiddenException("Account creation is currently closed.");
        }

        var username = UserName.Normalize(command.Request.Username);
        var password = command.Request.Password ?? string.Empty;

        RequireValid(username, password);
        await PasswordRules.RequireNotBreachedAsync(
            password, _featureManager, _breachedPasswords, "The registration request is invalid.", cancellationToken);

        var appUser = new AppUser
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(password)
        };

        await _appUserRepository.AddAsync(appUser, cancellationToken);
        await _appUserRepository.SaveChangesAsync(cancellationToken);

        return AppUserDto.FromEntity(appUser);
    }

    private static void RequireValid(string username, string password)
    {
        var failures = new List<ValidationFailure>();

        if (!UserName.IsValid(username))
        {
            failures.Add(new ValidationFailure(
                "InvalidUsername",
                "Username must be 3 to 32 characters: letters, numbers, _, . or -."));
        }

        failures.AddRange(PasswordRules.Check(password));

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The registration request is invalid.", failures);
        }
    }
}
