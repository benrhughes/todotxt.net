using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ToDoLib
{
    public class Task
    {
        const string priorityPattern = @"^(X\s)?(?<priority>\(\w\)).*";
        const string projectPattern = @".*(?<proj>\+\w*\s?).*";
        const string contextPattern = @".*(?<context>\@\w*\s?).*";
        const string completedPattern = @"^X";

        
        public string Project { get; set; }
        public string Context { get; set; }
        public string Priority { get; set; }
        public string Body { get; set; }
        public string Raw { get; set; }
        public bool Completed { get; set; }

        // Parsing needs to comply with these rules: https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format

        //TODO need to allow for multiple projects and context per task
        //TODO priority regex need to only recognice upper case single chars
        //TODO created/due date properties
        public Task(string raw)
        {
            Raw = raw.Replace(Environment.NewLine, ""); //make sure it's just on one line

            var reg = new Regex(priorityPattern, RegexOptions.IgnoreCase);
            Priority = reg.Match(raw).Groups["priority"].Value.Trim();
            if (Priority.Length > 0)
                raw = raw.Replace(Priority, "");

            reg = new Regex(projectPattern);
            Project = reg.Match(raw).Groups["proj"].Value.Trim();
            if (Project.Length > 0)
                raw = raw.Replace(Project, "");

            reg = new Regex(contextPattern);
            Context = reg.Match(raw).Groups["context"].Value.Trim();
            if (Context.Length > 0)
                raw = raw.Replace(Context, "");

            reg = new Regex(completedPattern, RegexOptions.IgnoreCase);
            Completed = reg.IsMatch(raw);
            if (Completed)
                raw = raw.Substring(1); //remove the first char

            Body = raw.Trim();
        }

        public Task(string priority, string project, string context, string body, bool completed = false)
        {
            Priority = priority;
            Project = project;
            Context = context;
            Body = body;
            Completed = completed;
        }

        public override string ToString()
        {
            string str ="";
            if (!string.IsNullOrEmpty(Raw))
            {
                var reg = new Regex(completedPattern, RegexOptions.IgnoreCase);
                var rawCompleted = reg.IsMatch(Raw);
                if (Completed && !rawCompleted)
                    str = "X " + Raw;
                else if (!Completed && rawCompleted)
                    str = Raw.Substring(1).TrimStart();
                else
                    str = Raw;
            }
            else
            {
                str = string.Format("{0}{1} {2} {3} {4}", Completed ? "X " : "", Priority, Body, Project, Context);
            }

            return str;
        }


    }
}
