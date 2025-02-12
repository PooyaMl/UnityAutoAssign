using System;

namespace AutoAssign
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class AutoAssignAttribute : Attribute
    {
        public AssignTarget SourceType { get; }

        public AutoAssignAttribute(AssignTarget sourceType = AssignTarget.Any)
        {
            SourceType = sourceType;
        }
    }
}