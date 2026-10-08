# Endpoints

Every route, its request and response schema, and where the code lives. Base path is `api`. Route constants are in [ApiEndpoints.cs](../src/TrailCompanion.Api/Endpoints/ApiEndpoints.cs). Auth details are in [auth.md](./auth.md).

Every endpoint except register, login, refresh and logout needs `Authorization: Bearer <token>`. Race reads also allow crew (a user who joined a race by PIN), and every write is owner-only: crew get 404 on any write. Anything else the caller does not own or has not joined returns 404.

## Users

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/users/register` | Create a new account from username and password, returning user. | [RegisterUserRequest](#registeruserrequest) | 201 [AppUserDto](#appuserdto); 400 `PasswordTooLong` / `PasswordBreached`; 403 when account creation is closed; 429 rate limited | [RegisterUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/RegisterUserEndpoint.cs) |
| POST | `/api/users/login` | Exchange username and password for a short-lived JWT access token. | [LoginUserRequest](#loginuserrequest) | 200 [AccessTokenDto](#accesstokendto); 403 foreign Origin | [LoginUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/LoginUserEndpoint.cs) |
| POST | `/api/users/refresh` | Rotate the `refresh_token` cookie and return a new access token. | cookie | 200 [AccessTokenDto](#accesstokendto); 403 foreign Origin | [RefreshTokenEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/RefreshTokenEndpoint.cs) |
| POST | `/api/users/logout` | Revoke the refresh cookie and clear it. No bearer needed. | cookie | 204; 403 foreign Origin | [LogoutEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/LogoutEndpoint.cs) |
| GET | `/api/users/me` | Return the authenticated caller's own account details and creation date. | none | 200 [AppUserDto](#appuserdto) | [GetMyUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetMyUserEndpoint.cs) |
| PUT | `/api/users/me` | Set or clear first and last name. Username cannot change. | [UpdateProfileRequest](#updateprofilerequest) | 200 [AppUserDto](#appuserdto) | [UpdateMyUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateMyUserEndpoint.cs) |
| DELETE | `/api/users/me` | Permanently hard-delete the account and everything it owns. 401 on wrong password. | [DeleteAccountRequest](#deleteaccountrequest) | 204 | [DeleteMyUserEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteMyUserEndpoint.cs) |
| POST | `/api/users/password` | Change password; revokes the caller's other refresh tokens. 401 on wrong current password. | [ChangePasswordRequest](#changepasswordrequest) | 204 | [ChangePasswordEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/ChangePasswordEndpoint.cs) |

## Races

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| GET | `/api/races` | List every race the caller owns or has joined as crew, without segments. | none | 200 [RaceDto](#racedto)[] | [GetRacesEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetRacesEndpoint.cs) |
| POST | `/api/races` | Create a race with a name and optional carb target. | [CreateRaceRequest](#createracerequest) | 201 [RaceDto](#racedto); 400 `LimitReached` | [CreateRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateRaceEndpoint.cs) |
| GET | `/api/races/{id}` | Return one race with its segments and each segment's items, for the owner or crew. `pin` is set for the owner only. | none | 200 [RaceDetailDto](#racedetaildto) | [GetRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetRaceEndpoint.cs) |
| PUT | `/api/races/{id}` | Rename an owned race and optionally change its carb target; omitted target is kept. | [CreateRaceRequest](#createracerequest) | 200 [RaceDto](#racedto) | [UpdateRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateRaceEndpoint.cs) |
| DELETE | `/api/races/{id}` | Delete an owned race; unknown or foreign races return 404. | none | 204 | [DeleteRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteRaceEndpoint.cs) |

## Crew

Crew are other users who joined a race with its 6-digit PIN. They can read the plan, including the owner's library items it references, and cannot change anything. A race holds at most 20 crew. Joining twice is harmless.

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/races/join` | Join a race's crew by PIN. | [JoinRaceRequest](#joinracerequest) | 200 [RaceDto](#racedto) with `role: "Crew"`; 400 `InvalidPin` / `OwnRace` / `LimitReached`; 404 unknown PIN; 429 rate limited | [JoinRaceEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/JoinRaceEndpoint.cs) |
| GET | `/api/races/{raceId}/crew` | List a race's crew, for the owner or a crew member. | none | 200 [CrewMemberDto](#crewmemberdto)[]; 404 | [GetRaceCrewEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetRaceCrewEndpoint.cs) |
| DELETE | `/api/races/{raceId}/crew/{memberId}` | Remove a crew member. The owner can remove anyone; a crew member can remove only themselves. `memberId` is the member's `appUserId`. | none | 204; 404 | [RemoveCrewMemberEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/RemoveCrewMemberEndpoint.cs) |
| POST | `/api/races/{id}/pin` | Replace the race's PIN; the old PIN stops working. Owner only. | none | 200 [RacePinDto](#racepindto); 404 | [RegenerateRacePinEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/RegenerateRacePinEndpoint.cs) |

## Segments

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/races/{raceId}/segments` | Add a segment to a race, appended at next position. | [SegmentRequest](#segmentrequest) | 201 [SegmentDto](#segmentdto); 400 `LimitReached` | [CreateSegmentEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateSegmentEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/order` | Set segment order from a complete list of segment ids; positions become 1..n. | [ReorderRequest](#reorderrequest) | 204 | [ReorderSegmentsEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/ReorderSegmentsEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/{id}` | Replace a race segment's name, duration, end km and notes fields. | [SegmentRequest](#segmentrequest) | 200 [SegmentDto](#segmentdto) | [UpdateSegmentEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateSegmentEndpoint.cs) |
| DELETE | `/api/races/{raceId}/segments/{id}` | Delete one segment from an owned race by its identifier. | none | 204 | [DeleteSegmentEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteSegmentEndpoint.cs) |

## Items

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/races/{raceId}/segments/{segmentId}/items` | Attach a nutrition library item to an owned segment. | [ItemRequest](#itemrequest) | 201 [ItemDto](#itemdto); 400 `LimitReached` | [CreateItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateItemEndpoint.cs) |
| PUT | `/api/races/{raceId}/segments/{segmentId}/items/order` | Set item order from a complete list of item ids; positions become 1..n. | [ReorderRequest](#reorderrequest) | 204 | [ReorderItemsEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/ReorderItemsEndpoint.cs) |
| DELETE | `/api/races/{raceId}/segments/{segmentId}/items/{id}` | Delete one fuel item from a segment by its identifier. | none | 204 | [DeleteItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteItemEndpoint.cs) |

## Aid stations

An aid station sits before a segment (the gap between the previous segment and this one), at most one per segment. Moving a station into an occupied gap swaps the two. Station items count toward the carbs of the segment the station precedes. Deleting a library item used by any station returns 409 as it does for segment items.

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| POST | `/api/races/{raceId}/aid-stations` | Add an aid station before a segment. 409 when that segment already has one. | [CreateAidStationRequest](#createaidstationrequest) | 201 [AidStationDto](#aidstationdto) | [CreateAidStationEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateAidStationEndpoint.cs) |
| PUT | `/api/races/{raceId}/aid-stations/{id}` | Replace an aid station's name, notes and kit list. | [AidStationRequest](#aidstationrequest) | 200 [AidStationDto](#aidstationdto) | [UpdateAidStationEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateAidStationEndpoint.cs) |
| DELETE | `/api/races/{raceId}/aid-stations/{id}` | Delete an aid station and its items. | none | 204 | [DeleteAidStationEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteAidStationEndpoint.cs) |
| PUT | `/api/races/{raceId}/aid-stations/{id}/move` | Move an aid station before another segment, swapping with any station already there. | [MoveAidStationRequest](#moveaidstationrequest) | 204 | [MoveAidStationEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/MoveAidStationEndpoint.cs) |
| POST | `/api/races/{raceId}/aid-stations/{aidStationId}/items` | Attach a nutrition library item to an aid station. | [ItemRequest](#itemrequest) | 201 [AidStationItemDto](#aidstationitemdto); 400 `LimitReached` | [CreateAidStationItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateAidStationItemEndpoint.cs) |
| PUT | `/api/races/{raceId}/aid-stations/{aidStationId}/items/order` | Set item order from a complete list of item ids; positions become 1..n. | [ReorderRequest](#reorderrequest) | 204 | [ReorderAidStationItemsEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/ReorderAidStationItemsEndpoint.cs) |
| DELETE | `/api/races/{raceId}/aid-stations/{aidStationId}/items/{id}` | Delete one item from an aid station. | none | 204 | [DeleteAidStationItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteAidStationItemEndpoint.cs) |

Every route returns 401 without a token and 404 for a missing or foreign race, station, segment or item. Validation failures return 400.

## Nutrition items

`Type` is one of `Gel`, `Liquid`, `Solid` or `Pill`, and it implies the unit of `Size`: grams for Gel and Solid, millilitres for Liquid, milligrams for Pill. Carbs are grams, calories are calories, every other macro is milligrams. Sodium, potassium, magnesium and calcium accept decimals (`numeric(8,2)`); size and the rest are whole numbers. Names are unique per user.

| Method | Route | Description | Request | Response | Endpoint file |
|---|---|---|---|---|---|
| GET | `/api/nutrition-items` | List the caller's nutrition library, ordered by name. | none | 200 [NutritionItemDto](#nutritionitemdto)[] | [GetNutritionItemsEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/GetNutritionItemsEndpoint.cs) |
| POST | `/api/nutrition-items` | Create a nutrition library item. 409 when the name is already taken. | [NutritionItemRequest](#nutritionitemrequest) | 201 [NutritionItemDto](#nutritionitemdto); 400 `LimitReached` | [CreateNutritionItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/CreateNutritionItemEndpoint.cs) |
| PUT | `/api/nutrition-items/{id}` | Replace every field of an owned library item. Changes apply to every race using it. | [NutritionItemRequest](#nutritionitemrequest) | 200 [NutritionItemDto](#nutritionitemdto) | [UpdateNutritionItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/UpdateNutritionItemEndpoint.cs) |
| DELETE | `/api/nutrition-items/{id}` | Delete an owned library item. 409 when any segment or aid station still uses it. | none | 204 | [DeleteNutritionItemEndpoint.cs](../src/TrailCompanion.Api/Endpoints/Users/DeleteNutritionItemEndpoint.cs) |

## Schemas

All live in [`Application/Modules/Users/Dtos/`](../src/TrailCompanion.Application/Modules/Users/Dtos/).

### Requests

| Schema | Description | Fields | File |
|---|---|---|---|
| <a id="registeruserrequest"></a>RegisterUserRequest | Username and password supplied when creating a new user account. | `username`, `password` | [RegisterUserRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RegisterUserRequest.cs) |
| <a id="loginuserrequest"></a>LoginUserRequest | Username and password credentials supplied when requesting an access token. | `username`, `password` | [LoginUserRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/LoginUserRequest.cs) |
| <a id="updateprofilerequest"></a>UpdateProfileRequest | Names to set; blank or null clears. | `firstName?`, `lastName?` | [UpdateProfileRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/UpdateProfileRequest.cs) |
| <a id="changepasswordrequest"></a>ChangePasswordRequest | Current password plus the new one. | `currentPassword`, `newPassword` | [ChangePasswordRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ChangePasswordRequest.cs) |
| <a id="deleteaccountrequest"></a>DeleteAccountRequest | Current password confirming the deletion. | `password` | [DeleteAccountRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/DeleteAccountRequest.cs) |
| <a id="joinracerequest"></a>JoinRaceRequest | The race PIN; spaces are ignored. | `pin` | [JoinRaceRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/JoinRaceRequest.cs) |
| <a id="createracerequest"></a>CreateRaceRequest | Race name plus optional carbs-per-hour target, which defaults to sixty. | `name`, `target?`, `startTime?` (`HH:mm:ss`), `raceDate?` (`YYYY-MM-DD`); on PUT, omitting `startTime` or `raceDate` clears it | [CreateRaceRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/CreateRaceRequest.cs) |
| <a id="segmentrequest"></a>SegmentRequest | Name, duration, optional end km (cumulative kilometre mark where the segment ends) and notes for a race segment. | `name`, `duration`, `endKm?`, `notes?` | [SegmentRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/SegmentRequest.cs) |
| <a id="itemrequest"></a>ItemRequest | Identifier of the nutrition library item to attach to a segment. | `nutritionItemId` | [ItemRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ItemRequest.cs) |
| <a id="nutritionitemrequest"></a>NutritionItemRequest | Name, type and optional size and macros for a nutrition library item. Every macro is nullable: blank means unknown, not zero. | `name`, `type`, `size?`, `carbs?`, `calories?`, `caffeine?`, `sodium?`, `potassium?`, `magnesium?`, `calcium?` | [NutritionItemRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/NutritionItemRequest.cs) |
| <a id="reorderrequest"></a>ReorderRequest | Every sibling id exactly once, in the wanted order. | `ids` | [ReorderRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ReorderRequest.cs) |
| <a id="createaidstationrequest"></a>CreateAidStationRequest | Segment the new aid station precedes, plus its name. | `segmentId`, `name` | [AidStationRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AidStationRequest.cs) |
| <a id="aidstationrequest"></a>AidStationRequest | Full replacement of an aid station's name, notes and kit list. | `name`, `notes?`, `kit?[]` (`label`, `certainty`: `Planned` or `Maybe`) | [AidStationRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AidStationRequest.cs) |
| <a id="kitentryrequest"></a>KitEntryRequest | One kit list entry on an aid station. | `label`, `certainty` | [AidStationRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AidStationRequest.cs) |
| <a id="moveaidstationrequest"></a>MoveAidStationRequest | Segment whose preceding gap the station moves to. | `segmentId` | [AidStationRequest.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AidStationRequest.cs) |

### Responses

| Schema | Description | Fields | File |
|---|---|---|---|
| <a id="appuserdto"></a>AppUserDto | User identifier, username, optional names and creation timestamp returned to the client. | `id`, `username`, `firstName?`, `lastName?`, `createdAtUtc` | [AppUserDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AppUserDto.cs) |
| <a id="accesstokendto"></a>AccessTokenDto | Signed JWT string plus its UTC expiry time for clients. | `accessToken`, `expiresAtUtc` | [AccessTokenDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AccessTokenDto.cs) |
| <a id="racedto"></a>RaceDto | Race summary with identifier, name, target and creation timestamp only. | `id`, `name`, `target`, `startTime?`, `createdAtUtc`, `raceDate?`, `role` (`Owner` or `Crew`) | [RaceDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RaceDto.cs) |
| <a id="racedetaildto"></a>RaceDetailDto | Race with nested segments, each carrying its own items list. | `id`, `name`, `target`, `startTime?`, `createdAtUtc`, `raceDate?`, `role`, `pin?` (owner only, null for crew), `segments[]` (`id`, `position`, `name`, `duration`, `endKm?`, `notes`, `items[]`, `aidStation?` (`id`, `name`, `notes`, `kit[]`, `items[]` of AidStationItemDto)) | [RaceDetailDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RaceDetailDto.cs) |
| <a id="crewmemberdto"></a>CrewMemberDto | One crew member of a race. | `appUserId`, `displayName`, `joinedAtUtc` | [CrewMemberDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/CrewMemberDto.cs) |
| <a id="racepindto"></a>RacePinDto | A race's join PIN. | `pin` | [RacePinDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/RacePinDto.cs) |
| <a id="segmentdto"></a>SegmentDto | Segment fields including its race identifier, without nested items. | `id`, `raceId`, `position`, `name`, `duration`, `endKm?`, `notes` | [SegmentDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/SegmentDto.cs) |
| <a id="itemdto"></a>ItemDto | Segment item row with its position and a flattened copy of the referenced library item's values. | `id`, `segmentId`, `nutritionItemId`, `position`, `name`, `type`, `size?`, `carbs?`, `calories?`, `caffeine?`, `sodium?`, `potassium?`, `magnesium?`, `calcium?` | [ItemDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/ItemDto.cs) |
| <a id="nutritionitemdto"></a>NutritionItemDto | One nutrition library item owned by the caller. | `id`, `name`, `type`, `size?`, `carbs?`, `calories?`, `caffeine?`, `sodium?`, `potassium?`, `magnesium?`, `calcium?` | [NutritionItemDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/NutritionItemDto.cs) |
| <a id="aidstationdto"></a>AidStationDto | Aid station without its items. | `id`, `segmentId`, `name`, `notes`, `kit[]` | [AidStationDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AidStationDto.cs) |
| <a id="aidstationitemdto"></a>AidStationItemDto | Aid station item row with its position and a flattened copy of the referenced library item's values. | `id`, `aidStationId`, `nutritionItemId`, `position`, `name`, `type`, `size?`, `carbs?`, `calories?`, `caffeine?`, `sodium?`, `potassium?`, `magnesium?`, `calcium?` | [AidStationItemDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/AidStationItemDto.cs) |
| <a id="kitentrydto"></a>KitEntryDto | One kit list entry. | `label`, `certainty` (`Planned` or `Maybe`) | [KitEntryDto.cs](../src/TrailCompanion.Application/Modules/Users/Dtos/KitEntryDto.cs) |

## Error responses

Failures return `ProblemDetails` with a `correlationId`: 400 validation, 401 unauthenticated or bad login, 404 missing or not owned, 403 login, refresh or logout from a foreign `Origin`, 409 duplicate username, duplicate nutrition item name or a library item still in use or a segment that already has an aid station, 429 rate limit (including more than 10 join attempts a minute).
