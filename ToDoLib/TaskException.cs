using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToDoLib
{
    class TaskException : Exception
    {

        public TaskException(string message)
            : base(message)
        {

        }
        public TaskException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }
}
