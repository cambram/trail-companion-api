using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
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

    public RegisterUserCommandHandler(IAppUserRepository appUserRepository, IPasswordHasher passwordHasher)
    {
        _appUserRepository = appUserRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AppUserDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = UserEmail.Normalize(command.Request.Email);
        var password = command.Request.Password ?? string.Empty;

        RequireValid(email, password);

        var appUser = new AppUser
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(password)
        };

        await _appUserRepository.AddAsync(appUser, cancellationToken);
        await _appUserRepository.SaveChangesAsync(cancellationToken);

        return AppUserDto.FromEntity(appUser);
    }

    private static void RequireValid(string email, string password)
    {
        var failures = new List<ValidationFailure>();

        if (email.Length is 0 or > UserEmail.MaxLength || !email.Contains('@'))
        {
            failures.Add(new ValidationFailure("InvalidEmail", "A valid email address is required."));
        }

        failures.AddRange(PasswordRules.Check(password));

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The registration request is invalid.", failures);
        }
    }
}
