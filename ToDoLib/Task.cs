using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ToDoLib
{
    public class Task
    {
        const string priorityPattern = @"^(?<priority>\(\w\)).*";
        const string projectPattern = @".*(?<proj>\+\w*\s?).*";
        const string contextPattern = @".*(?<context>\@\w*\s?).*";

        public string Project { get; set; }
        public string Context { get; set; }
        public string Priority { get; set; }
        public string Body { get; set; }
        public string Raw { get; set; }

        public Task(string raw)
        {
            Raw = raw;

            var reg = new Regex(priorityPattern);
            Priority = reg.Match(raw).Groups["priority"].Value.Trim();
            if (Priority.Length >0)
                raw = raw.Replace(Priority, "");

            reg = new Regex(projectPattern);
            Project = reg.Match(raw).Groups["proj"].Value.Trim();
            if (Project.Length >0)
                raw = raw.Replace(Project, "");

            reg = new Regex(contextPattern);
            Context = reg.Match(raw).Groups["context"].Value.Trim();
            if (Context.Length >0)
                raw = raw.Replace(Context, "");

            Body = raw.Trim();
        }

        public Task(string priority, string project, string context, string body)
        {
            Priority = priority;
            Project = project;
            Context = context;
            Body = body;
        }

        public override string ToString()
        {
            return Raw;
        }

        
    }
}
