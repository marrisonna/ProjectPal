CREATE TABLE [TaskMan].[Remark] (
    [RemarkId]     INT            IDENTITY (1, 1) NOT NULL,
    [TaskId]       INT            NULL,
    [ComponentId]  INT            NULL,
    [ProjectId]    INT            NULL,
    [RemarkText]   NVARCHAR (MAX) NULL,
    [Owner]        NVARCHAR (50)  NOT NULL,
    [ModifiedTime] DATETIME       NOT NULL,
    [ModifiedBy]   NVARCHAR (50)  NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Remark] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Remark] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Remark] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Remark] TO [eabs_pp]
    AS [dbo];

