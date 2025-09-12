using System.ComponentModel;

namespace Client
{
    // @sckaushal: Issue 279: Added description attribute to display in status
    public enum SortType
    {
        
        Alphabetical,
        Completed,
        Context,
        [Description("Due Date")]
        DueDate,
        Priority,
        Project,
        [Description("Order in file")]
        None,
        [Description("Creation Date")]
        Created
    }
}
