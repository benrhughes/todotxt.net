using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ToDoLib
{
	public class Task : IComparable
	{
		const string completedPattern = @"^X\s((\d{4})-(\d{2})-(\d{2}))?";
		const string priorityPattern = @"^(?<priority>\([A-Z]\)\s)";
		const string createdDatePattern = @"(?<date>(\d{4})-(\d{2})-(\d{2}))";
		const string dueDatePattern = @"due:(?<date>(\d{4})-(\d{2})-(\d{2}))";
		const string projectPattern = @"(?<proj>\+\w+)";
		const string contextPattern = @"(?<context>\@\w+)";

		public List<string> Projects { get; set; }
		public List<string> Contexts { get; set; }
		public string DueDate { get; set; }
		public string CompletedDate { get; set; }
		public string CreationDate { get; set; }
		public string Priority { get; set; }
		public string Body { get; set; }
		public string Raw { get; set; }

		private bool _completed;
		public bool Completed
		{
			get
			{
				return _completed;
			}

			set
			{
				_completed = value;
				if (_completed)
				{
					this.CompletedDate = DateTime.Now.ToString("yyyy-MM-dd");
					this.Priority = "";
				}
				else
				{
					this.CompletedDate = "";
				}
			}
		}

        /// <summary>
        /// Test task Due
        /// -1 task overDue
        /// 0 task due today
        /// 1 task not due
        /// </summary>
        public int IsTaskDue
        {
            set { }
            get
            {
                if (Completed)
                    return 1;

                DateTime tmp = new DateTime();

                if (DateTime.TryParse(DueDate, out tmp))
                {
                    if (tmp < DateTime.Today)
                        return -1;
                    if (tmp == DateTime.Today)
                        return 0;
                    return 1;
                }
                else {
                    return 1;
                }
            }
        }

		// Parsing needs to comply with these rules: https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format

		//TODO priority regex need to only recognice upper case single chars
		public Task(string raw)
		{
			Raw = raw.Replace(Environment.NewLine, ""); //make sure it's just on one line

			// because we are removing matches as we go, the order we process is important. It must be:
			// - completed
			// - priority
			// - due date
			// - created date
			// - projects | contexts
			// What we have left is the body

			var reg = new Regex(completedPattern, RegexOptions.IgnoreCase);
			var s = reg.Match(raw).Value.Trim();

			if (string.IsNullOrEmpty(s))
			{
				Completed = false;
				CompletedDate = "";
			}
			else
			{
				Completed = true;
				if (s.Length > 1)
					CompletedDate = s.Substring(2);
			}
			raw = reg.Replace(raw, "");


			reg = new Regex(priorityPattern, RegexOptions.IgnoreCase);
			Priority = reg.Match(raw).Groups["priority"].Value.Trim();
			raw = reg.Replace(raw, "");

			reg = new Regex(dueDatePattern);
			DueDate = reg.Match(raw).Groups["date"].Value.Trim();
			raw = reg.Replace(raw, "");

			reg = new Regex(createdDatePattern);
			CreationDate = reg.Match(raw).Groups["date"].Value.Trim();
			raw = reg.Replace(raw, "");

			Projects = new List<string>();
			reg = new Regex(projectPattern);
			var projects = reg.Matches(raw);

			foreach (Match project in projects)
			{
				var p = project.Groups["proj"].Value.Trim();
				Projects.Add(p);
			}

			raw = reg.Replace(raw, "");


			Contexts = new List<string>();
			reg = new Regex(contextPattern);
			var contexts = reg.Matches(raw);

			foreach (Match context in contexts)
			{
				var c = context.Groups["context"].Value.Trim();
				Contexts.Add(c);
			}

			raw = reg.Replace(raw, "");


			Body = raw.Trim();
		}

		public Task(string priority, List<string> projects, List<string> contexts, string body, string dueDate = "", bool completed = false)
		{
			Priority = priority;
			Projects = projects;
			Contexts = contexts;
			DueDate = dueDate;
			Body = body;
			Completed = completed;
		}

		public override string ToString()
		{
			string str = "";
			if (!string.IsNullOrEmpty(Raw)) // always use Raw if possible as it will preserve placement of projects and contexts
			{
				var reg = new Regex(completedPattern, RegexOptions.IgnoreCase);
				var rawCompleted = reg.IsMatch(Raw);

				str = Raw;

				// we only need to mess with the raw string if its completed status has changed
				if (rawCompleted != Completed)
				{
					if (Completed)
					{
						str = Regex.Replace(Raw, priorityPattern, "");
						str = "x " + CompletedDate + " " + str;
					}
					else
					{
						str = reg.Replace(Raw, "").Trim();
					}
				}

			}
			else
			{
				str = string.Format("{0}{1}{2} {3} {4}",
					Completed ? "x " + CompletedDate + " " : "",
					Priority == null ? "" : Priority + " ",
					Body, string.Join(" ", Projects), string.Join(" ", Contexts));
			}

			return str;
		}

		public int CompareTo(object obj)
		{
			var other = (Task)obj;

			return string.Compare(this.Raw, other.Raw);
		}

		public void IncPriority()
		{
			ChangePriority(-1);
		}

		public void DecPriority()
		{
			ChangePriority(1);
		}

		public void SetPriority(char priority)
		{
			var priorityString = char.IsLetter(priority) ? new string(new char[] { '(', priority, ')' }) : "";

			if (!Raw.IsNullOrEmpty())
			{
				if (Priority.IsNullOrEmpty())
					Raw = priorityString + " " + Raw;
				else
					Raw = Raw.Replace(Priority, priorityString);
			}

			Raw = Raw.Trim();

			Priority = priorityString;
		}

		// NB, you need asciiShift +1 to go from A to B, even though that's a 'decrease' in priority
		private void ChangePriority(int asciiShift)
		{
			if (Priority.IsNullOrEmpty())
			{
				SetPriority('A');
			}
			else
			{
				var current = Priority[1];

				var newPriority = (Char)((int)(current) + asciiShift);

				if (Char.IsLetter(newPriority))
				{
					SetPriority(newPriority);
				}
			}
		}
	}
}