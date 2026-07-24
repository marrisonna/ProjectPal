CREATE TABLE [TaskMan].[Task2Resource] (
    [Task2ResourceId] INT           IDENTITY (1, 1) NOT NULL,
    [TaskId]          INT           NOT NULL,
    [PersonId]        INT           NULL,
    [OtherResourceId] INT           NULL,
    [ModifiedBy]      NVARCHAR (50) NULL,
    [ModifiedTime]    DATETIME      NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Task2Resource] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Task2Resource] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Task2Resource] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Task2Resource] TO [eabs_pp]
    AS [dbo];

