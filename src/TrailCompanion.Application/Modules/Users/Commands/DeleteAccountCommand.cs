using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteAccountCommand : IRequest
{
    public DeleteAccountCommand(Guid appUserId, DeleteAccountRequest request)
    {
        AppUserId = appUserId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public DeleteAccountRequest Request { get; }
}

internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPasswordHasher _passwordHasher;

    public DeleteAccountCommandHandler(IAppUserRepository appUserRepository, IPasswordHasher passwordHasher)
    {
        _appUserRepository = appUserRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        var appUser = await _appUserRepository.GetByIdAsync(command.AppUserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(AppUser), command.AppUserId);

        if (!_passwordHasher.Verify(command.Request.Password ?? string.Empty, appUser.PasswordHash))
        {
            throw new UnauthorizedException("Password is incorrect.");
        }

        await _appUserRepository.DeleteAsync(appUser.Id, cancellationToken);
    }
}
