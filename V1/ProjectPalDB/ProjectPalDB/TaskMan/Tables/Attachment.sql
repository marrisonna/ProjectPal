CREATE TABLE [TaskMan].[Attachment] (
    [AttachmentId] INT             IDENTITY (1, 1) NOT NULL,
    [TaskId]       INT             NULL,
    [ComponentId]  INT             NULL,
    [ProjectId]    INT             NULL,
    [Name]         NVARCHAR (1000) NOT NULL,
    [CreateTime]   DATETIME        NULL,
    [DataType]     NVARCHAR (10)   NOT NULL,
    [From]         NVARCHAR (20)   NULL,
    [Size]         INT             NULL,
    [Data]         VARBINARY (MAX) NULL,
    [Owner]        NVARCHAR (50)   NULL,
    [ModifiedBy]   NVARCHAR (50)   NULL,
    [ModifiedTime] DATETIME        NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Attachment] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Attachment] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Attachment] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Attachment] TO [eabs_pp]
    AS [dbo];

