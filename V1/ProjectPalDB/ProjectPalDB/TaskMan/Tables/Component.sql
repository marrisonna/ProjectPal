CREATE TABLE [TaskMan].[Component] (
    [ComponentId]       INT             IDENTITY (1, 1) NOT NULL,
    [ParentComponentId] INT             NULL,
    [Name]              NVARCHAR (1000) NOT NULL,
    [Owner]             NVARCHAR (50)   NULL,
    [ModifiedBy]        NVARCHAR (50)   NULL,
    [ModifiedTime]      DATETIME        NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Component] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Component] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Component] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Component] TO [eabs_pp]
    AS [dbo];

