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
using WebsitePanel.EnterpriseServer;
using WebsitePanel.Providers.HostedSolution;
using WebsitePanel.Providers.ResultObjects;

namespace WebsitePanel.Portal.Lync
{
    public partial class LyncAddLyncUserPlan : WebsitePanelModuleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                PackageContext cntx = ES.Services.Packages.GetPackageContext(PanelSecurity.PackageId);

                string[] archivePolicy = ES.Services.Lync.GetPolicyList(PanelRequest.ItemID, LyncPolicyType.Archiving, null);
                if (archivePolicy != null)
                {
                    foreach (string policy in archivePolicy)
                    {
                        if (policy.ToLower()=="global") continue;
                        string txt = policy.Replace("Tag:","");
                        ddArchivingPolicy.Items.Add( new System.Web.UI.WebControls.ListItem( txt, policy) );
                    }
                }

                if (PanelRequest.GetInt("LyncUserPlanId") != 0)
                {
                    Providers.HostedSolution.LyncUserPlan plan = ES.Services.Lync.GetLyncUserPlan(PanelRequest.ItemID, PanelRequest.GetInt("LyncUserPlanId"));

                    txtPlan.Text = plan.LyncUserPlanName;
                    chkIM.Checked = plan.IM;
                    chkIM.Enabled = false;
                    chkFederation.Checked = plan.Federation;
                    chkConferencing.Checked = plan.Conferencing;
                    chkMobility.Checked = plan.Mobility;
                    chkEnterpriseVoice.Checked = plan.EnterpriseVoice;

                    /* because not used
                    switch (plan.VoicePolicy)
                    {
                        case LyncVoicePolicyType.None:
                            break;
                        case LyncVoicePolicyType.Emergency:
                            chkEmergency.Checked = true;
                            break;
                        case LyncVoicePolicyType.National:
                            chkNational.Checked = true;
                            break;
                        case LyncVoicePolicyType.Mobile:
                            chkMobile.Checked = true;
                            break;
                        case LyncVoicePolicyType.International:
                            chkInternational.Checked = true;
                            break;
                        default:
                            chkNone.Checked = true;
                            break;
                    }
                     */

	                chkRemoteUserAccess.Checked = plan.RemoteUserAccess;
	                chkPublicIMConnectivity.Checked = plan.PublicIMConnectivity;

	                chkAllowOrganizeMeetingsWithExternalAnonymous.Checked = plan.AllowOrganizeMeetingsWithExternalAnonymous;

                    ddTelephony.SelectedIndex = plan.Telephony;

	                tbServerURI.Text = plan.ServerURI;

                    locTitle.Text = plan.LyncUserPlanName;
                    this.DisableControls = true;

                    string planArchivePolicy = ""; 
                    if (plan.ArchivePolicy != null) planArchivePolicy = plan.ArchivePolicy;
                    string planTelephonyDialPlanPolicy = "";
                    if (plan.TelephonyDialPlanPolicy != null) planTelephonyDialPlanPolicy = plan.TelephonyDialPlanPolicy;
                    string planTelephonyVoicePolicy = "";
                    if (plan.TelephonyVoicePolicy != null) planTelephonyVoicePolicy = plan.TelephonyVoicePolicy;

                    ddArchivingPolicy.Items.Clear();
                    ddArchivingPolicy.Items.Add(new System.Web.UI.WebControls.ListItem(planArchivePolicy.Replace("Tag:", ""), planArchivePolicy));
                    ddTelephonyDialPlanPolicy.Items.Clear();
                    ddTelephonyDialPlanPolicy.Items.Add(new System.Web.UI.WebControls.ListItem(planTelephonyDialPlanPolicy.Replace("Tag:", ""), planTelephonyDialPlanPolicy));
                    ddTelephonyVoicePolicy.Items.Clear();
                    ddTelephonyVoicePolicy.Items.Add(new System.Web.UI.WebControls.ListItem(planTelephonyVoicePolicy.Replace("Tag:", ""), planTelephonyVoicePolicy));

                }
                else
                {
                    chkIM.Checked = true;
                    chkIM.Enabled = false;
                    // chkNone.Checked = true; because not used
                    if (cntx != null)
                    {
                        foreach (QuotaValueInfo quota in cntx.QuotasArray)
                        {
                            switch (quota.QuotaId)
                            {
                                case 371:
                                    chkFederation.Checked = Convert.ToBoolean(quota.QuotaAllocatedValue);
                                    chkFederation.Enabled = Convert.ToBoolean(quota.QuotaAllocatedValue);
                                    break;
                                case 372:
                                    chkConferencing.Checked = Convert.ToBoolean(quota.QuotaAllocatedValue);
                                    chkConferencing.Enabled = Convert.ToBoolean(quota.QuotaAllocatedValue);
                                    break;
                                case 375:
                                    chkEnterpriseVoice.Checked = Convert.ToBoolean(quota.QuotaAllocatedValue);
                                    chkEnterpriseVoice.Enabled = Convert.ToBoolean(quota.QuotaAllocatedValue);
                                    break;
                            }
                        }
                    }
                    else
                        this.DisableControls = true;
                }
            }

            chkEnterpriseVoice.Enabled = false;
            chkEnterpriseVoice.Checked = false;

            pnEnterpriseVoice.Visible = false;
            pnServerURI.Visible = false;

            switch (ddTelephony.SelectedIndex)
            {
                case 1:
                break;
                case 2:
                    pnEnterpriseVoice.Visible = true;
                    chkEnterpriseVoice.Checked = true;
                break;
                case 3:
                    pnServerURI.Visible = true;
                break;
                case 4:
                    pnServerURI.Visible = true;
                break;
                
            }

        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            AddPlan();
        }

        protected void btnAccept_Click(object sender, EventArgs e)
        {
            string name = tbTelephoneProvider.Text;

            if (string.IsNullOrEmpty(name)) return;

            ddTelephonyDialPlanPolicy.Items.Clear();
            string[] dialPlan = ES.Services.Lync.GetPolicyList(PanelRequest.ItemID, LyncPolicyType.DialPlan, name);
            if (dialPlan != null)
            {
                foreach (string policy in dialPlan)
                {
                    if (policy.ToLower() == "global") continue;
                    string txt = policy.Replace("Tag:", "");
                    ddTelephonyDialPlanPolicy.Items.Add(new System.Web.UI.WebControls.ListItem(txt, policy));
                }
            }

            ddTelephonyVoicePolicy.Items.Clear();
            string[] voicePolicy = ES.Services.Lync.GetPolicyList(PanelRequest.ItemID, LyncPolicyType.Voice, name);
            if (voicePolicy != null)
            {
                foreach (string policy in voicePolicy)
                {
                    if (policy.ToLower() == "global") continue;
                    string txt = policy.Replace("Tag:", "");
                    ddTelephonyVoicePolicy.Items.Add(new System.Web.UI.WebControls.ListItem(txt, policy));
                }
            }
        }

        private void AddPlan()
        {
            try
            {
                Providers.HostedSolution.LyncUserPlan plan = new Providers.HostedSolution.LyncUserPlan();
                plan.LyncUserPlanName = txtPlan.Text;
                plan.IsDefault = false;

                plan.IM = true;
                plan.Mobility = chkMobility.Checked;
                plan.Federation = chkFederation.Checked;
                plan.Conferencing = chkConferencing.Checked;


                plan.EnterpriseVoice = chkEnterpriseVoice.Checked;

                plan.VoicePolicy = LyncVoicePolicyType.None;

                /* because not used
                if (!plan.EnterpriseVoice)
                {
                    plan.VoicePolicy = LyncVoicePolicyType.None;
                }
                else
                {
                    if (chkEmergency.Checked)
                        plan.VoicePolicy = LyncVoicePolicyType.Emergency;
                    else if (chkNational.Checked)
                        plan.VoicePolicy = LyncVoicePolicyType.National;
                    else if (chkMobile.Checked)
                        plan.VoicePolicy = LyncVoicePolicyType.Mobile;
                    else if (chkInternational.Checked)
                        plan.VoicePolicy = LyncVoicePolicyType.International;
                    else
                        plan.VoicePolicy = LyncVoicePolicyType.None;

                } 
                */

	            plan.RemoteUserAccess = chkRemoteUserAccess.Checked;
	            plan.PublicIMConnectivity = chkPublicIMConnectivity.Checked;

	            plan.AllowOrganizeMeetingsWithExternalAnonymous = chkAllowOrganizeMeetingsWithExternalAnonymous.Checked;

	            plan.Telephony = ddTelephony.SelectedIndex;

	            plan.ServerURI = tbServerURI.Text;

                plan.ArchivePolicy = ddArchivingPolicy.SelectedValue;
                plan.TelephonyDialPlanPolicy = ddTelephonyDialPlanPolicy.SelectedValue;
                plan.TelephonyVoicePolicy = ddTelephonyVoicePolicy.SelectedValue;

                int result = ES.Services.Lync.AddLyncUserPlan(PanelRequest.ItemID,
                                                                                plan);


                if (result < 0)
                {
                    messageBox.ShowResultMessage(result);
                    messageBox.ShowErrorMessage("LYNC_UNABLE_TO_ADD_PLAN");
                    return;
                }

                Response.Redirect(EditUrl("ItemID", PanelRequest.ItemID.ToString(), "lync_userplans",
                    "SpaceID=" + PanelSecurity.PackageId));
            }
            catch (Exception ex)
            {
                messageBox.ShowErrorMessage("LYNC_ADD_PLAN", ex);
            }
        }
    }
}