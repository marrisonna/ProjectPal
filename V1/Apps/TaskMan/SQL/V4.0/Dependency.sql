

CREATE TABLE [TaskMan].[TimeDependency](
	[TimeDependencyId] [int] IDENTITY(1,1) NOT NULL,
	[PreTaskId] [int] NULL,
	[PreProjectId] [int] NULL,
	[PostTaskId] [int] NULL,
	[PostProjectId] [int] NULL,
	[ModifiedBy] [nvarchar](50) NULL,
	[ModifiedTime] [datetime] NULL
) ON [PRIMARY]

GO

GRANT DELETE ON [TaskMan].[TimeDependency] TO [eabs_pp]
GO
GRANT INSERT ON [TaskMan].[TimeDependency] TO [eabs_pp]
GO
GRANT SELECT ON [TaskMan].[TimeDependency] TO [eabs_pp]
GO
GRANT UPDATE ON [TaskMan].[TimeDependency] TO [eabs_pp]
GO


