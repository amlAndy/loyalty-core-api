# Member Favorite Businesses - API Usage Guide

## Overview

The Member Favorite Businesses feature allows authenticated members to save, manage, and track their preferred participating businesses.

---

## Base URL
```
/api/members/me/favorites
```

**Authentication:** Required (Bearer Token)

---

## Endpoints

### 1. Save a Favorite Business

**Endpoint:** `POST /api/members/me/favorites/{businessId}`

**Description:** Save a business as a favorite. If already saved, returns the existing preference.

**Path Parameters:**
- `businessId` (Guid) - The business to favorite

**Request Body:**
```json
{
  "notes": "Great local cafe, love their coffee!"
}
```

**Response (201 Created):**
```json
{
  "data": {
	"preferenceId": "550e8400-e29b-41d4-a716-446655440000",
	"businessId": "11111111-1111-1111-1111-111111111111",
	"name": "Coffee Shop",
	"category": "Cafe",
	"locationName": "Main Street",
	"town": "Brackley",
	"featured": true,
	"receiveOfferNotifications": true,
	"receiveUpdateNotifications": true,
	"notes": "Great local cafe, love their coffee!",
	"savedUtc": "2026-06-07T17:45:30.0000000Z"
  }
}
```

**Error Responses:**
- `404 Not Found` - Business or member not found

---

### 2. Get List of Favorite Businesses

**Endpoint:** `GET /api/members/me/favorites?page=1&pageSize=20`

**Description:** Retrieve all active favorite businesses for the authenticated member.

**Query Parameters:**
- `page` (int, default: 1) - Page number (1-based)
- `pageSize` (int, default: 20) - Items per page (1-100)

**Response (200 OK):**
```json
{
  "data": {
	"items": [
	  {
		"preferenceId": "550e8400-e29b-41d4-a716-446655440000",
		"businessId": "11111111-1111-1111-1111-111111111111",
		"name": "Coffee Shop",
		"category": "Cafe",
		"locationName": "Main Street",
		"town": "Brackley",
		"featured": true,
		"receiveOfferNotifications": true,
		"receiveUpdateNotifications": true,
		"notes": "Great local cafe",
		"savedUtc": "2026-06-07T17:45:30.0000000Z"
	  },
	  {
		"preferenceId": "660e8400-e29b-41d4-a716-446655440001",
		"businessId": "22222222-2222-2222-2222-222222222222",
		"name": "Restaurant",
		"category": "Food",
		"locationName": "High Street",
		"town": "Brackley",
		"featured": false,
		"receiveOfferNotifications": true,
		"receiveUpdateNotifications": false,
		"notes": "Best curry in town",
		"savedUtc": "2026-06-06T15:30:00.0000000Z"
	  }
	],
	"page": 1,
	"pageSize": 20,
	"totalCount": 2
  }
}
```

**Error Responses:**
- `404 Not Found` - Member not found

---

### 3. Remove a Favorite Business

**Endpoint:** `DELETE /api/members/me/favorites/{businessId}`

**Description:** Remove a business from favorites (soft delete). The preference is marked as removed but not deleted from the database.

**Path Parameters:**
- `businessId` (Guid) - The business to unfavorite

**Response (204 No Content)** - Success, no response body

**Error Responses:**
- `404 Not Found` - Favorite not found or member not found

---

### 4. Update Favorite Preferences

**Endpoint:** `PUT /api/members/me/favorites/{businessId}/preferences`

**Description:** Update notification preferences for a favorite business.

**Path Parameters:**
- `businessId` (Guid) - The favorite business to update

**Request Body:**
```json
{
  "receiveOfferNotifications": false,
  "receiveUpdateNotifications": true,
  "notes": "Updated notes about this business"
}
```

**Response (200 OK):**
```json
{
  "data": {
	"preferenceId": "550e8400-e29b-41d4-a716-446655440000",
	"businessId": "11111111-1111-1111-1111-111111111111",
	"name": "Coffee Shop",
	"category": "Cafe",
	"locationName": "Main Street",
	"town": "Brackley",
	"featured": true,
	"receiveOfferNotifications": false,
	"receiveUpdateNotifications": true,
	"notes": "Updated notes about this business",
	"savedUtc": "2026-06-07T17:45:30.0000000Z"
  }
}
```

**Error Responses:**
- `404 Not Found` - Favorite not found or member not found

---

## Common Response Formats

### Success Response Structure
```json
{
  "data": {
	// Response data object
  }
}
```

### Error Response Structure
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Not Found",
  "status": 404,
  "traceId": "0HN1GDNEV6R5V:00000001"
}
```

---

## Data Models

### FavoritedBusinessDto
```csharp
{
  Guid PreferenceId,
  Guid BusinessId,
  string Name,
  string Category,
  string LocationName,
  string Town,
  bool Featured,
  bool ReceiveOfferNotifications,
  bool ReceiveUpdateNotifications,
  string? Notes,
  DateTimeOffset SavedUtc
}
```

### MemberFavoritesResponse
```csharp
{
  IReadOnlyList<FavoritedBusinessDto> Items,
  int Page,
  int PageSize,
  int TotalCount
}
```

---

## Usage Examples

### JavaScript/TypeScript (Fetch API)

#### Save a Favorite
```javascript
const response = await fetch('/api/members/me/favorites/11111111-1111-1111-1111-111111111111', {
  method: 'POST',
  headers: {
	'Authorization': `Bearer ${token}`,
	'Content-Type': 'application/json'
  },
  body: JSON.stringify({
	notes: 'Great local cafe'
  })
});

const result = await response.json();
console.log(result.data);
```

#### Get Favorites
```javascript
const response = await fetch('/api/members/me/favorites?page=1&pageSize=10', {
  method: 'GET',
  headers: {
	'Authorization': `Bearer ${token}`
  }
});

const result = await response.json();
console.log(result.data.items);
```

#### Remove a Favorite
```javascript
const response = await fetch('/api/members/me/favorites/11111111-1111-1111-1111-111111111111', {
  method: 'DELETE',
  headers: {
	'Authorization': `Bearer ${token}`
  }
});

console.log(response.status); // 204 if successful
```

#### Update Preferences
```javascript
const response = await fetch('/api/members/me/favorites/11111111-1111-1111-1111-111111111111/preferences', {
  method: 'PUT',
  headers: {
	'Authorization': `Bearer ${token}`,
	'Content-Type': 'application/json'
  },
  body: JSON.stringify({
	receiveOfferNotifications: false,
	receiveUpdateNotifications: true,
	notes: 'Updated notes'
  })
});

const result = await response.json();
console.log(result.data);
```

### cURL

#### Save a Favorite
```bash
curl -X POST http://localhost:5000/api/members/me/favorites/11111111-1111-1111-1111-111111111111 \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notes": "Great local cafe"}'
```

#### Get Favorites
```bash
curl -X GET "http://localhost:5000/api/members/me/favorites?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Remove a Favorite
```bash
curl -X DELETE http://localhost:5000/api/members/me/favorites/11111111-1111-1111-1111-111111111111 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Business Rules

1. **Duplicate Prevention**: Attempting to save an already-favorited business returns the existing preference
2. **Soft Deletion**: Removing a favorite sets `RemovedUtc` rather than permanently deleting
3. **Member Association**: Favorites are always tied to the authenticated member (via JWT)
4. **Pagination**: Default page size is 20, maximum is 100
5. **Notification Defaults**: New favorites default to receiving both offer and update notifications
6. **Notes Optional**: The notes field is optional and can be empty

---

## Error Codes

| Status | Meaning |
|--------|---------|
| 200 | Success (GET, PUT) |
| 201 | Created (POST) |
| 204 | No Content (DELETE success) |
| 400 | Bad Request (invalid input) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found (business/member/preference not found) |
| 500 | Internal Server Error |

---

## Rate Limiting

Currently no rate limiting is implemented. Consider implementing for production use.

---

## Future Enhancements

- [ ] Bulk operations (add/remove multiple favorites at once)
- [ ] Favorite categories/collections
- [ ] Favorite recommendations based on purchase history
- [ ] Sharing favorites with friends
- [ ] Exporting favorite list
- [ ] Integration with offers and promotions
