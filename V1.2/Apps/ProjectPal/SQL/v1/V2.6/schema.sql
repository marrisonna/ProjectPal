

begin tran
CREATE TABLE TaskMan.Tmp_Project
	(
	ProjectId int NOT NULL IDENTITY (1, 1),
	Name varchar(100) NULL,
	ParentProjectId int NULL,
	Priority varchar(16) NULL,
	DetailedDescription varchar(5000) NULL,
	Owner varchar(50) NULL,
	StartDate datetime NULL,
	DueDate datetime NULL,
	ModifiedBy varchar(20) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
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
go



USE [TaskMan]
GO
/****** Object:  StoredProcedure [TaskMan].[UpdateProject]    Script Date: 02/27/2012 06:28:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [TaskMan].[UpdateProject] 
			@DatabaseId int,
			@ParentProjectId int,
            @Name varchar(100) =null,
            @Priority varchar(16)=null,
            @DetailedDescription varchar(5000)=null,
            @Owner varchar(500)=null,
            @DueDate datetime=null,
            @StartDate datetime=null     
	
AS
BEGIN
	declare @modifiedTime datetime
	select @modifiedTime = GETDATE()
	update [TaskMan].[System] set LastUpdateTime = @modifiedTime
	
	if @Owner is null
	select @Owner = USER
	
	if @DatabaseId > 0
	begin
		update TaskMan.Project set	
			 Name = @Name,
			 Priority = @Priority,
			 ParentProjectId = @ParentProjectId,
			 DetailedDescription = @DetailedDescription,
			 [Owner] = @Owner,
			 DueDate = @DueDate,
			 StartDate = @StartDate,
			 ModifiedBy = user,
			 ModifiedTime = @modifiedTime
		where  ProjectId = @DatabaseId
		 
		select @DatabaseId as DatabaseId, @modifiedTime as ModifiedTime			
	end
	else
	begin
		insert into TaskMan.Project 
			( Name, ParentProjectId, Priority,DetailedDescription,[Owner],DueDate,StartDate,ModifiedBy,ModifiedTime)
		values
			( @Name, @ParentProjectId, @Priority,@DetailedDescription,@Owner,@DueDate,@StartDate,user,@modifiedTime)
		 
		 select convert(int,@@IDENTITY) as DatabaseId, @modifiedTime as ModifiedTime		
	end
END
go


BEGIN TRANSACTION
GO
CREATE TABLE TaskMan.Tmp_Task
	(
	TaskId int NOT NULL IDENTITY (1, 1),
	OrigTaskNumber varchar(50) NULL,
	OrigResource varchar(50) NULL,
	Priority varchar(16) NULL,
	StartDate datetime NULL,
	DueDate datetime NULL,
	OrigAffectedComponent varchar(50) NULL,
	ComponentId int NULL,
	Description varchar(250) NULL,
	OrigRequestor varchar(50) NULL,
	Requestor_PersonId int NULL,
	DateAdded datetime NOT NULL,
	EffortEstimate float(53) NULL,
	TaskType varchar(50) NULL,
	Status varchar(20) NULL,
	StatusDate datetime NULL,
	DetailedDescription varchar(5000) NULL,
	Owner varchar(50) NULL,
	TentativeResourceAssignment bit NULL,
	ModifiedBy varchar(20) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task ON
GO
IF EXISTS(SELECT * FROM TaskMan.Task)
	 EXEC('INSERT INTO TaskMan.Tmp_Task (TaskId, OrigTaskNumber, OrigResource, Priority, DueDate, OrigAffectedComponent, ComponentId, Description, OrigRequestor, Requestor_PersonId, DateAdded, EffortEstimate, TaskType, Status, StatusDate, DetailedDescription, Owner, TentativeResourceAssignment, ModifiedBy, ModifiedTime)
		SELECT TaskId, OrigTaskNumber, OrigResource, Priority, DueDate, OrigAffectedComponent, ComponentId, Description, OrigRequestor, Requestor_PersonId, DateAdded, EffortEstimate, TaskType, Status, StatusDate, DetailedDescription, Owner, TentativeResourceAssignment, ModifiedBy, ModifiedTime FROM TaskMan.Task WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task OFF
GO
DROP TABLE TaskMan.Task
GO
EXECUTE sp_rename N'TaskMan.Tmp_Task', N'Task', 'OBJECT' 
GO
COMMIT
go


ALTER PROCEDURE [TaskMan].[UpdateTask] 
			@DatabaseId int,
            @Description varchar(250) =null,
            @DetailedDescription varchar(5000) =null,
            @Priority varchar(16) =null,
            @StartDate DateTime =null,
            @DueDate DateTime =null,
            @RequestorPersonId int,
            @AffectedComponentId int =null,
            @DateAdded DateTime,

            @EffortInDays float =null,
            @TaskType varchar(50) =null,
            @Status varchar(20) =null,
            @StatusDate DateTime =null,
            @Owner varchar(50) =null,
            @TentativeResourceAssignment bit =null
        
	
AS
BEGIN

	declare @modifiedTime datetime
	select @modifiedTime = GETDATE()
	update [TaskMan].[System] set LastUpdateTime = @modifiedTime

	if @Owner is null
	select @Owner = USER
	
	if @DatabaseId > 0
	begin
		update TaskMan.Task set	
			 Description = @Description,
			 DetailedDescription = @DetailedDescription,
			 Priority = @Priority,
			 StartDate = @StartDate,
			 DueDate = @DueDate,
			 Requestor_PersonId = @RequestorPersonId,
			 ComponentId = @AffectedComponentId,
			 DateAdded = @DateAdded,
			 EffortEstimate = @EffortInDays,
			 TaskType = @TaskType,
			 Status = @Status,
			 StatusDate = @StatusDate,
			 [Owner] = @Owner,
			 TentativeResourceAssignment = @TentativeResourceAssignment,
			 ModifiedBy = user,
			 ModifiedTime = @modifiedTime
		where  TaskId = @DatabaseId
		 
		select @DatabaseId as DatabaseId, @modifiedTime as ModifiedTime		
	end
	else
	begin
		insert into TaskMan.Task 
			(Description, DetailedDescription, Priority, StartDate, DueDate, Requestor_PersonId, 
			ComponentId, DateAdded, EffortEstimate, TaskType, Status,StatusDate,
			[Owner],TentativeResourceAssignment,
			ModifiedBy,ModifiedTime)
		values
			(@Description, @DetailedDescription, @Priority, @StartDate, @DueDate, @RequestorPersonId,
			 @AffectedComponentId, @DateAdded, @EffortInDays, @TaskType, @Status,@StatusDate,
			 @Owner,@TentativeResourceAssignment,
			 user,@modifiedTime)
		 
		 select convert(int,@@IDENTITY) as DatabaseId, @modifiedTime as ModifiedTime		
	end
END
go
