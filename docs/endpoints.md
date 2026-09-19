# Endpoints

Every route, its request and response schema, and where the code lives. Base path is `api`. Route constants are in [ApiEndpoints.cs](../src/TrailCompanion.Api/Endpoints/ApiEndpoints.cs). Auth details are in [auth.md](./auth.md).

Every endpoint except register and login needs `Authorization: Bearer <token>`. Race, segment and item routes only ever see the caller's own data; anything else returns 404.

## Users

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/users/register` | Create a new account from email and password, returning user. | [RegisterUserRequest](#registeruserrequest) | 201 [AppUserDto](#appuserdto) | [RegisterUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/RegisterUserEndpoint.cs) |
| POST | `/api/users/login` | Exchange email and password for a short-lived JWT access token. | [LoginUserRequest](#loginuserrequest) | 200 [AccessTokenDto](#accesstokendto) | [LoginUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/LoginUserEndpoint.cs) |
| GET | `/api/users/me` | Return the authenticated caller's own account details and creation date. | none | 200 [AppUserDto](#appuserdto) | [GetMyUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetMyUserEndpoint.cs) |

## Races

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| GET | `/api/races` | List every race owned by the authenticated user, without segments. | none | 200 [RaceDto](#racedto)[] | [GetRacesEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetRacesEndpoint.cs) |
| POST | `/api/races` | Create a race with a name and optional carb target. | [CreateRaceRequest](#createracerequest) | 201 [RaceDto](#racedto) | [CreateRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateRaceEndpoint.cs) |
| GET | `/api/races/{id}` | Return one race with its segments and each segment's items. | none | 200 [RaceDetailDto](#racedetaildto) | [GetRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetRaceEndpoint.cs) |
| PUT | `/api/races/{id}` | Rename an owned race and optionally change its carb target; omitted target is kept. | [CreateRaceRequest](#createracerequest) | 200 [RaceDto](#racedto) | [UpdateRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateRaceEndpoint.cs) |
| DELETE | `/api/races/{id}` | Delete an owned race; unknown or foreign races return 404. | none | 204 | [DeleteRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteRaceEndpoint.cs) |

## Segments

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/races/{raceId}/segments` | Add a segment to a race, appended at next position. | [SegmentRequest](#segmentrequest) | 201 [SegmentDto](#segmentdto) | [CreateSegmentEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateSegmentEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/order` | Set segment order from a complete list of segment ids; positions become 1..n. | [ReorderRequest](#reorderrequest) | 204 | [ReorderSegmentsEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/ReorderSegmentsEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/{id}` | Replace a race segment's name, duration, distance and notes fields. | [SegmentRequest](#segmentrequest) | 200 [SegmentDto](#segmentdto) | [UpdateSegmentEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateSegmentEndpoint.cs) |
| DELETE | `/api/races/{raceId}/segments/{id}` | Delete one segment from an owned race by its identifier. | none | 204 | [DeleteSegmentEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteSegmentEndpoint.cs) |

## Items

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/races/{raceId}/segments/{segmentId}/items` | Add a fuel item with carbs to an owned segment. | [ItemRequest](#itemrequest) | 201 [ItemDto](#itemdto) | [CreateItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateItemEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/{segmentId}/items/order` | Set item order from a complete list of item ids; positions become 1..n. | [ReorderRequest](#reorderrequest) | 204 | [ReorderItemsEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/ReorderItemsEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/{segmentId}/items/{id}` | Replace a fuel item's name and carbs on a segment. | [ItemRequest](#itemrequest) | 200 [ItemDto](#itemdto) | [UpdateItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateItemEndpoint.cs) |
| DELETE | `/api/races/{raceId}/segments/{segmentId}/items/{id}` | Delete one fuel item from a segment by its identifier. | none | 204 | [DeleteItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteItemEndpoint.cs) |

## Schemas

All live in [`Application/Modules/Users/Dtos/`](../src/TrailCompanion.Application/Modules/Users/Dtos/).

### Requests

| Schema | Description | Fields | File |
|---|---|---|---|
| <a id="registeruserrequest"></a>RegisterUserRequest | Email and password supplied when creating a new user account. | `email`, `password` | [RegisterUserRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RegisterUserRequest.cs) |
| <a id="loginuserrequest"></a>LoginUserRequest | Email and password credentials supplied when requesting an access token. | `email`, `password` | [LoginUserRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/LoginUserRequest.cs) |
| <a id="createracerequest"></a>CreateRaceRequest | Race name plus optional carbs-per-hour target, which defaults to sixty. | `name`, `target?` | [CreateRaceRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/CreateRaceRequest.cs) |
| <a id="segmentrequest"></a>SegmentRequest | Name, duration, optional distance and notes for a race segment. | `name`, `duration`, `distance?`, `notes?` | [SegmentRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/SegmentRequest.cs) |
| <a id="itemrequest"></a>ItemRequest | Name and carbs value for a single fuel item entry. | `name`, `carbs?` | [ItemRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ItemRequest.cs) |
| <a id="reorderrequest"></a>ReorderRequest | Every sibling id exactly once, in the wanted order. | `ids` | [ReorderRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ReorderRequest.cs) |

### Responses

| Schema | Description | Fields | File |
|---|---|---|---|
| <a id="appuserdto"></a>AppUserDto | User identifier, email and creation timestamp returned to the client. | `id`, `email`, `createdAtUtc` | [AppUserDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AppUserDto.cs) |
| <a id="accesstokendto"></a>AccessTokenDto | Signed JWT string plus its UTC expiry time for clients. | `accessToken`, `expiresAtUtc` | [AccessTokenDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AccessTokenDto.cs) |
| <a id="racedto"></a>RaceDto | Race summary with identifier, name, target and creation timestamp only. | `id`, `name`, `target`, `createdAtUtc` | [RaceDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RaceDto.cs) |
| <a id="racedetaildto"></a>RaceDetailDto | Race with nested segments, each carrying its own items list. | `id`, `name`, `target`, `createdAtUtc`, `segments[]` (`id`, `position`, `name`, `duration`, `distance?`, `notes`, `items[]`) | [RaceDetailDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RaceDetailDto.cs) |
| <a id="segmentdto"></a>SegmentDto | Segment fields including its race identifier, without nested items. | `id`, `raceId`, `position`, `name`, `duration`, `distance?`, `notes` | [SegmentDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/SegmentDto.cs) |
| <a id="itemdto"></a>ItemDto | Item fields including its owning segment identifier and its position. | `id`, `segmentId`, `position`, `name`, `carbs` | [ItemDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ItemDto.cs) |

## Error responses

Failures return `ProblemDetails` with a `correlationId`: 400 validation, 401 unauthenticated or bad login, 404 missing or not owned, 409 duplicate email, 429 login rate limit.
