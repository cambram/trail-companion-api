using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class UpdateProfileCommand : IRequest<AppUserDto>
{
    public const int MaxNameLength = 100;

    public UpdateProfileCommand(Guid appUserId, UpdateProfileRequest request)
    {
        AppUserId = appUserId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public UpdateProfileRequest Request { get; }
}

internal sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, AppUserDto>
{
    private readonly IAppUserRepository _appUserRepository;

    public UpdateProfileCommandHandler(IAppUserRepository appUserRepository)
    {
        _appUserRepository = appUserRepository;
    }

    public async Task<AppUserDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var firstName = Clean(command.Request.FirstName);
        var lastName = Clean(command.Request.LastName);

        var failures = new List<ValidationFailure>();
        AddIfTooLong(failures, "FirstNameTooLong", "First name", firstName);
        AddIfTooLong(failures, "LastNameTooLong", "Last name", lastName);

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The profile request is invalid.", failures);
        }

        var appUser = await _appUserRepository.GetByIdAsync(command.AppUserId, cancellationToken)
                      ?? throw new NotFoundException(nameof(AppUser), command.AppUserId);

        appUser.FirstName = firstName;
        appUser.LastName = lastName;
        await _appUserRepository.SaveChangesAsync(cancellationToken);

        return AppUserDto.FromEntity(appUser);
    }

    private static string? Clean(string? name) => string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private static void AddIfTooLong(List<ValidationFailure> failures, string code, string field, string? value)
    {
        if (value?.Length > UpdateProfileCommand.MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                code,
                $"{field} must be at most {UpdateProfileCommand.MaxNameLength} characters."));
        }
    }
}
