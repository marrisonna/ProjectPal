CREATE TABLE [TaskMan].[Person] (
    [PersonId]     INT            IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (100) NULL,
    [IsResource]   BIT            NULL,
    [IsActive]     BIT            NULL,
    [DBLogin]      NVARCHAR (50)  NULL,
    [UserType]     NVARCHAR (20)  NULL,
    [Colour]       NVARCHAR (25)  NULL,
    [ModifiedBy]   NVARCHAR (50)  NULL,
    [ModifiedTime] DATETIME       NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Person] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Person] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Person] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Person] TO [eabs_pp]
    AS [dbo];

