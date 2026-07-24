
BEGIN TRANSACTION
GO
CREATE TABLE TaskMan.Tmp_Person
	(
	PersonId int NOT NULL IDENTITY (1, 1),
	Name nvarchar(100) NULL,
	IsResource bit NULL,
	IsActive bit NULL,
	DBLogin nvarchar(50) NULL,
	UserType nvarchar(20) NULL,
	Colour nvarchar(25) NULL,
	ModifiedBy nvarchar(50) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
GO
ALTER TABLE TaskMan.Tmp_Person SET (LOCK_ESCALATION = TABLE)
GO
GRANT DELETE ON TaskMan.Tmp_Person TO eabs_pp  AS dbo
GO
GRANT INSERT ON TaskMan.Tmp_Person TO eabs_pp  AS dbo
GO
GRANT SELECT ON TaskMan.Tmp_Person TO eabs_pp  AS dbo
GO
GRANT UPDATE ON TaskMan.Tmp_Person TO eabs_pp  AS dbo
GO
SET IDENTITY_INSERT TaskMan.Tmp_Person ON
GO
IF EXISTS(SELECT * FROM TaskMan.Person)
	 EXEC('INSERT INTO TaskMan.Tmp_Person (PersonId, Name, IsResource, DBLogin, UserType, Colour, ModifiedBy, ModifiedTime)
		SELECT PersonId, Name, IsResource, DBLogin, UserType, Colour, ModifiedBy, ModifiedTime FROM TaskMan.Person WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Person OFF
GO
DROP TABLE TaskMan.Person
GO
EXECUTE sp_rename N'TaskMan.Tmp_Person', N'Person', 'OBJECT' 
GO
COMMIT
