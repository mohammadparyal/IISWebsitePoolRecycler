using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.Administration;

namespace ApplicationPoolRecycler.Models
{
    public class ApplicationDetailInfo
    {
        public string AppPoolName { get; set; }
        public string SiteName { get; set; }
        public List<KeyValuePair<string, string>> ConnectionStrings { get; set; }
        public ObjectState SiteStatus { get; set; } 
    }
}