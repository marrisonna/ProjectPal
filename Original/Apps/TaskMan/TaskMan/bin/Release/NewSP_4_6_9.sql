create schema ProjectPal
go

create Procedure ProjectPal.GetTasks
as
Begin

   select t.TaskId, t.Description, p.Name
   from TaskMan.Task t,
	    TaskMan.Project p
	where t.ProjectId = p.ProjectId
	and   t.Private =0
	and   p.Private =0

End
go