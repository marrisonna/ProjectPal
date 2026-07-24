CREATE TABLE [TaskMan].[Task] (
    [TaskId]                      INT            IDENTITY (1, 1) NOT NULL,
    [ProjectId]                   INT            NULL,
    [OrigTaskNumber]              NVARCHAR (50)  NULL,
    [Priority]                    NVARCHAR (50)  NULL,
    [AffectedComponentId]         INT            NULL,
    [Description]                 VARCHAR (1000) NULL,
    [RequestorPersonId]           INT            NULL,
    [DateAdded]                   DATETIME       NOT NULL,
    [EffortInDays]                FLOAT (53)     NULL,
    [EffortType]                  NVARCHAR (50)  NULL,
    [PercentageAllocation]        FLOAT (53)     NULL,
    [TaskType]                    NVARCHAR (50)  NULL,
    [Status]                      NVARCHAR (50)  NULL,
    [StatusDate]                  DATETIME       NULL,
    [DetailedDescription]         NVARCHAR (MAX) NULL,
    [Owner]                       NVARCHAR (50)  NULL,
    [Private]                     BIT            NULL,
    [TentativeResourceAssignment] BIT            NULL,
    [StartRelativeDaysToProject]  INT            NULL,
    [ModifiedBy]                  NVARCHAR (50)  NULL,
    [ModifiedTime]                DATETIME       NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[Task] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[Task] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[Task] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[Task] TO [eabs_pp]
    AS [dbo];

