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
        const string projectPattern = @"\s(?<proj>\+\w+)";
        const string contextPattern = @"\s(?<context>\@\w+)";
        const string completedPattern = @"^X";

        
        public List<string> Projects { get; set; }
        public List<string> Contexts { get; set; }
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

            Projects = new List<string>();
            reg = new Regex(projectPattern);
            var projects = reg.Matches(raw);
 
            foreach (Match project in projects)
            {
                var p = project.Groups["proj"].Value.Trim();
                if (p.Length > 0)
                    Projects.Add(p);
            }

            raw = reg.Replace(raw, "");

            Contexts = new List<string>();
            reg = new Regex(contextPattern);
            var contexts = reg.Matches(raw);

            foreach (Match context in contexts)
            {
                var c = context.Groups["context"].Value.Trim();
                if (c.Length > 0)
                    Contexts.Add(c);
            }

            raw = reg.Replace(raw, "");
            

            reg = new Regex(completedPattern, RegexOptions.IgnoreCase);
            Completed = reg.IsMatch(raw);
            if (Completed)
                raw = raw.Substring(1); //remove the first char

            Body = raw.Trim();
        }

        public Task(string priority, List<string> projects, List<string> contexts, string body, bool completed = false)
        {
            Priority = priority;
            Projects = projects;
            Contexts = contexts;
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
                str = string.Format("{0}{1} {2} {3} {4}", 
                    Completed ? "X " : "", Priority, Body, string.Join(" ", Projects), string.Join(" ", Contexts));
            }

            return str;
        }


    }
}
