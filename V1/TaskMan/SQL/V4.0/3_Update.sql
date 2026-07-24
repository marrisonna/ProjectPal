select a.Task2ProjectId into #del
from TaskMan.Task2Project a, TaskMan.Task2Project b
where a.TaskId = b.TaskId
and  ((a.ProjectId < b.ProjectId) or (a.ProjectId = b.ProjectId and a.Task2ProjectId < b.Task2ProjectId))


delete TaskMan.Task2Project
from #del
where TaskMan.Task2Project.Task2ProjectId = #del.Task2ProjectId




INSERT INTO [TaskMan].[TaskMan].[Project]
           ([Name],[ParentProjectId],[Priority],[DetailedDescription],[Owner] 
           ,[Private],[DueDate],[StartDate],[ModifiedBy],[ModifiedTime])
     VALUES
           ('Migration V4.0',null,'0_Closed','','EUROPE\marrison'
           ,1,null,'1-jan-2000','EUROPE\marrison',GETDATE())
GO

update TaskMan.Task
set ProjectId = [TaskMan].[Project].ProjectId
from  [TaskMan].[Project]
where [TaskMan].[Project].Name = 'Migration V4.0'

update TaskMan.Task
set ProjectId = TaskMan.Task2Project.ProjectId
from  TaskMan.Task2Project
where TaskMan.Task2Project.TaskId = TaskMan.Task.TaskId


select COUNT(*) as n, taskId into #resourceCount
from TaskMan.Task2Resource
group by taskId

insert into #resourceCount (n, TaskId)
select 1, TaskMan.Task.TaskId
from TaskMan.Task
where not (TaskMan.Task.TaskId in (select TaskId from #resourceCount))

update TaskMan.Task 
set PercentageAllocation =1
where PercentageAllocation =0

select TaskMan.Task.TaskId, 
       isnull(EffortInDays,0) / #resourceCount.n/ isnull(PercentageAllocation,1) as Duration into #duration
from #resourceCount,
     TaskMan.Task
where TaskMan.Task.TaskId = #resourceCount.TaskId
and EffortType = 'ManDays'


insert into #duration (TaskId, Duration)
select TaskMan.Task.TaskId, TaskMan.Task.EffortInDays
from TaskMan.Task
where not (TaskMan.Task.TaskId in (select TaskId from #duration))


update TaskMan.Task
set DueDate = StatusDate
where DueDate is null

select TaskMan.Task.TaskId, 
       DATEADD(day,-#duration.Duration,dueDate) as StartDate into #startdates
from TaskMan.Task, #duration
where TaskMan.Task.TaskId = #duration.taskId


select MIN(StartDate) as PStartDate, projectId into #projStart
from TaskMan.Task, #startdates
where TaskMan.Task.TaskId =  #startdates.TaskId
group by projectId


update TaskMan.Project
set StartDate = pStartDate
from #projStart
where TaskMan.Project.ProjectId = #projStart.ProjectId

update TaskMan.Project
set StartDate = '1-jan-2010' 
where StartDate is null

update TaskMan.Task 
set StartRelativeDaysToProject = DATEDIFF(day,TaskMan.Project.StartDate, #startdates.StartDate)
from TaskMan.Project, #startdates
where TaskMan.Task.ProjectId = TaskMan.Project.ProjectId
and #startdates.TaskId = TaskMan.Task.TaskId



select * from TaskMan.Task where StartRelativeDaysToProject is null
select * from TaskMan.Task where ProjectId is null
select * from TaskMan.Project where StartDate is null

ALTER TABLE TaskMan.Task
	DROP COLUMN DueDate
GO


