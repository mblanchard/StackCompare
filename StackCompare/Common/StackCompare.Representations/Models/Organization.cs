using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace StackCompare.Representations.Models
{
    [DebuggerDisplay("{Name}")]
    public class Organization
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }
        public string Description { get; set; }
        public List<Tool> Tools { get; set; } = new List<Tool>();

        public override bool Equals(object obj)
        {
            return Name == ((Organization)obj)?.Name;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
