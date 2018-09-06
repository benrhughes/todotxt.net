using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Client.Utilities
{
    public enum TaskTokenKind
    {
        Description,
        Completion,
        Priority,
        PriorityA,
        PriorityB,
        PriorityC,
        CompletionDate,
        CreationDate,
        Project,
        Context,
        KeyValue
    }

    public class TaskToken
    {
        public TaskToken(TaskTokenKind kind, string value)
        {
            Kind = kind;
            Value = value;
        }

        public TaskTokenKind Kind { get; }
        public string Value { get; set; }
    }

    internal static class TaskParser
    {
        private static readonly Regex DateRegex = new Regex(@"\d{4}-\d{2}-{2}");

        public static IEnumerable<TaskToken> ParseIncompleteTask(string raw)
        {
            var words = raw.Split(null).ToList();

            if (words.Count == 0)
            {
                yield break;
            }

            var priorityToken = ParsePriority(words[0]);
            if (priorityToken != null)
            {
                yield return priorityToken;
                words.RemoveAt(0);
            }

            if (words.Count == 0)
            {
                yield break;
            }

            var creationDateToken = ParseCreationDate(words[0]);
            if (creationDateToken != null)
            {
                yield return creationDateToken;
                words.RemoveAt(0);
            }

            foreach (var word in words)
            {
                yield return ParseDescription(word);
            }
        }

        private static TaskToken ParsePriority(string word)
        {
            if (word.Length != 3)
            {
                return null;
            }

            if (word[0] != '(' || word[2] != ')')
            {
                return null;
            }

            if (word[1] < 'A' || word[1] > 'Z')
            {
                return null;
            }

            TaskTokenKind kind;
            switch (word[1])
            {
                case 'A':
                    kind = TaskTokenKind.PriorityA;
                    break;

                case 'B':
                    kind = TaskTokenKind.PriorityB;
                    break;

                case 'C':
                    kind = TaskTokenKind.PriorityC;
                    break;

                default:
                    kind = TaskTokenKind.Priority;
                    break;
            }

            return new TaskToken(kind, word);
        }

        private static TaskToken ParseCreationDate(string word)
        {
            if (!DateRegex.IsMatch(word))
            {
                return null;
            }

            return new TaskToken(TaskTokenKind.CreationDate, word);
        }

        private static TaskToken ParseDescription(string word)
        {
            if (word.StartsWith("+"))
            {
                return new TaskToken(TaskTokenKind.Project, word);
            }

            if (word.StartsWith("@"))
            {
                return new TaskToken(TaskTokenKind.Context, word);
            }

            if (word.Count(c => c == ':') == 1)
            {
                var colonIndex = word.IndexOf(':');
                if (colonIndex != 0 && colonIndex != (word.Length - 1))
                {
                    return new TaskToken(TaskTokenKind.KeyValue, word);
                }
            }

            return new TaskToken(TaskTokenKind.Description, word);
        }
    }
}