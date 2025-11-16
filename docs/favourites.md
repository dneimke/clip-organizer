## Favourites

This document describes how to use the Favourites feature and how it is implemented across the backend and frontend.

### Using Favourites (for users)
- Toggle favourite on a clip:
  - On any clip card or list row, click the heart icon.
  - Filled heart = favourited; outline heart = not favourited.
- Filter to only favourites:
  - In the Browse page’s search bar, check “Favourites only” to show only clips marked as favourites.
- Works with other filters:
  - “Favourites only” combines with search text, tags, subfolders, and sorting.


### Solution architecture (overview)
- Global flag per clip:
  - A boolean `IsFavorite` on each clip. No per-user scope.
- Read/write API:
  - GET `/api/clips?favoriteOnly=true` filters results to favourites.
  - PATCH `/api/clips/{id}/favorite` toggles a single clip’s favourite state.
- Frontend integration:
  - Search bar exposes “Favourites only” filter.
  - Each clip presents a heart button for optimistic toggle.


### Backend design

#### Data model
- Entity: `Clip` adds `IsFavorite: bool` (default false).
  - File: `backend/ClipOrganizer.Api/Models/Clip.cs`
- EF configuration default:
  - File: `backend/ClipOrganizer.Api/Data/ClipDbContext.cs`
  - `entity.Property(e => e.IsFavorite).HasDefaultValue(false);`
- SQLite column bootstrap (for existing DBs):
  - File: `backend/ClipOrganizer.Api/Program.cs`
  - Adds `IsFavorite INTEGER NOT NULL DEFAULT 0` if missing.

#### API surface
- Filter favourites in listing:
  - Endpoint: `GET /api/clips`
  - Query param: `favoriteOnly=true|false`
  - Implementation: `ClipsController.GetClips(...)` adds `query = query.Where(c => c.IsFavorite)` when `favoriteOnly` is true.
- Toggle a favourite:
  - Endpoint: `PATCH /api/clips/{id}/favorite`
  - Request body DTO: `{ "isFavorite": boolean }`
  - Implementation: sets `clip.IsFavorite` and returns updated `ClipDto`.
- DTO updates:
  - `ClipDto` includes `IsFavorite`.
  - `ToggleFavoriteDto { bool IsFavorite }`.

#### Tests
- File: `backend/ClipOrganizer.Api.Tests/Controllers/ClipsControllerTests.cs`
  - Ensures `favoriteOnly` filter returns only favourites.
  - Ensures PATCH toggle sets and unsets `IsFavorite` as expected.


### Frontend design

#### Types and API
- Type: `Clip` includes `isFavorite: boolean`.
  - File: `frontend/types/index.ts`
- Fetch with favourite filter:
  - `getClips(..., favoriteOnly?: boolean)` appends `favoriteOnly=true` when set.
  - File: `frontend/lib/api/clips.ts`
- Toggle favourite:
  - `setFavorite(clipId: number, isFavorite: boolean)` calls `PATCH /api/clips/{id}/favorite`.
  - File: `frontend/lib/api/clips.ts`

#### UI integration
- “Favourites only” filter control:
  - Component: `frontend/components/SearchBar.tsx`
  - Props: `favoriteOnly`, `onFavoriteOnlyChange`
  - Consumed in: `frontend/app/browse/page.tsx` to update query and reload clips.
- Heart icon on clip items:
  - Components: `frontend/components/ClipCard.tsx`, `frontend/components/ClipListItem.tsx`
  - Behaviour: Optimistic UI update; revert if API call fails.
  - Visuals: Filled heart for favourited, outline for not favourited.


### Error handling and UX notes
- Optimistic toggle updates the heart immediately; on failure, it reverts.
- The “Favourites only” filter composes with other filters; results reflect all active filters.
- No per-user state; favourites are global.


### Future enhancements (optional)
- Per-user favourites behind authentication.
- Bulk favourite/unfavourite actions.
- Persist “Favourites only” preference across sessions.


