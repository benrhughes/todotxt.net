using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ToDoTests
{
    // abstract away dropbox so we can drop it in later
    class FileAccess
    {
        public static IEnumerable<string> RawTaskText()
        {
            return File.ReadAllLines(@"C:\Users\ben\Dropbox\todo\todo.txt");
        }
    }
}
