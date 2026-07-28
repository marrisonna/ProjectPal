

update [TaskMan].[System]
set ReleaseVersion = '2.5.0'
go

BEGIN TRANSACTION
GO
CREATE TABLE TaskMan.Tmp_Project
	(
	ProjectId int NOT NULL IDENTITY (1, 1),
	Name varchar(100) NULL,
	ParentProjectId int NULL,
	Priority varchar(16) NULL,
	DetailedDescription varchar(5000) NULL,
	Owner varchar(50) NULL,
	DueDate datetime NULL,
	ModifiedBy varchar(20) NULL,
	ModifiedTime datetime NULL
	)  ON [PRIMARY]
GO
SET IDENTITY_INSERT TaskMan.Tmp_Project ON
GO
IF EXISTS(SELECT * FROM TaskMan.Project)
	 EXEC('INSERT INTO TaskMan.Tmp_Project (ProjectId, Name, ParentProjectId, Priority, DetailedDescription, Owner, ModifiedBy, ModifiedTime)
		SELECT ProjectId, Name, ParentProjectId, Priority, DetailedDescription, Owner, ModifiedBy, ModifiedTime FROM TaskMan.Project WITH (HOLDLOCK TABLOCKX)')
GO
SET IDENTITY_INSERT TaskMan.Tmp_Project OFF
GO
DROP TABLE TaskMan.Project
GO
EXECUTE sp_rename N'TaskMan.Tmp_Project', N'Project', 'OBJECT' 
GO
COMMIT

USE [TaskMan]
GO
/****** Object:  StoredProcedure [TaskMan].[UpdateProject]    Script Date: 02/22/2012 18:56:41 ******/
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
            @DueDate datetime=null     
	
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
			 ModifiedBy = user,
			 ModifiedTime = @modifiedTime
		where  ProjectId = @DatabaseId
		 
		select @DatabaseId as DatabaseId, @modifiedTime as ModifiedTime			
	end
	else
	begin
		insert into TaskMan.Project 
			( Name, ParentProjectId, Priority,DetailedDescription,[Owner],DueDate,ModifiedBy,ModifiedTime)
		values
			( @Name, @ParentProjectId, @Priority,@DetailedDescription,@Owner,@DueDate,user,@modifiedTime)
		 
		 select convert(int,@@IDENTITY) as DatabaseId, @modifiedTime as ModifiedTime		
	end
END
go

