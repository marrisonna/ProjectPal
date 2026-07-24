CREATE TABLE [TaskMan].[System] (
    [ReleaseVersion] NVARCHAR (50) NULL,
    [LastUpdateTime] DATETIME      NULL
);


GO
GRANT DELETE
    ON OBJECT::[TaskMan].[System] TO [eabs_pp]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[TaskMan].[System] TO [eabs_pp]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[TaskMan].[System] TO [eabs_pp]
    AS [dbo];


GO
GRANT UPDATE
    ON OBJECT::[TaskMan].[System] TO [eabs_pp]
    AS [dbo];

