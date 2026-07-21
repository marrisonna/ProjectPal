CREATE TABLE [TaskMan].[Task2Project] (
    [Task2ProjectId] INT           IDENTITY (1, 1) NOT NULL,
    [TaskId]         INT           NULL,
    [ProjectId]      INT           NULL,
    [ModifiedBy]     NVARCHAR (50) NULL,
    [ModifiedTime]   DATETIME      NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Task2Project] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Task2Project] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Task2Project] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Task2Project] TO [eabs_pp]
    AS [dbo];

