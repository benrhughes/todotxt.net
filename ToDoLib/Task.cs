using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using CommonExtensions;

namespace ToDoLib
{
    public enum Due
    {
        NotDue,
        Today,
        Overdue
    }

    public class Task : IComparable
    {
        private const string CompletedPattern = @"^X\s((\d{4})-(\d{2})-(\d{2}))?";
        private const string PriorityPattern = @"^(?<priority>\([A-Z]\)\s)";
        private const string CreatedDatePattern = @"(?<date>(\d{4})-(\d{2})-(\d{2}))";

        private const string RelativeDatePatternBare =
            @"(?<dateRelative>today|tomorrow|(?<weekday>mon(?:day)?|tue(?:sday)?|wed(?:nesday)?|thu(?:rsday)?|fri(?:day)?|sat(?:urday)?|sun(?:day)?))";
        private const string DueRelativePattern = @"\bdue:" + RelativeDatePatternBare + @"\b";
        private const string ThresholdRelativePattern = @"t:"+ RelativeDatePatternBare;

        private const string DueDatePattern = @"due:(?<date>(\d{4})-(\d{2})-(\d{2}))";
        private const string ThresholdDatePattern = @"t:(?<date>(\d{4})-(\d{2})-(\d{2}))";
        private const string ProjectPattern = @"(?<proj>(?<=^|\s)\+[^\s]+)";
        private const string ContextPattern = @"(^|\s)(?<context>\@[^\s]+)";

        private const string RecurPattern = @"rec:(?<date>\+?\d+[dwmy])";

        public List<string> Projects { get; set; }
        public string PrimaryProject { get; private set; }
        public List<string> Contexts { get; set; }
        public string PrimaryContext { get; private set; }
        public string DueDate { get; set; }
        public string CompletedDate { get; set; }
        public string CreationDate { get; set; }
        public string ThresholdDate { get; set; }
        public string Priority { get; set; }
        public string Body { get; set; }
        public string Raw { get; set; }

        private bool _completed;

        public bool Completed
        {
            get { return _completed; }

            set
            {
                _completed = value;
                if(_completed)
                {
                    CompletedDate = DateTime.Now.ToString("yyyy-MM-dd");
                    Priority = "";
                }
                else
                {
                    CompletedDate = "";
                }
            }
        }

        public Due IsTaskDue
        {
            get
            {
                if(Completed)
                    return Due.NotDue;

                var tmp = new DateTime();

                if(!DateTime.TryParse(DueDate, out tmp))
                    return Due.NotDue;

                if(tmp < DateTime.Today)
                    return Due.Overdue;

                if(tmp == DateTime.Today)
                    return Due.Today;

                return Due.NotDue;
            }
        }

        // Parsing needs to comply with these rules: https://github.com/ginatrapani/todo.txt-touch/wiki/Todo.txt-File-Format

        //TODO priority regex need to only recognice upper case single chars
        public Task(string raw)
        {
            raw = raw.Replace(Environment.NewLine, ""); //make sure it's just on one line

            raw = ParseDate(raw, DueRelativePattern);
            raw = ParseDate(raw, ThresholdRelativePattern);

            //Set Raw string after replacing relative date but before removing matches
            Raw = raw;

            // because we are removing matches as we go, the order we process is important. It must be:
            // - completed
            // - priority
            // - due date
            // - created date
            // - projects | contexts
            // What we have left is the body

            var reg = new Regex(CompletedPattern, RegexOptions.IgnoreCase);
            var s = reg.Match(raw).Value.Trim();

            if(string.IsNullOrEmpty(s))
            {
                Completed = false;
                CompletedDate = "";
            }
            else
            {
                Completed = true;
                if(s.Length > 1)
                    CompletedDate = s.Substring(2);
            }

            raw = reg.Replace(raw, "");

            reg = new Regex(PriorityPattern, RegexOptions.IgnoreCase);
            Priority = reg.Match(raw).Groups["priority"].Value.Trim();
            raw = reg.Replace(raw, "");

            reg = new Regex(DueDatePattern);
            DueDate = reg.Match(raw).Groups["date"].Value.Trim();
            raw = reg.Replace(raw, "");

            reg = new Regex(ThresholdDatePattern);
            var match = reg.Match(raw);
            var @group = match.Groups["date"];
            var value = @group.Value;
            ThresholdDate = value.Trim();
            raw = reg.Replace(raw, "");

            reg = new Regex(CreatedDatePattern);
            CreationDate = reg.Match(raw).Groups["date"].Value.Trim();
            raw = reg.Replace(raw, "");
            
            reg = new Regex(RecurPattern);
            Recur = reg.Match(raw).Groups["date"].Value.Trim();
            raw = reg.Replace(raw, "");
            
            var ProjectSet = new SortedSet<string>();
            reg = new Regex(ProjectPattern);
            var projects = reg.Matches(raw);
            PrimaryProject = null;
            int i = 0;
            foreach(Match project in projects)
            {
                var p = project.Groups["proj"].Value.Trim();
                ProjectSet.Add(p);
                if (i == 0)
                {
                    PrimaryProject = p;
                }
                i++;
            }
            Projects = ProjectSet.ToList<string>();
            raw = reg.Replace(raw, "");

            var ContextsSet = new SortedSet<string>();
            reg = new Regex(ContextPattern);
            var contexts = reg.Matches(raw);
            PrimaryContext = null;
            i = 0;
            foreach(Match context in contexts)
            {
                var c = context.Groups["context"].Value.Trim();
                ContextsSet.Add(c);
                if (i == 0)
                {
                    PrimaryContext = c;
                }
                i++;
            }
            Contexts = ContextsSet.ToList<string>();
            raw = reg.Replace(raw, "");

            Body = raw.Trim();
        }

        public string Recur { get; set; }

        private string ParseDate(string raw, string datePattern)
        {

            //Replace relative days with hard date
            //Supports english: 'today', 'tomorrow', and full weekdays ('monday', 'tuesday', etc)
            //If today is the specified weekday, due date will be in one week
            //TODO other languages
            var reg = new Regex(datePattern, RegexOptions.IgnoreCase);
            var regMatch = reg.Match(raw);
            var dateRelative = regMatch.Groups["dateRelative"].Value.Trim();
            if (!dateRelative.IsNullOrEmpty())
            {
                var isValid = false;

                var date = DateTime.Now;
                dateRelative = dateRelative.ToLower();
                if (dateRelative == "today")
                {
                    isValid = true;
                }
                else if (dateRelative == "tomorrow")
                {
                    date = date.AddDays(1);
                    isValid = true;
                }
                else if (regMatch.Groups["weekday"].Success)
                {
                    var count = 0;
                    var lookingForShortDay = dateRelative.Substring(0, 3);

                    //if day of week, add days to today until weekday matches input
                    //if today is the specified weekday, due date will be in one week
                    do
                    {
                        count++;
                        date = date.AddDays(1);
                        isValid = string.Equals(date.ToString("ddd", new CultureInfo("en-US")),
                            lookingForShortDay,
                            StringComparison.CurrentCultureIgnoreCase);
                    } while (!isValid && (count < 7));
                    // The count check is to prevent an endless loop in case of other culture.
                }

                if (isValid)
                    if (datePattern == DueRelativePattern)
                        raw = reg.Replace(raw, "due:" + date.ToString("yyyy-MM-dd"));
                    else if (datePattern == ThresholdRelativePattern)
                        raw = reg.Replace(raw, "t:" + date.ToString("yyyy-MM-dd"));
            }
            return raw;
        }

        public Task(string priority, List<string> projects, List<string> contexts, string body, string dueDate = "",
                    bool completed = false, string thresholdDate = "", string recur = "")
        {
            Priority = priority;
            Projects = projects;
            Contexts = contexts;
            DueDate = dueDate;
            Body = body;
            Completed = completed;
            ThresholdDate = thresholdDate;
            Recur = recur;
        }

        public override string ToString()
        {
            var str = "";
            if(!string.IsNullOrEmpty(Raw))
                // always use Raw if possible as it will preserve placement of projects and contexts
            {
                var reg = new Regex(CompletedPattern, RegexOptions.IgnoreCase);
                var rawCompleted = reg.IsMatch(Raw);

                str = Raw;

                // we only need to mess with the raw string if its completed status has changed
                if(rawCompleted != Completed)
                {
                    if(Completed)
                    {
                        str = Regex.Replace(Raw, PriorityPattern, "");
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
                                    Priority == null ? "N/A" : Priority + " ",
                                    Body,
                                    string.Join(" ", Projects),
                                    string.Join(" ", Contexts));
            }

            return str;
        }

        public int CompareTo(object obj)
        {
            var other = (Task)obj;

            return string.Compare(Raw, other.Raw);
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
            var priorityString = char.IsLetter(priority) ? new string(new[] {'(', priority, ')'}) : "";

            if(!Raw.IsNullOrEmpty())
            {
                if(Priority.IsNullOrEmpty())
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
            if(Priority.IsNullOrEmpty())
                SetPriority('A');
            else
            {
                var current = Priority[1];

                var newPriority = (Char)((current) + asciiShift);

                if(Char.IsLetter(newPriority))
                    SetPriority(newPriority);
            }
        }

        public void ApplyRecur()
        {
            // By default, task recurrence is based on completion date. (https://updatenotes.blog/todotxt-recurring-tasks/)
            // the recurrence may be preceded by ‘+‘ to indicate that the task should repeat from due date.

            if (Recur.Length > 0)
            {
                var pattern = Recur;
                var isStrict = pattern[0] == '+';

                if (isStrict)
                    pattern = pattern.Substring(1);
                
                var period = int.Parse(pattern.Substring(0, pattern.Length-1));
                var periodType = pattern[pattern.Length - 1];

                if (!string.IsNullOrEmpty(DueDate))
                {
                    DueDate = AdvanceDate(DueDate);
                    var dueDateReg = new Regex(DueDatePattern);
                    Raw = dueDateReg.Replace(Raw, "due:" + DueDate);
                }

                if (!string.IsNullOrEmpty(ThresholdDate))
                {
                    ThresholdDate = AdvanceDate(ThresholdDate);
                    var thresholdDateReg = new Regex(ThresholdDatePattern);
                    Raw = thresholdDateReg.Replace(Raw, "t:" + ThresholdDate);
                }

                string AdvanceDate(string date)
                {
                    if (!isStrict) date = DateTime.Now.ToString("yyyy-MM-dd");

                    var dateTime = Convert.ToDateTime(date);

                    switch (periodType)
                    {
                        case 'd':
                            dateTime = dateTime.AddDays(period);
                            break;
                        case 'w': 
                            dateTime = dateTime.AddDays(period * 7);
                            break;
                        case 'm': 
                            dateTime = dateTime.AddMonths(period);
                            break;
                        case 'y': 
                            dateTime = dateTime.AddYears(period);
                            break;
                    }

                    return dateTime.ToString("yyyy-MM-dd");
                }
            }
        }
    }
}
