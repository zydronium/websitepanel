USE [WebsitePanel]
GO
/****** Object:  StoredProcedure [dbo].[UpdatePackage]    Script Date: 15-01-2015 11:40:16 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
ALTER TABLE [dbo].[Packages] ADD DefaultTopPackage bit
GO


ALTER PROCEDURE [dbo].[GetMyPackages]
(
	@ActorID int,
	@UserID int
)
AS

-- check rights
IF dbo.CheckActorUserRights(@ActorID, @UserID) = 0
RAISERROR('You are not allowed to access this account', 16, 1)

SELECT
	P.PackageID,
	P.ParentPackageID,
	P.PackageName,
	P.StatusID,
	P.PlanID,
	P.PurchaseDate,
	
	dbo.GetItemComments(P.PackageID, 'PACKAGE', @ActorID) AS Comments,
	
	-- server
	ISNULL(P.ServerID, 0) AS ServerID,
	ISNULL(S.ServerName, 'None') AS ServerName,
	ISNULL(S.Comments, '') AS ServerComments,
	ISNULL(S.VirtualServer, 1) AS VirtualServer,
	
	-- hosting plan
	HP.PlanName,
	
	-- user
	P.UserID,
	U.Username,
	U.FirstName,
	U.LastName,
	U.FullName,
	U.RoleID,
	U.Email,

	P.DefaultTopPackage
FROM Packages AS P
INNER JOIN UsersDetailed AS U ON P.UserID = U.UserID
LEFT OUTER JOIN Servers AS S ON P.ServerID = S.ServerID
LEFT OUTER JOIN HostingPlans AS HP ON P.PlanID = HP.PlanID
WHERE P.UserID = @UserID
RETURN
GO
ALTER PROCEDURE [dbo].[GetPackages]
(
	@ActorID int,
	@UserID int
)
AS

SELECT
	P.PackageID,
	P.ParentPackageID,
	P.PackageName,
	P.StatusID,
	P.PurchaseDate,
	
	-- server
	ISNULL(P.ServerID, 0) AS ServerID,
	ISNULL(S.ServerName, 'None') AS ServerName,
	ISNULL(S.Comments, '') AS ServerComments,
	ISNULL(S.VirtualServer, 1) AS VirtualServer,
	
	-- hosting plan
	P.PlanID,
	HP.PlanName,
	
	-- user
	P.UserID,
	U.Username,
	U.FirstName,
	U.LastName,
	U.RoleID,
	U.Email,

	P.DefaultTopPackage
FROM Packages AS P
INNER JOIN Users AS U ON P.UserID = U.UserID
INNER JOIN Servers AS S ON P.ServerID = S.ServerID
INNER JOIN HostingPlans AS HP ON P.PlanID = HP.PlanID
WHERE
	P.UserID = @UserID	
RETURN

GO
GO

ALTER PROCEDURE [dbo].[GetPackage]
(
	@PackageID int,
	@ActorID int
)
AS

-- Note: ActorID is not verified
-- check both requested and parent package

SELECT
	P.PackageID,
	P.ParentPackageID,
	P.UserID,
	P.PackageName,
	P.PackageComments,
	P.ServerID,
	P.StatusID,
	P.PlanID,
	P.PurchaseDate,
	P.OverrideQuotas,
	P.DefaultTopPackage
FROM Packages AS P
WHERE P.PackageID = @PackageID
RETURN

GO

ALTER PROCEDURE [dbo].[UpdatePackage]
(
	@ActorID int,
	@PackageID int,
	@PackageName nvarchar(300),
	@PackageComments ntext,
	@StatusID int,
	@PlanID int,
	@PurchaseDate datetime,
	@OverrideQuotas bit,
	@QuotasXml ntext,
	@DefaultTopPackage bit
)
AS

-- check rights
IF dbo.CheckActorPackageRights(@ActorID, @PackageID) = 0
RAISERROR('You are not allowed to access this package', 16, 1)

BEGIN TRAN

DECLARE @ParentPackageID int
DECLARE @OldPlanID int

SELECT @ParentPackageID = ParentPackageID, @OldPlanID = PlanID FROM Packages
WHERE PackageID = @PackageID

-- update package
UPDATE Packages SET
	PackageName = @PackageName,
	PackageComments = @PackageComments,
	StatusID = @StatusID,
	PlanID = @PlanID,
	PurchaseDate = @PurchaseDate,
	OverrideQuotas = @OverrideQuotas,
	DefaultTopPackage = @DefaultTopPackage
WHERE
	PackageID = @PackageID

-- update quotas (if required)
EXEC UpdatePackageQuotas @ActorID, @PackageID, @QuotasXml

-- check resulting quotas
DECLARE @ExceedingQuotas AS TABLE (QuotaID int, QuotaName nvarchar(50), QuotaValue int)

-- check exceeding quotas if plan has been changed
IF (@OldPlanID <> @PlanID) OR (@OverrideQuotas = 1)
BEGIN
	INSERT INTO @ExceedingQuotas
	SELECT * FROM dbo.GetPackageExceedingQuotas(@ParentPackageID) WHERE QuotaValue > 0
END

SELECT * FROM @ExceedingQuotas

IF EXISTS(SELECT * FROM @ExceedingQuotas)
BEGIN
	ROLLBACK TRAN
	RETURN
END


COMMIT TRAN
RETURN