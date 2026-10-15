using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Domain.Enums
{
    public class Enums
    {
        public enum TaskItemStatus { Pending = 0, InProgress = 1, Done = 2 }
        public enum TaskPriority { Low = 0, Medium = 1, High = 2 }
        public enum UserRole { User = 0, Admin = 1 }
    }
}
