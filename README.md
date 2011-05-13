### todotxt.net

This is an implemention of [todo.txt](http://todotxt.com/) on the .NET framework. It is currently at v0.7 and is fairly 
usable. As far as I am aware, it is fully complient with [Gina's spec](https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format). 

There is installer for the latest version available from the [github download page](https://github.com/benrhughes/todotxt.net/downloads).

#### Goals

 - minimalist, keyboard-driven UI
 - gmail/twitter-like keyboard nav (single key, easily accessible)
 - re-usable library that other projects can use as a todo.txt API
 - API (but not UI) runs under Mono
 - full compliance with Gina's specs


#### Current features:

 - Sorting by completed status, priority, project, context, due date or the order in the file
 - Sorting respects multiple projects and contexts
 - Remembers preferences for the todo.txt file, window size and sort order
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