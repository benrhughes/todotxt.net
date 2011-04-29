### todotxt.net

This is an implemention of [todo.txt](http://todotxt.com/) on the .NET framework. It is currently at v0.4 and is fairly 
usable. The aim is for it to be fully complient with [this spec](https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format). 

There is installer for the latest version available from the [github download page](https://github.com/benrhughes/todotxt.net/downloads).

Current features:

 - Displays tasks
 - Sorts by completed, priority, project, context, due date or the order in the file
 - Sorting respects multiple projects and contexts
 - Preferences for the todo.txt file, window size and sort order are persisted
 - Keyboard shortcuts
	- O: open todo.txt file
	- N: new task
	- J: next task
	- K: prev task
	- X: toggle task completion
	- D: delete task (with confirmation)
	- U: update task
	- F: filter tasks (free-text, one filter condition per line)
	- .: reload tasks from file
	- ?: show help

The main things missing (that I know of) are

 - completed date support
 - UI prettiness

Stuff I'm aiming for:

 - minimalist, keyboard-driven UI
 - gmail/twitter-like keyboard nav
 - re-usable library that other projects can use as a todo.txt API
 - API (but not UI) runs under Mono
 - full compliance with Gina's specs