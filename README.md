### todotxt.net

This is my very rough first cut at implementing [todo.txt](http://todotxt.com/) for the .NET framework. It's in no way usable yet. The aim is for it to be fully complient with [this spec](https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format). 

Currently the UI

 - Displays tasks (from a hard-coded file location)
 - Sorts by priority, project, context or the order in the file
 - Has an input box for creating tasks, which are written to the file

Under the covers there is also

 - delete support
 - mark completed support

The main things missing (that I know of) are

 - multiple projects and contexts per task
 - created date support
 - due date support
 - filtering in the UI
 - deleting in the UI
 - updating in the UI
 - mark (un)completed in the UI
 - user prefs for the file location, default sort order etc
 - UI prettiness

Stuff I'm aiming for:

 - minimalist, keyboard-driven UI
 - gmail-like keyboard nav
 - re-usable library that other projects can use as a todo.txt API
 - API (but not UI) runs under Mono
 - full compliance with Gina's specs