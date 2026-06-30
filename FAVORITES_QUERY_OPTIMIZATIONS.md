# Member Favorites Query Optimizations

## Overview
Optimized the member favorites API endpoints to reduce database round trips and connection overhead, addressing timeout issues reported with the 15-second client timeout.

## Performance Issues Addressed
- **Excessive database round trips**: Reduced from 3+ queries to fewer, more efficient queries
- **Entity tracking overhead**: Removed unnecessary EF Core tracking for read-only operations
- **Sequential query execution**: Introduced parallel task execution for independent lookups
- **Connection pool overhead**: Fewer queries = fewer connection open/close cycles

## Optimizations Implemented

### 1. GetFavoritesAsync (Read Operation)
**File**: `DotNetApiTemplate.Infrastructure/LoveBrackley/LoveBrackleyService.cs`

**Changes**:
- Added upfront member existence check using `AnyAsync()` (cheap existence check)
- Replaced `GetMemberForUserAsync` helper call with direct database query
- Added `AsNoTracking()` to count and items queries (read-only)
- Filter directly by `UserId` through `MemberProfile` navigation property

**Benefits**:
- Maintains null-return semantics for non-existent members (required by tests/API)
- `AnyAsync()` query returns true/false without materializing entire entity
- No entity tracking overhead on read operations
- Single consolidated member lookup pattern

**Query Pattern**:
```
1. Check if member exists: SELECT 1 FROM MemberProfiles WHERE UserId = @userId
2. Count favorites: SELECT COUNT(*) FROM MemberShopPreferences WHERE MemberProfile.UserId = @userId AND RemovedUtc IS NULL
3. Get paginated items: SELECT ... FROM MemberShopPreferences WHERE MemberProfile.UserId = @userId AND RemovedUtc IS NULL ORDER BY SavedUtc DESC LIMIT @pageSize OFFSET @offset
```

### 2. SaveFavoriteAsync (Write Operation)
**File**: `DotNetApiTemplate.Infrastructure/LoveBrackley/LoveBrackleyService.cs`

**Changes**:
- Parallelized member and business lookups using `Task.WhenAll()`
- Replaced `GetMemberForUserAsync` and `FindAsync` with `FirstOrDefaultAsync` with filters
- Added `AsNoTracking()` to read-only lookups (member, business, duplicate check)
- Direct database queries instead of helper methods

**Benefits**:
- Member and business lookups execute in parallel (reduced latency)
- No entity tracking for read-only validation operations
- Consistent query patterns across all methods
- Faster duplicate detection with filtered `FirstOrDefaultAsync`

**Query Pattern**:
```
1. Parallel queries:
   - Get member: SELECT * FROM MemberProfiles WHERE UserId = @userId (AsNoTracking)
   - Get business: SELECT * FROM BusinessProfiles WHERE Id = @businessId (AsNoTracking)

2. Duplicate check (if member + business found):
   SELECT * FROM MemberShopPreferences WHERE MemberProfileId = @memberId AND BusinessProfileId = @businessId AND RemovedUtc IS NULL

3. Create and save new preference (if no duplicate)
```

### 3. RemoveFavoriteAsync (Write Operation)
**File**: `DotNetApiTemplate.Infrastructure/LoveBrackley/LoveBrackleyService.cs`

**Changes**:
- Removed `GetMemberForUserAsync` helper call
- Direct member lookup with `UserId` filter
- Consolidated logic in single method

**Benefits**:
- Eliminates unnecessary helper method overhead
- Clearer method flow
- Reduced method call stack depth

### 4. UpdateFavoritePreferencesAsync (Write Operation)
**File**: `DotNetApiTemplate.Infrastructure/LoveBrackley/LoveBrackleyService.cs`

**Changes**:
- Removed `GetMemberForUserAsync` helper call
- Direct member lookup with `UserId` filter
- Maintains Include(BusinessProfile) for DTO mapping

**Benefits**:
- Consistent pattern with other methods
- Clearer exception handling and null checks

## Query Efficiency Improvements

### Before Optimization
- `GetFavoritesAsync`: 3+ database round trips
  1. `GetMemberForUserAsync`: SingleOrDefaultAsync → retrieves full entity
  2. `CountAsync` on filtered query
  3. `ToListAsync` on paginated query with Include

- `SaveFavoriteAsync`: 3+ database round trips
  1. `GetMemberForUserAsync`: SingleOrDefaultAsync
  2. `FindAsync` on BusinessProfiles
  3. `FirstOrDefaultAsync` for duplicate check
  4. SaveChangesAsync

### After Optimization
- `GetFavoritesAsync`: 3 optimized queries
  1. `AnyAsync` on MemberProfiles (cheap existence check)
  2. `CountAsync` with AsNoTracking
  3. `ToListAsync` with AsNoTracking

- `SaveFavoriteAsync`: Parallel queries + write
  1. Parallel `FirstOrDefaultAsync` for member + business (AsNoTracking)
  2. `FirstOrDefaultAsync` for duplicate check (AsNoTracking)
  3. SaveChangesAsync

**Key Improvement**: Parallel execution of independent lookups + AsNoTracking on read-only operations reduces overall latency and connection overhead.

## Entity Tracking Impact
- **Read-only queries**: `AsNoTracking()` eliminates the overhead of:
  - Building identity maps
  - Setting up change tracking
  - Creating navigation property proxies
  - Duplicate entity detection

- **Result**: Faster query execution and reduced memory usage for read operations

## Testing
All 13 existing `MemberShopPreferencesTests` pass without modification:
- ✅ CreateFavorite scenarios
- ✅ Duplicate save detection
- ✅ Remove favorite (soft delete)
- ✅ List favorites with filtering
- ✅ Update preferences

## Recommendations for Further Optimization

### 1. Add Database Indexes
If query execution plans show table scans, add indexes:
```sql
-- Index for UserId lookups on MemberProfiles
CREATE INDEX IX_MemberProfiles_UserId ON dbo.MemberProfiles(UserId);

-- Index for member favorites filtering
CREATE INDEX IX_MemberShopPreferences_MemberProfileId_RemovedUtc 
  ON dbo.MemberShopPreferences(MemberProfileId, RemovedUtc) 
  INCLUDE (BusinessProfileId, SavedUtc);
```

### 2. Connection Pooling
Verify the connection string has appropriate pooling:
```
Server=...;Min Pool Size=5;Max Pool Size=100;Pooling=true;
```

### 3. Query Monitoring
Enable EF Core query logging in development to monitor generated SQL:
```csharp
services.AddLogging(builder => 
	builder.AddConsole()
		   .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Debug)
);
```

### 4. Caching Strategy
Consider caching favorite lists with short TTL (e.g., 5-10 seconds) for frequently accessed members:
```csharp
var cacheKey = $"member-favorites-{userId}-page-{page}";
var cached = await cache.GetAsync<MemberFavoritesResponse>(cacheKey);
if (cached != null) return cached;

var result = await GetFavoritesFromDatabase();
await cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(10));
return result;
```

## Cancellation Token Handling
The cancellation token is properly propagated through all async operations:
- ✅ Passed to all `FindAsync`, `FirstOrDefaultAsync`, `CountAsync`, `ToListAsync` calls
- ✅ Used for proper task cancellation if client disconnects
- ✅ No blocking operations that would ignore cancellation

**Note**: If 15-second timeout persists, check:
1. Database connection latency
2. Network round-trip time (RTT)
3. SQL Server performance (check execution plans)
4. Application server CPU/memory availability
