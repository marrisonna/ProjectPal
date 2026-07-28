
BEGIN TRANSACTION
GO
CREATE TABLE TaskMan.Tmp_Task
	(
	TaskId int NOT NULL IDENTITY (1, 1),
	OrigTaskNumber nvarchar(50) NULL,
	Priority nvarchar(50) NULL,
	DueDate datetime NULL,
	AffectedComponentId int NULL,
	Description varchar(1000) NULL,
	RequestorPersonId int NULL,
	DateAdded datetime NOT NULL,
	EffortInDays float(53) NULL,
	EffortType nvarchar(50) NULL,
	PercentageAllocation float(53) NULL,
	TaskType nvarchar(50) NULL,
	Status nvarchar(50) NULL,
	StatusDate datetime NULL,
	DetailedDescription nvarchar(MAX) NULL,
	Owner nvarchar(50) NULL,
	Private bit NULL,
	TentativeResourceAssignment bit NULL,
	ModifiedBy nvarchar(50) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE TaskMan.Tmp_Task SET (LOCK_ESCALATION = TABLE)
GO
GRANT DELETE ON TaskMan.Tmp_Task TO eabs_pp  AS dbo
GO
GRANT INSERT ON TaskMan.Tmp_Task TO eabs_pp  AS dbo
GO
GRANT SELECT ON TaskMan.Tmp_Task TO eabs_pp  AS dbo
GO
GRANT UPDATE ON TaskMan.Tmp_Task TO eabs_pp  AS dbo
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task ON
GO
IF EXISTS(SELECT * FROM TaskMan.Task)
	 EXEC('INSERT INTO TaskMan.Tmp_Task (TaskId, OrigTaskNumber, Priority, DueDate, AffectedComponentId, Description, RequestorPersonId, DateAdded, EffortInDays, EffortType, PercentageAllocation, TaskType, Status, StatusDate, DetailedDescription, Owner, TentativeResourceAssignment, ModifiedBy, ModifiedTime)
		SELECT TaskId, OrigTaskNumber, Priority, DueDate, AffectedComponentId, Description, RequestorPersonId, DateAdded, EffortInDays, EffortType, PercentageAllocation, TaskType, Status, StatusDate, DetailedDescription, Owner, TentativeResourceAssignment, ModifiedBy, ModifiedTime FROM TaskMan.Task WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task OFF
GO
DROP TABLE TaskMan.Task
GO
EXECUTE sp_rename N'TaskMan.Tmp_Task', N'Task', 'OBJECT' 
GO

-------------------------------------

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
	 EXEC('INSERT INTO TaskMan.Tmp_Project (ProjectId, Name, ParentProjectId, Priority, DetailedDescription, Owner, DueDate, ModifiedBy, ModifiedTime)
		SELECT ProjectId, Name, ParentProjectId, Priority, DetailedDescription, Owner, DueDate, ModifiedBy, ModifiedTime FROM TaskMan.Project WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Project OFF
GO
DROP TABLE TaskMan.Project
GO
EXECUTE sp_rename N'TaskMan.Tmp_Project', N'Project', 'OBJECT' 
GO

COMMIT
