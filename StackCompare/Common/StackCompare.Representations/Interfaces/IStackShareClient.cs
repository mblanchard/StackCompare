using StackCompare.Representations.Configuration;
using StackCompare.Representations.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StackCompare.Representations.Interfaces
{
    public interface IStackShareClient
    {
        IEnumerable<Organization> GetMatchingOrgs(List<Tool> tools, GitHubConfig config);
    }
}
