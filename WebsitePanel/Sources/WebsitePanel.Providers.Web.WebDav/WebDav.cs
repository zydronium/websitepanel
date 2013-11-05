﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebsitePanel.Providers;
using WebsitePanel.Providers.Web;
using Microsoft.Web.Administration;
using WebsitePanel.Providers.Web.Extensions;

namespace WebsitePanel.Providers.Web
{
    public class WebDav : IWebDav
    {
        #region Fields

        private string _usersDomain;        

        #endregion

        public WebDav(string domain)
        {
            _usersDomain = domain;
        }

        public void CreateWebDavRule(string organizationId, string folder, WebDavFolderRule rule)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection authoringRulesSection = config.GetSection("system.webServer/webdav/authoringRules", string.Format("{0}/{1}/{2}", _usersDomain, organizationId, folder));

                ConfigurationElementCollection authoringRulesCollection = authoringRulesSection.GetCollection();

                ConfigurationElement addElement = authoringRulesCollection.CreateElement("add");

                if (rule.Users.Any())
                {
                    addElement["users"] = string.Join(", ", rule.Users.Select(x => x.ToString()).ToArray());
                }

                if (rule.Roles.Any())
                {
                    addElement["roles"] = string.Join(", ", rule.Roles.Select(x => x.ToString()).ToArray());
                }

                if (rule.Pathes.Any())
                {
                    addElement["path"] = string.Join(", ", rule.Pathes.ToArray());
                }

                addElement["access"] = rule.AccessRights;
                authoringRulesCollection.Add(addElement);

                serverManager.CommitChanges();
            }
        }
        public bool DeleteWebDavRule(string organizationId, string folder, WebDavFolderRule rule)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection authoringRulesSection = config.GetSection("system.webServer/webdav/authoringRules", string.Format("{0}/{1}/{2}", _usersDomain, organizationId, folder));

                ConfigurationElementCollection authoringRulesCollection = authoringRulesSection.GetCollection();

                var toDeleteRule = authoringRulesCollection.FindWebDavRule(rule);

                if (toDeleteRule != null)
                {
                    authoringRulesCollection.Remove(toDeleteRule);
                    serverManager.CommitChanges();
                    return true;
                }
                return false;
            }
        }

        public bool SetFolderWebDavRules(string organizationId, string folder, WebDavFolderRule[] newRules)
        {
            try
            {
                if (DeleteAllWebDavRules(organizationId, folder))
                {
                    if (newRules != null)
                    {
                        foreach (var rule in newRules)
                        {
                            CreateWebDavRule(organizationId, folder, rule);
                        }
                    }

                    return true;
                }
            }
            catch {  }
            return false;
        }

        public WebDavFolderRule[] GetFolderWebDavRules(string organizationId, string folder)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection authoringRulesSection = config.GetSection("system.webServer/webdav/authoringRules", string.Format("{0}/{1}/{2}", _usersDomain, organizationId, folder));

                ConfigurationElementCollection authoringRulesCollection = authoringRulesSection.GetCollection();

                var rules = new List<WebDavFolderRule>();

                foreach (var rule in authoringRulesCollection)
                {
                    rules.Add(rule.ToWebDavFolderRule());
                }

                return rules.ToArray();
            }
        }


        public bool DeleteAllWebDavRules(string organizationId, string folder)
        {
            try
            {
                using (ServerManager serverManager = new ServerManager())
                {

                    Configuration config = serverManager.GetApplicationHostConfiguration();
                    //ConfigurationSection authoringRulesSection = config.GetSection("system.webServer/webdav/authoringRules", string.Format("{0}/{1}/{2}", _usersDomain, organizationId, folder));

                    //ConfigurationElementCollection authoringRulesCollection = authoringRulesSection.GetCollection();
                    //authoringRulesCollection.Clear();

                    config.RemoveLocationPath(string.Format("{0}/{1}/{2}", _usersDomain, organizationId, folder));
                    serverManager.CommitChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
