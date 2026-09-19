using Template.Application.Modules.Users.Dtos;
using Template.Domain.Modules.Users.Entities;
using Template.Domain.Modules.Users.Repositories;
using MediatR;

namespace Template.Application.Modules.Users.Queries;

public sealed class GetCurrentAppUserQuery : IRequest<AppUserDto>
{
    public GetCurrentAppUserQuery(Guid appUserId)
    {
        AppUserId = appUserId;
    }

    public Guid AppUserId { get; }
}

internal sealed class GetCurrentAppUserQueryHandler : IRequestHandler<GetCurrentAppUserQuery, AppUserDto>
{
    private readonly IAppUserRepository _appUserRepository;

    public GetCurrentAppUserQueryHandler(IAppUserRepository appUserRepository)
    {
        _appUserRepository = appUserRepository;
    }

    public async Task<AppUserDto> Handle(GetCurrentAppUserQuery request, CancellationToken cancellationToken)
    {
        var appUser = await _appUserRepository.GetByIdAsync(request.AppUserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(AppUser), request.AppUserId);

        return AppUserDto.FromEntity(appUser);
    }
}
