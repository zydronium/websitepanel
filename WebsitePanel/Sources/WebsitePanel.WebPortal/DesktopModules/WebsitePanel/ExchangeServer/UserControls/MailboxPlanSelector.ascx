﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MailboxPlanSelector.ascx.cs" Inherits="WebsitePanel.Portal.ExchangeServer.UserControls.MailboxPlanSelector" %>
<asp:DropDownList ID="ddlMailboxPlan" runat="server" CssClass="NormalTextBox" 
    onselectedindexchanged="ddlMailboxPlan_SelectedIndexChanged"></asp:DropDownList>