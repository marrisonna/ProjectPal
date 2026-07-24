/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
GO
CREATE TABLE TaskMan.Tmp_Task
	(
	TaskId int NOT NULL IDENTITY (1, 1),
	OrigTaskNumber varchar(50) NULL,
	OrigResource varchar(50) NULL,
	Priority varchar(16) NULL,
	DueDate datetime NULL,
	OrigAffectedComponent varchar(50) NULL,
	ComponentId int NULL,
	Description varchar(250) NULL,
	OrigRequestor varchar(50) NULL,
	Requestor_PersonId int NULL,
	DateAdded datetime NOT NULL,
	EffortEstimate float(53) NULL,
	EffortType varchar(10) NULL,
	PercentageAllocation float(53) NULL,
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
	 EXEC('INSERT INTO TaskMan.Tmp_Task (TaskId, OrigTaskNumber, OrigResource, Priority, DueDate, OrigAffectedComponent, ComponentId, Description, OrigRequestor, Requestor_PersonId, DateAdded, EffortEstimate, PercentageAllocation, TaskType, Status, StatusDate, DetailedDescription, Owner, TentativeResourceAssignment, ModifiedBy, ModifiedTime)
		SELECT TaskId, OrigTaskNumber, OrigResource, Priority, DueDate, OrigAffectedComponent, ComponentId, Description, OrigRequestor, Requestor_PersonId, DateAdded, EffortEstimate, PercentageAllocation, TaskType, Status, StatusDate, DetailedDescription, Owner, TentativeResourceAssignment, ModifiedBy, ModifiedTime FROM TaskMan.Task WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Task OFF
GO
DROP TABLE TaskMan.Task
GO
EXECUTE sp_rename N'TaskMan.Tmp_Task', N'Task', 'OBJECT' 
GO
COMMIT

USE [TaskMan]
GO
/****** Object:  StoredProcedure [TaskMan].[UpdateTask]    Script Date: 05/04/2012 18:39:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER PROCEDURE [TaskMan].[UpdateTask] 
			@DatabaseId int,
            @Description varchar(250) =null,
            @DetailedDescription varchar(5000) =null,
            @Priority varchar(16) =null,
            @DueDate DateTime =null,
            @RequestorPersonId int,
            @AffectedComponentId int =null,
            @DateAdded DateTime,

            @EffortInDays float =null,
            @EffortType varchar(10) = null,
            @PercentageAllocation float =null,
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
			 DueDate = @DueDate,
			 Requestor_PersonId = @RequestorPersonId,
			 ComponentId = @AffectedComponentId,
			 DateAdded = @DateAdded,
			 EffortEstimate = @EffortInDays,
			 EffortType = @EffortType,
			 PercentageAllocation = isnull(@PercentageAllocation,1),
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
			(Description, DetailedDescription, Priority, DueDate, Requestor_PersonId, 
			ComponentId, DateAdded, EffortEstimate, EffortType,PercentageAllocation,TaskType, Status,StatusDate,
			[Owner],TentativeResourceAssignment,
			ModifiedBy,ModifiedTime)
		values
			(@Description, @DetailedDescription, @Priority, @DueDate, @RequestorPersonId,
			 @AffectedComponentId, @DateAdded, @EffortInDays, @EffortType, isnull(@PercentageAllocation,1), @TaskType, @Status,@StatusDate,
			 @Owner,@TentativeResourceAssignment,
			 user,@modifiedTime)
		 
		 select convert(int,@@IDENTITY) as DatabaseId, @modifiedTime as ModifiedTime		
	end
END
go

update TaskMan.Task
set PercentageAllocation = 1
where PercentageAllocation is null

update TaskMan.Task
set EffortType = 'ManDays'


update TaskMan.System
set ReleaseVersion = '3.0.0'
