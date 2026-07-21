CREATE TABLE [TaskMan].[Project] (
    [ProjectId]           INT             IDENTITY (1, 1) NOT NULL,
    [Name]                NVARCHAR (1000) NULL,
    [ParentProjectId]     INT             NULL,
    [Priority]            NVARCHAR (50)   NULL,
    [DetailedDescription] NVARCHAR (MAX)  NULL,
    [Owner]               NVARCHAR (50)   NULL,
    [Private]             BIT             NULL,
    [DueDate]             DATETIME        NULL,
    [StartDate]           DATETIME        NULL,
    [ModifiedBy]          NVARCHAR (50)   NULL,
    [ModifiedTime]        DATETIME        NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Project] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Project] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Project] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Project] TO [eabs_pp]
    AS [dbo];

