using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace StackCompare.Representations.Models
{
    [DebuggerDisplay("{Name}")]
    public class Tool
    {
        public int StackShareId { get; set; }
        public string Description { get; set; }
        public string StackLayer { get; set; }
        public string Name { get; set; }
        public Uri Uri { get; set; }
        public List<Organization> Orgs = new List<Organization>();

        
    }
}
