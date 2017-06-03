using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApplicationPoolRecycler.Models
{
    public class ApplicationPoolInfo
    {
        public string AppPoolName { get; set; }
        public string SiteName { get; set; }
        public List<KeyValuePair<string,string>> ConnectionStrings { get; set; }
    }
}