
BEGIN TRANSACTION
GO
CREATE TABLE TaskMan.Tmp_Project
	(
	ProjectId int NOT NULL IDENTITY (1, 1),
	Name nvarchar(1000) NULL,
	ParentProjectId int NULL,
	Priority nvarchar(50) NULL,
	DetailedDescription nvarchar(MAX) NULL,
	Owner nvarchar(50) NULL,
	Private bit NULL,
	DueDate datetime NULL,
	StartDate datetime NULL,
	ModifiedBy nvarchar(50) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE TaskMan.Tmp_Project SET (LOCK_ESCALATION = TABLE)
GO
GRANT DELETE ON TaskMan.Tmp_Project TO eabs_pp  AS dbo
GO
GRANT INSERT ON TaskMan.Tmp_Project TO eabs_pp  AS dbo
GO
GRANT SELECT ON TaskMan.Tmp_Project TO eabs_pp  AS dbo
GO
GRANT UPDATE ON TaskMan.Tmp_Project TO eabs_pp  AS dbo
GO
SET IDENTITY_INSERT TaskMan.Tmp_Project ON
GO
IF EXISTS(SELECT * FROM TaskMan.Project)
	 EXEC('INSERT INTO TaskMan.Tmp_Project (ProjectId, Name, ParentProjectId, Priority, DetailedDescription, Owner, Private, DueDate, ModifiedBy, ModifiedTime)
		SELECT ProjectId, Name, ParentProjectId, Priority, DetailedDescription, Owner, Private, DueDate, ModifiedBy, ModifiedTime FROM TaskMan.Project WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Project OFF
GO
DROP TABLE TaskMan.Project
GO
EXECUTE sp_rename N'TaskMan.Tmp_Project', N'Project', 'OBJECT' 
GO
COMMIT
