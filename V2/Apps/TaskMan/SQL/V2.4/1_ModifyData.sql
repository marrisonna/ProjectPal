update TaskMan.Task2Project
set ModifiedTime = GETDATE()
where ModifiedTime is null
go

update TaskMan.Task2Resource
set ModifiedTime = GETDATE()
where ModifiedTime is null
go

ALTER PROCEDURE [TaskMan].[SelectAllAttachements] 
AS
BEGIN
	select AttachmentId,
		   TaskId,
		   ComponentId,
		   ProjectId,
		   Name,
		   CreateTime,
		   DataType,
		   Size,
		   [From],
		   [Owner],
		   ModifiedTime,
		   ModifiedBy
	 from TaskMan.Attachment
END
go

CREATE TABLE [TaskMan].[System]
	(
	ReleaseVersion varchar(50) NULL,
	LastUpdateTime datetime NULL
	)  ON [PRIMARY]
GO

insert into [TaskMan].[System] (ReleaseVersion,LastUpdateTime) values ('2.4.0',GETDATE())
go


CREATE PROCEDURE [TaskMan].[SelectSystem] 
AS
BEGIN
	select * 
	from [TaskMan].[System]
END
go

GRANT EXECUTE ON [TaskMan].[SelectSystem] TO [ABSIT]
GO


