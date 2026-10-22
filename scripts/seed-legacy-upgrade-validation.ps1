param(
	[string]$Server = "(localdb)\MSSQLLocalDB",
	[string]$Database = "LOCALDB_STAFFACCESS_UPGRADE_VALIDATION_20261022",
	[int]$ConnectTimeoutSeconds = 15,
	[int]$CommandTimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'

$connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=$ConnectTimeoutSeconds;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$transaction = $null

try {
	$connection.Open()

	$verify = $connection.CreateCommand()
	$verify.CommandTimeout = $CommandTimeoutSeconds
	$verify.CommandText = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName"
	$reader = $verify.ExecuteReader()
	[void]$reader.Read()
	$resolvedServer = $reader.GetString(0)
	$resolvedDb = $reader.GetString(1)
	$reader.Close()

	Write-Host "Connected server: $resolvedServer"
	Write-Host "Connected database: $resolvedDb"

	if ($resolvedDb -ne $Database) {
		throw "Connected database '$resolvedDb' does not match expected '$Database'."
	}

	$preCheck = $connection.CreateCommand()
	$preCheck.CommandTimeout = $CommandTimeoutSeconds
	$preCheck.CommandText = @"
SELECT
	(SELECT COUNT(*) FROM AspNetUsers) AS AspNetUsersCount,
	(SELECT COUNT(*) FROM BusinessProfiles) AS BusinessProfilesCount,
	(SELECT COUNT(*) FROM MemberProfiles) AS MemberProfilesCount,
	(SELECT COUNT(*) FROM BusinessStaffAccesses) AS BusinessStaffAccessesCount,
	(SELECT COUNT(*) FROM LoyaltyTransactions) AS LoyaltyTransactionsCount;
"@

	$preReader = $preCheck.ExecuteReader()
	[void]$preReader.Read()
	$existingUsers = $preReader.GetInt32(0)
	$existingBusinesses = $preReader.GetInt32(1)
	$existingMembers = $preReader.GetInt32(2)
	$existingStaff = $preReader.GetInt32(3)
	$existingTransactions = $preReader.GetInt32(4)
	$preReader.Close()

	Write-Host "Existing rows before seed: AspNetUsers=$existingUsers, BusinessProfiles=$existingBusinesses, MemberProfiles=$existingMembers, BusinessStaffAccesses=$existingStaff, LoyaltyTransactions=$existingTransactions"

	if ($existingUsers -ne 0 -or $existingBusinesses -ne 0 -or $existingMembers -ne 0 -or $existingStaff -ne 0 -or $existingTransactions -ne 0) {
		throw "Target database is not empty. Refusing to seed to avoid duplicate/partial state."
	}

	$transaction = $connection.BeginTransaction()

	$seed = $connection.CreateCommand()
	$seed.Transaction = $transaction
	$seed.CommandTimeout = $CommandTimeoutSeconds
	$seed.CommandText = @"
SET NOCOUNT ON;
DECLARE @now datetimeoffset = SYSUTCDATETIME();

INSERT INTO AspNetUsers (Id, DisplayName, Status, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES
('owner-legacy','Owner Legacy','Active','owner.legacy@example.com','OWNER.LEGACY@EXAMPLE.COM','owner.legacy@example.com','OWNER.LEGACY@EXAMPLE.COM',1,0,0,0,0),
('member-legacy','Member Legacy','Active','member.legacy@example.com','MEMBER.LEGACY@EXAMPLE.COM','member.legacy@example.com','MEMBER.LEGACY@EXAMPLE.COM',1,0,0,0,0),
('staff-active','Staff Active','Active','staff.active@example.com','STAFF.ACTIVE@EXAMPLE.COM','staff.active@example.com','STAFF.ACTIVE@EXAMPLE.COM',1,0,0,0,0),
('staff-held','Staff Held','Active','staff.held@example.com','STAFF.HELD@EXAMPLE.COM','staff.held@example.com','STAFF.HELD@EXAMPLE.COM',1,0,0,0,0),
('staff-reset','Staff Reset','Active','staff.reset@example.com','STAFF.RESET@EXAMPLE.COM','staff.reset@example.com','STAFF.RESET@EXAMPLE.COM',1,0,0,0,0),
('staff-unknown','Staff Unknown','Active','staff.unknown@example.com','STAFF.UNKNOWN@EXAMPLE.COM','staff.unknown@example.com','STAFF.UNKNOWN@EXAMPLE.COM',1,0,0,0,0),
('staff-ambiguous','Staff Ambiguous','Active','staff.ambiguous@example.com','STAFF.AMBIGUOUS@EXAMPLE.COM','staff.ambiguous@example.com','STAFF.AMBIGUOUS@EXAMPLE.COM',1,0,0,0,0);

INSERT INTO BusinessProfiles (Id, OwnerUserId, Name, BrandReference, Category, Description, LocationName, AddressLine1, Town, Postcode, ContactEmail, PrimaryContactName, TradingHoursSummary, ParticipationStatus, Featured, CreatedUtc, UpdatedUtc, BusinessCategoryId, TownId)
VALUES
('22222222-2222-2222-2222-222222222281','owner-legacy','Legacy Business A','LBA','Retail','Legacy A','A Site','1 A Street','Brackley','NN13 7AA','a@example.com','Owner Legacy','Mon-Fri','Active',0,@now,@now,'20000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000001'),
('22222222-2222-2222-2222-222222222282','owner-legacy','Legacy Business B','LBB','Retail','Legacy B','B Site','2 B Street','Brackley','NN13 7AB','b@example.com','Owner Legacy','Mon-Fri','Active',0,@now,@now,'20000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000001');

INSERT INTO MemberProfiles (Id, UserId, DisplayName, Email, MobileNumber, HomeArea, PublicMemberReference, CardCode, Status, PointsBalance, MarketingConsent, EmailUpdatesPreference, LocalOffersPreference, CardReminderPreference, CreatedUtc, UpdatedUtc)
VALUES ('11111111-1111-1111-1111-111111111281','member-legacy','Member Legacy','member.legacy@example.com',NULL,NULL,'MBR-LEG-1','CARD-LEG-1','Active',1000,1,1,1,1,@now,@now);

INSERT INTO BusinessStaffAccesses (Id, BusinessProfileId, UserId, DisplayName, BusinessAccessRole, PinHash, PinStatus, LastActivitySummary, CreatedUtc, UpdatedUtc)
VALUES
('44444444-4444-4444-4444-444444444481','22222222-2222-2222-2222-222222222281','staff-active','Staff Active','Staff','hash-active','Active','Legacy active linked',@now,@now),
('44444444-4444-4444-4444-444444444482','22222222-2222-2222-2222-222222222281','staff-held','Staff Held','Staff','hash-held','Held','Legacy held',@now,@now),
('44444444-4444-4444-4444-444444444483','22222222-2222-2222-2222-222222222281','staff-reset','Staff Reset','Staff','hash-reset','ResetRequired','Legacy reset required',@now,@now),
('44444444-4444-4444-4444-444444444484','22222222-2222-2222-2222-222222222281',NULL,'Staff Unlinked','Staff','hash-unlinked','Active','Legacy unlinked active pin',@now,@now),
('44444444-4444-4444-4444-444444444485','22222222-2222-2222-2222-222222222281','staff-unknown','Staff Unknown','Staff','hash-unknown','UnknownState','Legacy unknown state',@now,@now),
('44444444-4444-4444-4444-444444444486','22222222-2222-2222-2222-222222222281','staff-ambiguous','Staff Ambiguous A','Staff','hash-ambig-a','Active','Legacy ambiguous A',@now,@now),
('44444444-4444-4444-4444-444444444487','22222222-2222-2222-2222-222222222282','staff-ambiguous','Staff Ambiguous B','Staff','hash-ambig-b','Active','Legacy ambiguous B',@now,@now);

INSERT INTO LoyaltyTransactions (Id, MemberProfileId, BusinessProfileId, BusinessLocationId, StaffAccessId, TransactionType, Points, PurchaseAmount, RedeemedValue, Status, Reference, Notes, CreatedUtc)
VALUES ('66666666-6666-6666-6666-666666666681','11111111-1111-1111-1111-111111111281','22222222-2222-2222-2222-222222222281',NULL,'44444444-4444-4444-4444-444444444481','Redeem',100,1000,100,'Completed','TX-LEG-001','Legacy tx reference',@now);
"@

	[void]$seed.ExecuteNonQuery()

	$counts = $connection.CreateCommand()
	$counts.Transaction = $transaction
	$counts.CommandTimeout = $CommandTimeoutSeconds
	$counts.CommandText = @"
SELECT
	(SELECT COUNT(*) FROM AspNetUsers) AS AspNetUsersCount,
	(SELECT COUNT(*) FROM BusinessProfiles) AS BusinessProfilesCount,
	(SELECT COUNT(*) FROM MemberProfiles) AS MemberProfilesCount,
	(SELECT COUNT(*) FROM BusinessStaffAccesses) AS BusinessStaffAccessesCount,
	(SELECT COUNT(*) FROM LoyaltyTransactions) AS LoyaltyTransactionsCount;
"@

	$countReader = $counts.ExecuteReader()
	[void]$countReader.Read()
	$users = $countReader.GetInt32(0)
	$businesses = $countReader.GetInt32(1)
	$members = $countReader.GetInt32(2)
	$staff = $countReader.GetInt32(3)
	$transactions = $countReader.GetInt32(4)
	$countReader.Close()

	$transaction.Commit()
	$transaction = $null

	Write-Host "Seed committed successfully."
	Write-Host "Inserted row counts: AspNetUsers=$users, BusinessProfiles=$businesses, MemberProfiles=$members, BusinessStaffAccesses=$staff, LoyaltyTransactions=$transactions"
}
catch {
	if ($transaction -ne $null) {
		try { $transaction.Rollback() } catch {}
	}

	Write-Error "Legacy seed failed: $($_.Exception.GetType().FullName): $($_.Exception.Message)"
	if ($_.Exception.InnerException) {
		Write-Error "Inner exception: $($_.Exception.InnerException.GetType().FullName): $($_.Exception.InnerException.Message)"
	}
	throw
}
finally {
	if ($connection.State -ne [System.Data.ConnectionState]::Closed) {
		$connection.Close()
	}
	$connection.Dispose()
}
