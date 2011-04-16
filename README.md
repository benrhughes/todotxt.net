### todotxt.net

This is an implemention of [todo.txt](http://todotxt.com/) on the .NET framework. It is currently at v0.1 and is marginally usable. The aim is for it to be fully complient with [this spec](https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format). 

There is installer for the latest version available from the github download page.

Current features:

 - Displays tasks (from a hard-coded file location)
 - Sorts by completed, priority, project, context or the order in the file
 - Has an input box for creating tasks, which are written to the file
 - Preferences for the todo.txt file, window size and sort order are persisted
 - Keyboard shortcuts
	- O: open todo.txt file
	- N: new task
	- J: next task
	- K: prev task
	- X: toggle task completion
	- D: delete task (with confirmation)
	- U: update task
	- .: reload tasks from file

The main things missing (that I know of) are

 - multiple projects and contexts per task
 - created date support
 - due date support
 - filtering in the UI
 - UI prettiness

Stuff I'm aiming for:

 - minimalist, keyboard-driven UI
 - gmail/twitter-like keyboard nav
 - re-usable library that other projects can use as a todo.txt API
 - API (but not UI) runs under Mono
 - full compliance with Gina's specs