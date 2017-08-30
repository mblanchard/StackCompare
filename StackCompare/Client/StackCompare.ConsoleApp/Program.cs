using StackCompare.Representations.Configuration;
using StackCompare.Representations.Models;
using StackCompare.StackShareIntegration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StackCompare.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new StackShareClient();
            var tools = new List<Tool>();
            var config = new GitHubConfig() { };
            tools.Add(new Tool() { Name = "cassandra" });
            //tools.Add(new Tool() { Name = "dot-net" });
            var orgs = client.GetMatchingOrgs(tools, config).ToList();
            foreach(var org in orgs)
            {
                System.Console.WriteLine($"- {org.Name}");
            }
        }
    }
}