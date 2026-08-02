CREATE TABLE TaskMan.Tmp_Task
	(
	TaskId int NOT NULL IDENTITY (1, 1),
	ProjectId int NULL,
	OrigTaskNumber nvarchar(50) NULL,
	Priority nvarchar(50) NULL,
	AffectedComponentId int NULL,
	Description varchar(1000) NULL,
	ExternalReferenceURL nvarchar(150) NULL,
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
	StartRelativeDaysToProject int NULL,
	ModifiedBy nvarchar(50) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
	 TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE TaskMan.Tmp_Task SET (LOCK_ESCALATION = TABLE)
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task ON
GO
IF EXISTS(SELECT * FROM TaskMan.Task)
	 EXEC('INSERT INTO TaskMan.Tmp_Task (TaskId, ProjectId, OrigTaskNumber, Priority, AffectedComponentId, Description, RequestorPersonId, DateAdded, EffortInDays, EffortType, PercentageAllocation, TaskType, Status, StatusDate, DetailedDescription, Owner, Private, TentativeResourceAssignment, StartRelativeDaysToProject, ModifiedBy, ModifiedTime)
		SELECT TaskId, ProjectId, OrigTaskNumber, Priority, AffectedComponentId, Description, RequestorPersonId, DateAdded, EffortInDays, EffortType, PercentageAllocation, TaskType, Status, StatusDate, DetailedDescription, Owner, Private, TentativeResourceAssignment, StartRelativeDaysToProject, ModifiedBy, ModifiedTime FROM TaskMan.Task WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task OFF
GO
DROP TABLE TaskMan.Task
GO
EXECUTE sp_rename N'TaskMan.Tmp_Task', N'Task', 'OBJECT' 
GO