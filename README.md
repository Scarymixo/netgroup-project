# Netgroup home project
## Requirements
- Create a system which enables to create events and for users to register to those events. The app has two roles, admin and user, but only the admin can authenticate and login.

### General requirements
- The app has to have a header with a authenticate button, so that the admin can log in
- The app has to include a list of events. Events appear when an admin creates them. If there are no events, the corresponding message is displayed.
- No hard requirement for functional requirements like validating fields or showing error messages.

### Admin user requirements
- Admin user can log in with email and password. (Login info has to be in app config)
- Admin can create new events. Events have a name, time and maximum people
- Admins do not have to have the ability to delete events

### User requirements
- Cannot log in
- Entering the app user can see admin created events
- Can choose an event from the list and register
- One user can register to multiple events
- On register the user has to enter first name, last name and national id code
- User does not have to have the ability to cancel registration

### Technology used
- Backend in .NET
- Frontend in Vue (In separate repo)
- Database in PostgreSQL

## Running

```bash
dotnet build
dotnet run --project WebApp
```

Postgres must be reachable on startup — `WaitDbConnection` loops until it responds.

### Install or update tooling for .net

~~~bash
dotnet tool update -g dotnet-ef
dotnet tool update -g dotnet-aspnet-codegenerator
dotnet tool update -g Microsoft.Web.LibraryManager.Cli
~~~

## JS Libs

Add htmx and alpine to js libs.
~~~bash
libman install htmx.org --files dist/htmx.min.js 
libman install alpinejs --files dist/cdn.min.js 
~~~

### Generate database migration

Run from solution folder.

~~~bash
dotnet ef migrations --project App.DAL.EF --startup-project WebApp add Initial
dotnet ef database   --project App.DAL.EF --startup-project WebApp drop
dotnet ef migrations --project App.DAL.EF --startup-project WebApp remove
dotnet ef database   --project App.DAL.EF --startup-project WebApp update
~~~

## Generate identity UI

Install Microsoft.VisualStudio.Web.CodeGeneration.Design to WebApp.  
Run from inside the WebApp directory.

~~~bash
dotnet aspnet-codegenerator identity -dc DAL.App.EF.AppDbContext -f  
~~~

## Generate controllers

Run from inside the WebApp directory.    
Don't forget to add ***Microsoft.VisualStudio.Web.CodeGeneration.Design*** package to the WebApp project as a NuGet package reference.

MVC Web Controllers (disable global warnings as errors - otherwise only one controller will be generated, then compile starts to fail)

~~~bash
dotnet aspnet-codegenerator controller -name EventsController -m  Event -actions -dc AppDbContext -outDir Areas/Admin/Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f
dotnet aspnet-codegenerator controller -name ParticipantsController -m  Participant -actions -dc AppDbContext -outDir Areas/Admin/Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f
~~~

API Controllers. (Run from inside the WebApp directory.)

~~~bash
dotnet aspnet-codegenerator controller -name EventsController     -m App.Domain.Event    -actions -dc AppDbContext -outDir ApiControllers -api --useAsyncActions  -f
dotnet aspnet-codegenerator controller -name ParticipantsController     -m App.Domain.Participant     -actions -dc AppDbContext -outDir ApiControllers -api --useAsyncActions  -f
~~~
