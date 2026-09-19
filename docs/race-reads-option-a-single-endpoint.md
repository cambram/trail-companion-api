# Race reads, Option A: one "get race" endpoint

Status: not built. Documented in full so it can be built once the segments and items repositories exist.

## Idea

One endpoint returns everything the race screen needs, as a single nested DTO: the race, its segments, and each segment's items.

| Endpoint | Returns |
|---|---|
| `GET /api/races/{id}` | race, with its segments, each with its items |

`GET /api/races` (the list) already exists. Opening a race screen costs one request.

## Why this fits the screen

The race screen in `inspiration/race-planner-mvp-clean.html` shows the hero, all segments and all items together. The hero totals (carbs planned and needed) need every item of every segment. No screen shows segments without their items, so there is nothing to fetch separately.

## Response shape

```json
{
  "id": "guid",
  "name": "UTCT 55K",
  "target": 60,
  "createdAtUtc": "2026-09-19T11:00:00Z",
  "segments": [
    {
      "id": "guid",
      "position": 1,
      "name": "Start to Aid 1",
      "duration": 90,
      "distance": 11,
      "notes": "Easy on the first climb.",
      "items": [
        { "id": "guid", "position": 1, "name": "Maurten Gel 160", "carbs": 40 }
      ]
    }
  ]
}
```

Segments are ordered by `Position`. Items are ordered by `Position`. A race with no segments returns `"segments": []`. A segment with no items returns `"items": []`.

Pace, carbs needed, carbs planned and on-target status are not in the response. The frontend derives them, as the HTML does today.

## Ownership

Only `Races` carries `AppUserId`. Segments and items are owned through their race.

Rules from CLAUDE.md that apply:

- Ownership is enforced in the handler, not the endpoint. The endpoint supplies `appUserId` from `ICurrentUser`, never from the route or body.
- A cross-user read throws `NotFoundException`, which becomes 404. Never 403.
- Every repository read takes `appUserId`.

Because one endpoint owns the whole read, ownership is proved once: load the race with the owner filter first. If it is null, throw. Only then load its children.

## Domain layer

`Domain/Modules/Users/Repositories/ISegmentsRepository.cs`

```csharp
public interface ISegmentsRepository
{
    Task<IReadOnlyList<Segments>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(Segments segment, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`Domain/Modules/Users/Repositories/IItemsRepository.cs`

```csharp
public interface IItemsRepository
{
    Task<IReadOnlyList<Items>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default);
    Task AddAsync(Items item, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Items are fetched by race, not by segment. That returns every item of the race in one query, so the read never loops over segments.

`IRacesRepository.GetByIdAsync(appUserId, id)` already exists and is reused unchanged.

## Infrastructure layer

Files: `Repositories/Users/SegmentsRepository.cs` and `ItemsRepository.cs`. Both inherit `Repository` and use `Context.Set<T>()`. Register both in `RepositoryConfiguration`.

There are no navigation properties, so the owner filter is a subquery on `Races`.

```csharp
public async Task<IReadOnlyList<Segments>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default) =>
    await Context.Set<Segments>()
        .AsNoTracking()
        .Where(s => s.RaceId == raceId
                    && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId))
        .OrderBy(s => s.Position)
        .ToListAsync(cancellationToken);
```

```csharp
public async Task<IReadOnlyList<Items>> GetByRaceIdAsync(Guid appUserId, Guid raceId, CancellationToken cancellationToken = default) =>
    await Context.Set<Items>()
        .AsNoTracking()
        .Where(i => Context.Set<Segments>().Any(s => s.Id == i.SegmentId
                        && s.RaceId == raceId
                        && Context.Set<Races>().Any(r => r.Id == raceId && r.AppUserId == appUserId)))
        .OrderBy(i => i.Position)
        .ToListAsync(cancellationToken);
```

Items come back flat and ordered by `Position`. The handler groups them by `SegmentId`.

A read of a race costs three SQL statements: race, segments, items. That count does not grow with the number of segments.

## Application layer

DTOs in `Modules/Users/Dtos/`, in the existing record style:

```csharp
public record ItemDto(
    Guid Id,
    int Position,
    string Name,
    int Carbs)
{
    public static ItemDto FromEntity(Items item) => new(
        item.Id, item.Position, item.Name, item.Carbs);
}

public record SegmentDto(
    Guid Id,
    int Position,
    string Name,
    int Duration,
    double? Distance,
    string Notes,
    IReadOnlyList<ItemDto> Items)
{
    public static SegmentDto FromEntity(Segments segment, IReadOnlyList<ItemDto> items) => new(
        segment.Id, segment.Position, segment.Name, segment.Duration,
        segment.Distance, segment.Notes, items);
}

public record RaceDetailDto(
    Guid Id,
    string Name,
    int Target,
    DateTime CreatedAtUtc,
    IReadOnlyList<SegmentDto> Segments);
```

Query in `Modules/Users/Queries/GetRaceQuery.cs`: a sealed class taking `appUserId` and `raceId`, with an internal sealed `GetRaceQueryHandler` in the same file.

Handler steps:

1. `IRacesRepository.GetByIdAsync(appUserId, raceId)`. Null throws `NotFoundException(nameof(Races), raceId)`.
2. `ISegmentsRepository.GetByRaceIdAsync(appUserId, raceId)`.
3. `IItemsRepository.GetByRaceIdAsync(appUserId, raceId)`. Group with `ToLookup(i => i.SegmentId)`.
4. Map each segment to a `SegmentDto`, attaching its items from the lookup (empty when none).
5. Return `RaceDetailDto`.

Step 1 makes a foreign or missing race a 404 before any child is read.

## Api layer

Route in `ApiEndpoints.Users`, no raw strings elsewhere:

```csharp
public const string GetRace = $"{RaceBase}/{{id:guid}}";
```

Endpoint `Endpoints/Users/GetRaceEndpoint.cs`, following `GetRacesEndpoint`:

- `[FromServices] ISender` and `ICurrentUser`.
- Return `Unauthorized` when `AppUserId` is null. Otherwise send `GetRaceQuery` and return `TypedResults.Ok`.
- `WithName(Name)`, `WithTags(Modules.Users)`, `.Produces<RaceDetailDto>()`, `.ProducesProblem(401)`, `.ProducesProblem(404)`, `.RequireAuthorization()`.

Once `GetRace` exists, point `CreateRaceEndpoint`'s `Location` at the new race instead of the collection URL.

## Tests

Handler test, `GetRaceQueryHandlerTests`, NSubstitute mocks, `Handle(request, CancellationToken.None)`:

- Returns the nested DTO with items grouped under the right segment.
- A segment with no items returns an empty `Items` list.
- A race with no segments returns an empty `Segments` list.
- Missing or foreign race throws `NotFoundException` and never calls the segments or items repositories.

Integration tests, tagged `[Trait("Category", "Integration")]`, real PostgreSQL:

- `GET /api/races/{id}` returns 401 with no token.
- It returns 404 for another user's race, and for an unknown id.
- It returns segments and items ordered by `Position`.
- Repository tests in `Infrastructure.IntegrationTests` prove the owner subqueries on real PostgreSQL. Do not rely on the EF in-memory provider for this.

## Build order

1. `ItemDto`, `SegmentDto`, `RaceDetailDto`.
2. `ISegmentsRepository`, `IItemsRepository`, implementations, DI registration.
3. `GetRaceQuery` and handler, with handler tests.
4. Route constant, `GetRaceEndpoint`, integration tests.
5. No migration needed. The schema does not change.

## Writes stay granular

Only the read is combined. Edits stay small and separate, as the HTML does: rename a segment, set a duration, add an item, reorder items, delete a segment. Each will be its own command and endpoint later. The frontend either updates local state after a write or re-fetches `GET /api/races/{id}`.

## Tradeoffs

Pros:
- One request per screen open.
- Ownership is proved once.
- One handler, one endpoint and one test set for the whole read.
- No partial loads. The screen renders from one consistent snapshot.

Cons:
- The response is larger than any single piece. A race has a handful of segments with a handful of items each, so this stays small.
- A refresh of one segment's items re-fetches the whole race. Acceptable at this size.
- The DTO couples the three entities in one shape. Adding a field to any of them touches this DTO.

## Related gap: the list page

The race cards in the HTML show segment count, total duration, and planned versus needed carbs. Today's `RaceDto` has none of that. The list needs a slimmer summary DTO or an aggregate query. That is separate from this endpoint.

## If Option B is ever needed

If a later screen shows segments or items on their own, or the nested response grows heavy, add separate endpoints (`/races/{raceId}/segments`, `/segments/{segmentId}/items`) alongside this one. The repositories above already support both.
