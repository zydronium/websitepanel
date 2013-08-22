// Copyright (c) 2012, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using WebsitePanel.Providers.HostedSolution;
using WebsitePanel.EnterpriseServer;

namespace WebsitePanel.Portal.ExchangeServer
{
    public partial class OrganizationSecurityGroups : WebsitePanelModuleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnCreateGroup_Click(object sender, EventArgs e)
        {
            Response.Redirect(EditUrl("ItemID", PanelRequest.ItemID.ToString(), "create_secur_group",
                "SpaceID=" + PanelSecurity.PackageId.ToString()));
        }

        public string GetListEditUrl(string accountId)
        {
            return EditUrl("SpaceID", PanelSecurity.PackageId.ToString(), "secur_group_settings",
                    "AccountID=" + accountId,
                    "ItemID=" + PanelRequest.ItemID.ToString());
        }

        public bool IsNotDefault(string accountType)
        {
            return (ExchangeAccountType)Enum.Parse(typeof(ExchangeAccountType), accountType) != ExchangeAccountType.DefaultSecurityGroup;
        }

        protected void odsSecurityGroupsPaged_Selected(object sender, ObjectDataSourceStatusEventArgs e)
        {
            if (e.Exception != null)
            {
                messageBox.ShowErrorMessage("ORGANIZATION_GET_SECURITY_GROUP", e.Exception);
                e.ExceptionHandled = true;
            }
        }

        protected void gvSecurityGroups_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeleteItem")
            {
                // delete security group
                int accountId = Utils.ParseInt(e.CommandArgument.ToString(), 0);

                try
                {
                    int result = ES.Services.Organizations.DeleteSecurityGroup(PanelRequest.ItemID, accountId);
                    if (result < 0)
                    {
                        messageBox.ShowResultMessage(result);
                        return;
                    }

                    // rebind grid
                    gvGroups.DataBind();
                }
                catch (Exception ex)
                {
                    messageBox.ShowErrorMessage("ORGANIZATION_DELETE_SECURITY_GROUP", ex);
                }
            }
        }

        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)   
        {
            gvGroups.PageSize = Convert.ToInt16(ddlPageSize.SelectedValue);   
       
            // rebind grid   
            gvGroups.DataBind();   
                
        }  
    }
}