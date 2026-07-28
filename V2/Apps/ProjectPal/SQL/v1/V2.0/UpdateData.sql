declare @neil varchar(20)
declare @now datetime

select @neil = 'EUROPE\marrison'
select @now = getdate()

update TaskMan.Attachment set Owner = @neil, ModifiedBy = @neil, ModifiedTime = @now
update TaskMan.Component set Owner = @neil, ModifiedBy = @neil, ModifiedTime = @now
update TaskMan.Person set ModifiedBy = @neil, ModifiedTime = @now
update TaskMan.Project set Owner = @neil, ModifiedBy = @neil, ModifiedTime = @now
update TaskMan.Remark set Owner = @neil, ModifiedBy = @neil, ModifiedTime = @now
update TaskMan.Task set Owner = @neil, ModifiedBy = @neil, ModifiedTime = @now

UPDATE [TaskMan].[Person] SET [DBLogin] = 'ASIAPAC\damlepar', [UserType] = 'NormalUser' where Name = 'Parth'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'ASIAPAC\kapoorr1', [UserType] = 'NormalUser' where Name = 'Rahul'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'ASIAPAC\parnaik', [UserType] = 'PowerUser' where Name = 'Paresh'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'ASIAPAC\patilsa', [UserType] = 'NormalUser' where Name = 'Sachin'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'EUROPE\dsouzaru', [UserType] = 'NormalUser' where Name = 'Ruth'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'EUROPE\gaddams', [UserType] = 'NormalUser' where Name = 'Sreevani'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'EUROPE\marrison', [UserType] = 'SuperUser' where Name = 'Neil'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'EUROPE\mingelgd', [UserType] = 'ReadOnlyUser' where Name = 'Danny'
UPDATE [TaskMan].[Person] SET [DBLogin] = 'EUROPE\pykero', [UserType] = 'ReadOnlyUser' where Name = 'Ron'

