<!-- 16cbad1a-cd88-4297-9e16-bad71e46b101 f128839c-ec1b-4ab8-bc8b-165004204073 -->
# RFID Lab Access Control System Implementation

## Overview

Implement RFID-based lab entry/exit tracking with automatic door control. The system tracks whether users are inside or outside the lab (not specific rooms), manages door open/close states, and provides card registration for admin users.

## Key Requirements

- Track user location: inside/outside lab (room-specific)
- Door status stored in separate DoorState model
- Door auto-resets immediately (no timing delay)
- RFID card registration: admin-only endpoint in RoomsController
- Reader identification: only `readerUid` sent, system looks up room from `rfid_readers` table

## Implementation Steps

### 1. Create DoorState Model

**File**: `ailab-super-app/Models/DoorState.cs` (new)

Create a new model to track door open/close status:

- `Id` (Guid, primary key)
- `RoomId` (Guid, foreign key to Room)
- `IsOpen` (bool, door status)
- `LastUpdatedAt` (DateTime)
- Navigation property to Room

### 2. Update AppDbContext

**File**: `ailab-super-app/Data/AppDbContext.cs`

Add DoorState DbSet and configure the entity:

- Add `public DbSet<DoorState> DoorStates { get; set; }`
- Configure entity in `OnModelCreating`: table name, indexes, foreign key relationship

### 3. Create DTOs for RFID Operations

**Files**: Create new DTOs in `ailab-super-app/DTOs/Rfid/` directory

**CardScanRequestDto.cs**:

- `CardUid` (string)
- `ReaderUid` (string)

**CardScanResponseDto.cs**:

- `Success` (bool)
- `Message` (string)
- `DoorShouldOpen` (bool)
- `UserName` (string, optional)
- `IsEntry` (bool) - true for entry, false for exit

**RegisterCardRequestDto.cs**:

- `UserId` (Guid)
- `CardUid` (string)

**DoorStatusResponseDto.cs**:

- `RoomId` (Guid)
- `RoomName` (string)
- `IsOpen` (bool)
- `LastUpdatedAt` (DateTime)

### 4. Create IRoomAccessService Interface

**File**: `ailab-super-app/Services/Interfaces/IRoomAccessService.cs` (new)

Define service methods:

- `Task<CardScanResponseDto> ProcessCardScanAsync(CardScanRequestDto request)`
- `Task<DoorStatusResponseDto> GetDoorStatusAsync(Guid roomId)`
- `Task ResetDoorStatusAsync(Guid roomId)`
- `Task<RfidCard> RegisterCardAsync(RegisterCardRequestDto request, Guid registeredBy)`

### 5. Implement RoomAccessService

**File**: `ailab-super-app/Services/RoomAccessService.cs` (new)

Implement the core business logic:

**ProcessCardScanAsync logic**:

1. Validate `readerUid` exists in `rfid_readers` table
2. Get `RoomId` and `ReaderLocation` from reader
3. Validate `cardUid` exists in `rfid_cards` table and get associated `UserId`
4. Check if user is currently in lab (exists in `lab_current_occupancy`)
5. Apply door logic based on table:

   - User outside + Outside reader → open door, add to occupancy
   - User outside + Inside reader → don't open, add to occupancy
   - User inside + Outside reader → don't open, remove from occupancy
   - User inside + Inside reader → open door, remove from occupancy

6. Update `DoorState.IsOpen` to true if door should open
7. Create `RoomAccess` log entry
8. Create `LabEntry` record
9. Update `RfidCard.LastUsed`
10. **IMPORTANT**: If door was opened (IsOpen set to true), immediately reset it back to false after returning response
11. Return response with door action

**GetDoorStatusAsync**: Query current door state for a room

**ResetDoorStatusAsync**: Set `DoorState.IsOpen = false` for a room

**RegisterCardAsync**:

- Validate user exists
- Check if card already exists (update or create)
- Associate card with user
- Return RfidCard entity

### 6. Update RoomsController

**File**: `ailab-super-app/Controllers/RoomsController.cs`

Add endpoints:

**POST /api/rooms/card-scan** (no auth - called by RFID hardware):

- Accepts `CardScanRequestDto`
- Calls `ProcessCardScanAsync`
- Returns `CardScanResponseDto`

**GET /api/rooms/{roomId}/door-status** (no auth - called by door controller):

- Returns current door status
- Calls `GetDoorStatusAsync`

**POST /api/rooms/register-card** (admin only):

- Accepts `RegisterCardRequestDto`
- Validates admin role with `[Authorize(Roles = "Admin")]`
- Calls `RegisterCardAsync`
- Returns success message

### 7. Register Service in Program.cs

**File**: `ailab-super-app/Program.cs`

Add service registration:

```csharp
builder.Services.AddScoped<IRoomAccessService, RoomAccessService>();
```

### 8. Create Database Migration

Run EF Core migration command to create `door_states` table:

```bash
dotnet ef migrations add AddDoorStateTable
```

## Data Flow Examples

### Entry Flow (User Outside → Scans Outside Reader)

1. RFID reader sends: `{ "cardUid": "ABC123", "readerUid": "READER-OUT-01" }`
2. System looks up reader → finds RoomId and Location=Outside
3. System looks up card → finds UserId
4. System checks occupancy → user NOT in lab
5. System sets DoorState.IsOpen = true
6. System adds user to lab_current_occupancy
7. System creates RoomAccess and LabEntry records
8. Response: `{ "success": true, "doorShouldOpen": true, "isEntry": true }`

### Exit Flow (User Inside → Scans Inside Reader)

1. RFID reader sends: `{ "cardUid": "ABC123", "readerUid": "READER-IN-01" }`
2. System looks up reader → finds RoomId and Location=Inside
3. System checks occupancy → user IS in lab
4. System sets DoorState.IsOpen = true
5. System removes user from lab_current_occupancy
6. System creates RoomAccess and LabEntry records
7. Response: `{ "success": true, "doorShouldOpen": true, "isEntry": false }`

### Door Reset Flow

1. Door controller polls: GET `/api/rooms/{roomId}/door-status`
2. If `isOpen == true`, door controller opens physical door
3. After sending response as isOpen == True, reset state
4. System sets DoorState.IsOpen = false
5. Door controller closes physical door

## Files to Create

- `ailab-super-app/Models/DoorState.cs`
- `ailab-super-app/DTOs/Rfid/CardScanRequestDto.cs`
- `ailab-super-app/DTOs/Rfid/CardScanResponseDto.cs`
- `ailab-super-app/DTOs/Rfid/RegisterCardRequestDto.cs`
- `ailab-super-app/DTOs/Rfid/DoorStatusResponseDto.cs`
- `ailab-super-app/Services/Interfaces/IRoomAccessService.cs`
- `ailab-super-app/Services/RoomAccessService.cs`

## Files to Modify

- `ailab-super-app/Data/AppDbContext.cs` (add DoorState DbSet and configuration)
- `ailab-super-app/Controllers/RoomsController.cs` (add RFID endpoints)
- `ailab-super-app/Program.cs` (register IRoomAccessService)

### To-dos

- [ ] Create DoorState model with RoomId, IsOpen, and LastUpdatedAt fields
- [ ] Create DTOs for RFID operations (CardScanRequest, CardScanResponse, RegisterCardRequest, DoorStatusResponse)
- [ ] Add DoorState DbSet and entity configuration to AppDbContext
- [ ] Create IRoomAccessService interface with card scanning and door management methods
- [ ] Implement RoomAccessService with complete door logic and user tracking
- [ ] Add RFID endpoints to RoomsController (card-scan, door-status, reset-door, register-card)
- [ ] Register IRoomAccessService in Program.cs dependency injection
- [ ] Generate and apply EF Core migration for DoorState table