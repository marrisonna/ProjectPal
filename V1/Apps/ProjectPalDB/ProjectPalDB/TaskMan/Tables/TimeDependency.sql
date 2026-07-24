CREATE TABLE [TaskMan].[TimeDependency] (
    [TimeDependencyId] INT           IDENTITY (1, 1) NOT NULL,
    [PreTaskId]        INT           NULL,
    [PreProjectId]     INT           NULL,
    [PostTaskId]       INT           NULL,
    [PostProjectId]    INT           NULL,
    [ModifiedBy]       NVARCHAR (50) NULL,
    [ModifiedTime]     DATETIME      NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[TimeDependency] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[TimeDependency] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[TimeDependency] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[TimeDependency] TO [eabs_pp]
    AS [dbo];

