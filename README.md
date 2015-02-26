### todotxt.net

This is an implemention of [todo.txt](http://todotxt.com/) using the .NET framework. As far as I am aware, it is fully compliant with [Gina's spec](https://github.com/ginatrapani/todo.txt-cli/wiki/The-Todo.txt-Format). 

There is installer for the latest version available from the [the main site](http://benrhughes.github.io/todotxt.net/).

#### Contributors please note

If you change any of the ToDoLib files, please make sure the current unit tests pass, and add new tests where appropriate.

Please send your pull requests to the 'dev' branch. If you forget it's not a major deal: it's just a bit more work for me as I have to manually merge.

#### Goals

 - minimalist, keyboard-driven UI
 - vim/gmail/twitter-like keyboard nav (single key, easily accessible)
 - re-usable library that other projects can use as a todo.txt API
 - API (but not UI) runs under Mono
 - full compliance with Gina's specs


#### Current features:

 - Sorting by completed status, priority, project, context, alphabetically due date or the order in the file
 - Sorting respects multiple projects and contexts
 - Remembers preferences for the todo.txt file, sort order, window size and position
 - Manual or automatic moving of completed tasks into an archive (done.txt) file
 - Free text filtering/search
 - Intellisense for projects and contexts
 - Minimize to tray icon - double-click the icon or Win-Alt-T to hide or show the app
 - Keyboard shortcuts:
    - O or Ctrl+O: open todo.txt file
    - C or Ctrl+N: new todo.txt file
    - N: new task
    - J: next task
    - K: prev task
    - X: toggle task completion
    - A: archive tasks
    - D or Del or Backspace: delete task (with confirmation)
    - U or F2: update task
    - T: append text to selected tasks
    - F: filter tasks (free-text, one filter condition per line)
    - 0: clear filter
    - 1-9: apply numbered filter preset
    - . or F5: reload tasks from file
    - ?: show help
    - Alt+Up: increase priority
    - Alt+Down: decrease priority
    - Alt+Left/Right: clear priority
    - Ctrl+Alt+Up: increase due date by 1 day
    - Ctrl+Alt+Down: decrease due date by 1 day
    - Ctrl+Alt+Left/Right: remove due date 
    - Ctrl+C: copy task to clipboard
    - Ctrl+Shift+C: copy task to edit field
    - Win+Alt+T: hide/unhide windows