using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using ApplicationPoolRecycler.Models;
using Microsoft.Web.Administration;
using System.IO;
using System.Xml;

namespace ApplicationPoolRecycler.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            ViewBag.IpAddress = Request.ServerVariables["LOCAL_ADDR"];
            return View(GetAllAppInfo());
        }

        public List<ApplicationPoolInfo> GetAllAppInfo()
        {
            var currentSiteName = HostingEnvironment.ApplicationHost.GetSiteName();
            List<ApplicationPoolInfo> modelList = new List<ApplicationPoolInfo>();
            using (var serverManger = new ServerManager())
            {
                foreach (var site in serverManger.Sites.Where(x => x.State == ObjectState.Started))
                {

                    if (site.Applications.Count > 0 && site.Name != currentSiteName)
                    {
                        foreach (var appPool in site.Applications)
                        {
                            if (!modelList.Exists(x => x.AppPoolName == appPool.ApplicationPoolName))
                            {
                                var model = new ApplicationPoolInfo() { AppPoolName = appPool.ApplicationPoolName, SiteName = site.Name };
                                try
                                {
                                    var section = site.GetWebConfiguration().GetSection("connectionStrings");
                                    if (section != null && section.GetCollection().Count() > 0)
                                    {
                                        model.ConnectionStrings = new List<KeyValuePair<string, string>>();
                                        foreach (var element in section.GetCollection().Where(x => !string.IsNullOrEmpty(x.GetAttributeValue("connectionString").ToString()) && !x.GetAttributeValue("connectionString").ToString().Contains("SQLEXPRESS")))
                                        {
                                            model.ConnectionStrings.Add(new KeyValuePair<string, string>(element.GetAttributeValue("name").ToString(), element.GetAttributeValue("connectionString").ToString()));
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                modelList.Add(model);
                            }
                        }
                    }
                }
            }
            return modelList;
        }

        [HttpPost]
        public JsonResult RecyclePool(string applicationName)
        {
            using (var serverManger = new ServerManager())
            {
                var applicationPool = serverManger.ApplicationPools.FirstOrDefault(x => x.Name == applicationName);
                if (applicationPool != null)
                {
                    if (applicationPool.State == ObjectState.Started)
                    {
                        applicationPool.Recycle();
                        serverManger.CommitChanges();
                        return Json(new { applicationName, @class = "success", message = string.Format("App pool '{0}' Recycled Successfully !!", applicationName) });
                    }
                }
            }
            return Json(new { applicationName, @class = "danger", message = string.Format("Oh no!! Something is wrong !! Please do it mannually :)", applicationName) });
        }

        public ActionResult Result()
        {
            ViewBag.AppPoolName = Session["AppPoolName"];
            Session["AppPoolName"] = null;
            return View();
        }

        [HttpGet]
        public ActionResult Edit(string siteName)
        {
            return View(BindModel(siteName));
        }

        private ApplicationDetailInfo BindModel(string siteName)
        {

            var model = new ApplicationDetailInfo();

            if (!string.IsNullOrEmpty(siteName))
            {
                using (var serverManger = new ServerManager())
                {
                    var site = serverManger.Sites.FirstOrDefault(x => x.Name == siteName);

                    if (site != null)
                    {
                        model.SiteStatus = site.State;
                        model.SiteName = site.Name;
                        var section = site.GetWebConfiguration().GetSection("connectionStrings");
                        if (section != null && section.GetCollection().Count() > 0)
                        {
                            model.ConnectionStrings = new List<KeyValuePair<string, string>>();
                            foreach (var element in section.GetCollection().Where(x => !string.IsNullOrEmpty(x.GetAttributeValue("connectionString").ToString()) && !x.GetAttributeValue("connectionString").ToString().Contains("SQLEXPRESS")))
                            {
                                model.ConnectionStrings.Add(new KeyValuePair<string, string>(element.GetAttributeValue("name").ToString(), element.GetAttributeValue("connectionString").ToString()));
                            }
                        }
                    }
                }
            }

            return model;

        }

        [HttpPost]
        public ActionResult Edit(FormCollection formCollection)
        {
            if (!string.IsNullOrEmpty(formCollection["siteName"]))
            {
                using (var serverManger = new ServerManager())
                {
                    var model = new ApplicationDetailInfo();

                    var site = serverManger.Sites.FirstOrDefault(x => x.Name == formCollection["siteName"]);

                    if (site != null)
                    {
                        model.SiteStatus = site.State;
                        model.SiteName = site.Name;

                        string[] file = Directory.GetFiles(site.Applications[0].VirtualDirectories[0].PhysicalPath, "Web.config");
                        if (file != null && file.Length == 1)
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(file[0]);
                            XmlNodeList configurationStrings = doc.SelectNodes("/configuration/connectionStrings/add");
                            for (int i = 0; i < formCollection.AllKeys.Count(); i++)
                            {
                                if (formCollection.Keys[i] != "siteName" && configurationStrings[i].Attributes["name"].Value == formCollection.Keys[i])
                                {
                                    configurationStrings[i].Attributes["connectionString"].Value = formCollection[formCollection.Keys[i]];
                                }
                            }
                            doc.Save(file[0]);
                            TempData["Message"] = "Site configuration has been changed verify";
                        }

                    }
                }
                return Redirect("/Home/Edit?siteName=" + formCollection["siteName"]);
            }

            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
        }
        [HttpPost]
        public JsonResult ValidateConnectionString(string connectionString)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    conn.Close();
                    return Json("1", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
            }

            return Json("0", JsonRequestBehavior.AllowGet);
        }
    }
}
