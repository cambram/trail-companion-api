using Template.Application.Modules.Users.Dtos;
using Template.Domain.Modules.Users.Entities;
using Template.Domain.Modules.Users.Repositories;
using MediatR;

namespace Template.Application.Modules.Users.Commands;

public sealed class RegisterUserCommand : IRequest<AppUserDto>
{
    public const int MinimumPasswordLength = 12;

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

        if (password.Length < RegisterUserCommand.MinimumPasswordLength)
        {
            failures.Add(new ValidationFailure(
                "PasswordTooShort",
                $"Password must be at least {RegisterUserCommand.MinimumPasswordLength} characters."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The registration request is invalid.", failures);
        }
    }
}
