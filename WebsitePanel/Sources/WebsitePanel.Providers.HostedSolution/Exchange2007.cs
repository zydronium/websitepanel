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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Configuration;
using System.DirectoryServices;
using System.Security;
using System.Security.Principal;
using System.Security.AccessControl;

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using WebsitePanel.Providers;
using WebsitePanel.Providers.Common;
using WebsitePanel.Providers.HostedSolution;
using WebsitePanel.Providers.Utils;
using WebsitePanel.Server.Utils;
using Microsoft.Exchange.Data.Directory.Recipient;
using Microsoft.Win32;

using Microsoft.Exchange.Data;
using Microsoft.Exchange.Data.Directory;
using Microsoft.Exchange.Data.Storage;

namespace WebsitePanel.Providers.HostedSolution
{
    public class Exchange2007 : HostingServiceProviderBase, IExchangeServer
    {
        #region Static constructor
        static Exchange2007()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveExchangeAssembly);
            ExchangeRegistryPath = "SOFTWARE\\Microsoft\\Exchange\\Setup";
        }
        #endregion

        #region Constants
        private const string CONFIG_CLEAR_QUERYBASEDN = "WebsitePanel.Exchange.ClearQueryBaseDN";
        #endregion

        #region Properties

        internal string RootOU
        {
            get { return ProviderSettings["RootOU"]; }
        }

        internal string StorageGroup
        {
            get { return ProviderSettings["StorageGroup"]; }
        }

        internal string MailboxDatabase
        {
            get { return ProviderSettings["MailboxDatabase"]; }
        }

        internal bool PublicFolderDistributionEnabled
        {
            get { return ProviderSettings.GetBool("PublicFolderDistributionEnabled"); }
        }

        internal int KeepDeletedItemsDays
        {
            get { return Int32.Parse(ProviderSettings["KeepDeletedItemsDays"]); }
        }

        internal int KeepDeletedMailboxesDays
        {
            get { return Int32.Parse(ProviderSettings["KeepDeletedMailboxesDays"]); }
        }

        internal string RootDomain
        {
            get { return ServerSettings.ADRootDomain; }
        }

        internal string MailboxCluster
        {
            get { return ProviderSettings["MailboxCluster"]; }
        }

        internal string PrimaryDomainController
        {
            get { return ProviderSettings["PrimaryDomainController"]; }
        }

        internal string PublicFolderServer
        {
            get { return ProviderSettings["PublicFolderServer"]; }
        }

        internal string OABGenerationServer
        {
            get { return ProviderSettings["OABServer"]; }
        }

        internal static string ExchangeRegistryPath
        {
            get;
            set;
        }

        internal virtual string ExchangeSnapInName
        {
            get { return "Microsoft.Exchange.Management.PowerShell.Admin"; }
        }

        #endregion

        #region IExchangeServer Members

        #region Common
        public bool CheckAccountCredentials(string username, string password)
        {
            return CheckAccountCredentialsInternal(username, password);
        }
        #endregion

        #region Organizations

        /// <summary>
        /// Extend existing organization with exchange functionality
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="securityGroup"></param>
        /// <returns></returns>
        public Organization ExtendToExchangeOrganization(string organizationId, string securityGroup, bool IsConsumer)
        {
            return ExtendToExchangeOrganizationInternal(organizationId, securityGroup, IsConsumer);
        }

        /// <summary>
        /// Creates organization OAB on the Client Access Server
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="securityGroup"></param>
        /// <returns></returns>
        public Organization CreateOrganizationOfflineAddressBook(string organizationId, string securityGroup, string oabVirtualDir)
        {
            return CreateOrganizationOfflineAddressBookInternal(organizationId, securityGroup, oabVirtualDir);
        }

        /// <summary>
        /// Updates organization OAB
        /// </summary>
        /// <param name="oabId"></param>
        public void UpdateOrganizationOfflineAddressBook(string oabId)
        {
            UpdateOrganizationOfflineAddressBookInternal(oabId);
        }


        public string GetOABVirtualDirectory()
        {
            return GetOABVirtualDirectoryInternal();
        }

        public Organization CreateOrganizationAddressBookPolicy(string organizationId, string gal, string addressBook, string roomList, string oab)
        {
            return CreateOrganizationAddressBookPolicyInternal(organizationId, gal, addressBook, roomList, oab);
        }

        public bool DeleteOrganization(string organizationId, string distinguishedName,
            string globalAddressList, string addressList, string roomList, string offlineAddressBook,
            string securityGroup, string addressBookPolicy, List<ExchangeDomainName> acceptedDomains)
        {
            return DeleteOrganizationInternal(organizationId, distinguishedName, globalAddressList,
                addressList, roomList, offlineAddressBook, securityGroup, addressBookPolicy, acceptedDomains);
        }

        public void SetOrganizationStorageLimits(string organizationDistinguishedName, long issueWarningKB, long prohibitSendKB,
            long prohibitSendReceiveKB, int keepDeletedItemsDays)
        {
            SetOrganizationStorageLimitsInternal(organizationDistinguishedName, issueWarningKB, prohibitSendKB,
                prohibitSendReceiveKB, keepDeletedItemsDays);
        }

        public ExchangeItemStatistics[] GetMailboxesStatistics(string organizationDistinguishedName)
        {
            return GetMailboxesStatisticsInternal(organizationDistinguishedName);
        }
        #endregion

        #region Domains

        public string[] GetAuthoritativeDomains()
        {
            return GetAuthoritativeDomainsInternal();
        }

        public void AddAuthoritativeDomain(string domain)
        {
            CreateAuthoritativeDomainInternal(domain);
        }

        public void DeleteAuthoritativeDomain(string domain)
        {
            DeleteAuthoritativeDomainInternal(domain);
        }

        public void ChangeAcceptedDomainType(string domainName, ExchangeAcceptedDomainType domainType)
        {
            ChangeAcceptedDomainTypeInternal(domainName, domainType);
        }
        #endregion

        #region Mailboxes

        public ExchangeMailbox GetMailboxPermissions(string organizationId, string accountName)
        {
            return GetMailboxPermissionsInternal(organizationId, accountName, null);
        }

        public void SetMailboxPermissions(string organizationId, string accountName, string[] sendAsAccounts, string[] fullAccessAccounts)
        {
            SetMailboxPermissionsInternal(organizationId, accountName, sendAsAccounts, fullAccessAccounts);
        }

        public void DeleteMailbox(string accountName)
        {
            DeleteMailboxInternal(accountName);
        }

        public ExchangeMailbox GetMailboxGeneralSettings(string accountName)
        {
            return GetMailboxGeneralSettingsInternal(accountName);
        }

        public void SetMailboxGeneralSettings(string accountName, bool hideFromAddressBook, bool disabled)
        {
            SetMailboxGeneralSettingsInternal(accountName, hideFromAddressBook, disabled);
        }

        public ExchangeMailbox GetMailboxMailFlowSettings(string accountName)
        {
            return GetMailboxMailFlowSettingsInternal(accountName);
        }

        public void SetMailboxMailFlowSettings(string accountName, bool enableForwarding,
            string forwardingAccountName, bool forwardToBoth, string[] sendOnBehalfAccounts,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            SetMailboxMailFlowSettingsInternal(accountName, enableForwarding, forwardingAccountName,
                forwardToBoth, sendOnBehalfAccounts, acceptAccounts, rejectAccounts, requireSenderAuthentication);
        }

        public ExchangeMailbox GetMailboxAdvancedSettings(string accountName)
        {
            return GetMailboxAdvancedSettingsInternal(accountName);
        }

        public void SetMailboxAdvancedSettings(string organizationId, string accountName, bool enablePOP,
            bool enableIMAP, bool enableOWA, bool enableMAPI, bool enableActiveSync,
            long issueWarningKB, long prohibitSendKB, long prohibitSendReceiveKB, int keepDeletedItemsDays, int maxRecipients, int maxSendMessageSizeKB,
            int maxReceiveMessageSizeKB, bool enabledLitigationHold, long recoverabelItemsSpace, long recoverabelItemsWarning, string litigationHoldUrl, string litigationHoldMsg)
        {
            SetMailboxAdvancedSettingsInternal(organizationId, accountName, enablePOP, enableIMAP, enableOWA,
                enableMAPI, enableActiveSync, issueWarningKB,
                prohibitSendKB, prohibitSendReceiveKB, keepDeletedItemsDays, maxRecipients, maxSendMessageSizeKB, maxReceiveMessageSizeKB,
                enabledLitigationHold, recoverabelItemsSpace, recoverabelItemsWarning, litigationHoldUrl, litigationHoldMsg );
        }

        public ExchangeEmailAddress[] GetMailboxEmailAddresses(string accountName)
        {
            return GetMailboxEmailAddressesInternal(accountName);
        }

        public void SetMailboxEmailAddresses(string accountName, string[] emailAddresses)
        {
            SetMailboxEmailAddressesInternal(accountName, emailAddresses);
        }

        public void SetMailboxPrimaryEmailAddress(string accountName, string emailAddress)
        {
            SetMailboxPrimaryEmailAddressInternal(accountName, emailAddress);
        }

        public ExchangeMailboxStatistics GetMailboxStatistics(string id)
        {
            return GetMailboxStatisticsInternal(id);
        }
        #endregion

        #region Contacts
        public void CreateContact(string organizationId, string organizationDistinguishedName,
            string contactDisplayName, string contactAccountName, string contactEmail, string defaultOrganizationDomain)
        {
            CreateContactInternal(organizationId, organizationDistinguishedName, contactDisplayName,
                contactAccountName, contactEmail, defaultOrganizationDomain);
        }

        public void DeleteContact(string accountName)
        {
            DeleteContactInternal(accountName);
        }

        public ExchangeContact GetContactGeneralSettings(string accountName)
        {
            return GetContactGeneralSettingsInternal(accountName);
        }

        public void SetContactGeneralSettings(string accountName, string displayName, string email,
            bool hideFromAddressBook, string firstName, string initials, string lastName, string address,
            string city, string state, string zip, string country, string jobTitle, string company,
            string department, string office, string managerAccountName, string businessPhone, string fax,
            string homePhone, string mobilePhone, string pager, string webPage, string notes, int useMapiRichTextFormat, string defaultOrganizationDomain)
        {
            SetContactGeneralSettingsInternal(accountName, displayName, email, hideFromAddressBook,
                firstName, initials, lastName, address, city, state, zip, country, jobTitle,
                company, department, office, managerAccountName, businessPhone, fax, homePhone,
                mobilePhone, pager, webPage, notes, useMapiRichTextFormat, defaultOrganizationDomain);
        }

        public ExchangeContact GetContactMailFlowSettings(string accountName)
        {
            return GetContactMailFlowSettingsInternal(accountName);
        }

        public void SetContactMailFlowSettings(string accountName, string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            SetContactMailFlowSettingsInternal(accountName, acceptAccounts, rejectAccounts, requireSenderAuthentication);
        }
        #endregion

        #region Distribution lists
        public void CreateDistributionList(string organizationId, string organizationDistinguishedName,
            string displayName, string accountName, string name, string domain, string managedBy, string[] addressLists)
        {
            CreateDistributionListInternal(organizationId, organizationDistinguishedName, displayName,
                accountName, name, domain, managedBy, addressLists);
        }

        public void DeleteDistributionList(string accountName)
        {
            DeleteDistributionListInternal(accountName);
        }

        public ExchangeDistributionList GetDistributionListGeneralSettings(string accountName)
        {
            return GetDistributionListGeneralSettingsInternal(accountName);
        }

        public void SetDistributionListGeneralSettings(string accountName, string displayName,
            bool hideFromAddressBook, string managedBy, string[] members, string notes, string[] addressLists)
        {
            SetDistributionListGeneralSettingsInternal(accountName, displayName, hideFromAddressBook,
                managedBy, members, notes, addressLists);
        }

        public void AddDistributionListMembers(string accountName, string[] memberAccounts, string[] addressLists)
        {
            AddDistributionListMembersInternal(accountName, memberAccounts, addressLists);
        }


        public void RemoveDistributionListMembers(string accountName, string[] memberAccounts, string[] addressLists)
        {
            RemoveDistributionListMembersInternal(accountName, memberAccounts, addressLists);
        }

        public ExchangeDistributionList GetDistributionListMailFlowSettings(string accountName)
        {
            return GetDistributionListMailFlowSettingsInternal(accountName);
        }

        public void SetDistributionListMailFlowSettings(string accountName, string[] acceptAccounts,
            string[] rejectAccounts, bool requireSenderAuthentication, string[] addressLists)
        {
            SetDistributionListMailFlowSettingsInternal(accountName, acceptAccounts, rejectAccounts, requireSenderAuthentication, addressLists);
        }

        public ExchangeEmailAddress[] GetDistributionListEmailAddresses(string accountName)
        {
            return GetDistributionListEmailAddressesInternal(accountName);
        }

        public void SetDistributionListEmailAddresses(string accountName, string[] emailAddresses, string[] addressLists)
        {
            SetDistributionListEmailAddressesInternal(accountName, emailAddresses, addressLists);
        }

        public void SetDistributionListPrimaryEmailAddress(string accountName, string emailAddress, string[] addressLists)
        {
            SetDistributionListPrimaryEmailAddressInternal(accountName, emailAddress, addressLists);
        }

        public ExchangeDistributionList GetDistributionListPermissions(string organizationId, string accountName)
        {
            return GetDistributionListPermissionsInternal(organizationId, accountName, null);
        }

        public void SetDistributionListPermissions(string organizationId, string accountName, string[] sendAsAccounts, string[] sendOnBehalfAccounts, string[] addressLists)
        {
            SetDistributionListPermissionsInternal(organizationId, accountName, sendAsAccounts, sendOnBehalfAccounts, addressLists);
        }
        #endregion

        #region Public folders
        public void CreatePublicFolder(string organizationDistinguishedName, string organizationId, string securityGroup, string parentFolder,
            string folderName, bool mailEnabled, string accountName, string name, string domain)
        {
            CreatePublicFolderInternal(organizationId, securityGroup, parentFolder, folderName,
                mailEnabled, accountName, name, domain);
        }

        public void DeletePublicFolder(string organizationId, string folder)
        {
            DeletePublicFolderInternal(folder);
        }

        public void EnableMailPublicFolder(string organizationId, string folder, string accountName,
            string name, string domain)
        {
            EnableMailPublicFolderInternal(organizationId, folder, accountName, name, domain);
        }

        public void DisableMailbox(string id)
        {
            DisableMailboxInternal(id);
        }

        internal virtual void DisableMailboxInternal(string id)
        {
            ExchangeLog.LogStart("DisableMailboxIntenal");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                RemoveDevicesInternal(runSpace, id);

                Command cmd = new Command("Disable-Mailbox");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("Confirm", false);
                ExecuteShellCommand(runSpace, cmd);
            }

            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DisableMailboxIntenal");
        }
        public void DisableMailPublicFolder(string organizationId, string folder)
        {
            DisableMailPublicFolderInternal(folder);
        }

        public ExchangePublicFolder GetPublicFolderGeneralSettings(string organizationId, string folder)
        {
            return GetPublicFolderGeneralSettingsInternal(folder);
        }

        public void SetPublicFolderGeneralSettings(string organizationId, string folder, string newFolderName,
             bool hideFromAddressBook, ExchangeAccount[] accounts)
        {
            SetPublicFolderGeneralSettingsInternal(folder, newFolderName, hideFromAddressBook, accounts);
        }
        public ExchangePublicFolder GetPublicFolderMailFlowSettings(string organizationId, string folder)
        {
            return GetPublicFolderMailFlowSettingsInternal(folder);
        }

        public void SetPublicFolderMailFlowSettings(string organizationId, string folder,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            SetPublicFolderMailFlowSettingsInternal(folder, acceptAccounts, rejectAccounts, requireSenderAuthentication);
        }

        public ExchangeEmailAddress[] GetPublicFolderEmailAddresses(string organizationId, string folder)
        {
            return GetPublicFolderEmailAddressesInternal(folder);
        }

        public void SetPublicFolderEmailAddresses(string organizationId, string folder, string[] emailAddresses)
        {
            SetPublicFolderEmailAddressesInternal(folder, emailAddresses);
        }

        public void SetPublicFolderPrimaryEmailAddress(string organizationId, string folder, string emailAddress)
        {
            SetPublicFolderPrimaryEmailAddressInternal(folder, emailAddress);
        }

        public ExchangeItemStatistics[] GetPublicFoldersStatistics(string organizationId, string[] folders)
        {
            return GetPublicFoldersStatisticsInternal(folders);
        }

        public string[] GetPublicFoldersRecursive(string organizationId, string parent)
        {
            return GetPublicFoldersRecursiveInternal(parent);
        }

        public long GetPublicFolderSize(string organizationId, string folder)
        {
            return GetPublicFolderSizeInternal(folder);
        }
        #endregion

        #region ActiveSync
        public void CreateOrganizationActiveSyncPolicy(string organizationId)
        {
            CreateOrganizationActiveSyncPolicyInternal(organizationId);
        }


        public ExchangeActiveSyncPolicy GetActiveSyncPolicy(string organizationId)
        {
            return GetActiveSyncPolicyInternal(organizationId);
        }

        public void SetActiveSyncPolicy(string id, bool allowNonProvisionableDevices, bool attachmentsEnabled,
            int maxAttachmentSizeKB, bool uncAccessEnabled, bool wssAccessEnabled, bool devicePasswordEnabled,
            bool alphanumericPasswordRequired, bool passwordRecoveryEnabled, bool deviceEncryptionEnabled,
            bool allowSimplePassword, int maxPasswordFailedAttempts, int minPasswordLength, int inactivityLockMin,
            int passwordExpirationDays, int passwordHistory, int refreshInterval)
        {
            SetActiveSyncPolicyInternal(id, allowNonProvisionableDevices, attachmentsEnabled,
                maxAttachmentSizeKB, uncAccessEnabled, wssAccessEnabled,
                devicePasswordEnabled, alphanumericPasswordRequired, passwordRecoveryEnabled,
                deviceEncryptionEnabled, allowSimplePassword, maxPasswordFailedAttempts,
                minPasswordLength, inactivityLockMin, passwordExpirationDays, passwordHistory, refreshInterval);
        }
        #endregion

        #region Mobile devices
        public ExchangeMobileDevice[] GetMobileDevices(string accountName)
        {
            return GetMobileDevicesInternal(accountName);
        }
        public ExchangeMobileDevice GetMobileDevice(string id)
        {
            return GetMobileDeviceInternal(id);
        }
        public void WipeDataFromDevice(string id)
        {
            WipeDataFromDeviceInternal(id);
        }
        public void CancelRemoteWipeRequest(string id)
        {
            CancelRemoteWipeRequestInternal(id);
        }
        public void RemoveDevice(string id)
        {
            RemoveDeviceInternal(id);
        }
        #endregion

        #endregion

        #region IHostingServiceProvider Members

        public override void ChangeServiceItemsState(ServiceProviderItem[] items, bool enabled)
        {
            foreach (ServiceProviderItem item in items)
            {
                if (item is Organization)
                {
                    try
                    {
                        // make E2K7 mailboxes disabled
                        Organization org = item as Organization;
                        ChangeOrganizationState(org.DistinguishedName, enabled);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteError(String.Format("Error switching '{0}' {1}", item.Name, item.GetType().Name), ex);
                    }
                }
            }
        }

        public override void DeleteServiceItems(ServiceProviderItem[] items)
        {
            foreach (ServiceProviderItem item in items)
            {
                try
                {
                    if (item is Organization)
                    {
                        Organization org = item as Organization;
                        DeleteOrganization(org.OrganizationId, org.DistinguishedName, org.GlobalAddressList,
                            org.AddressList, org.RoomsAddressList, org.OfflineAddressBook, org.SecurityGroup, org.AddressBookPolicy, null);
                    }
                    else if (item is ExchangeDomain)
                    {
                        DeleteAcceptedDomain(null, item.Name);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteError(String.Format("Error deleting '{0}' {1}", item.Name, item.GetType().Name), ex);
                }
            }
        }

        public override ServiceProviderItemDiskSpace[] GetServiceItemsDiskSpace(ServiceProviderItem[] items)
        {
            List<ServiceProviderItemDiskSpace> itemsDiskspace = new List<ServiceProviderItemDiskSpace>();

            // update items with diskspace
            foreach (ServiceProviderItem item in items)
            {
                if (item is Organization)
                {
                    try
                    {
                        Log.WriteStart(String.Format("Calculating '{0}' disk space", item.Name));
                        Organization org = item as Organization;
                        // calculate disk space
                        ServiceProviderItemDiskSpace diskspace = new ServiceProviderItemDiskSpace();
                        diskspace.ItemId = item.Id;
                        diskspace.DiskSpace = CalculateOrganizationDiskSpace(org.OrganizationId, org.DistinguishedName);
                        itemsDiskspace.Add(diskspace);

                        Log.WriteEnd(String.Format("Calculating '{0}' disk space", item.Name));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteError(String.Format("Error calculating '{0}' Exchange organization disk space", item.Name), ex);
                    }
                }
            }

            return itemsDiskspace.ToArray();
        }

        #endregion

        #region Common
        private bool CheckAccountCredentialsInternal(string username, string password)
        {
            try
            {
                string path = ConvertDomainName(RootDomain);
                DirectoryEntry entry = new DirectoryEntry(path, username, password);
                //Bind to the native AdsObject to force authentication.
                object obj = entry.NativeObject;

                DirectorySearcher search = new DirectorySearcher(entry);

                search.Filter = string.Format("(userPrincipalName={0})", username);
                search.PropertiesToLoad.Add("cn");
                SearchResult result = search.FindOne();

                if (result == null)
                {
                    return false;
                }

                //Update the new path to the user in the directory.
                path = result.Path;
                string filterAttribute = (string)result.Properties["cn"][0];
            }
            catch (Exception)
            {
                return false;
                //throw new Exception("Error authenticating user. " + ex.Message);
            }
            return true;
        }
        #endregion

        #region Organizations

        /// <summary>
        /// Creates organization on Mail Server
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        internal virtual Organization ExtendToExchangeOrganizationInternal(string organizationId, string securityGroup, bool IsConsumer)
        {
            ExchangeLog.LogStart("CreateOrganizationInternal");
            ExchangeLog.DebugInfo("  Organization Id: {0}", organizationId);

            ExchangeTransaction transaction = StartTransaction();
            Organization info = new Organization();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                string server = GetServerName();
                string securityGroupPath = AddADPrefix(securityGroup);

                //Create mail enabled organization security group
                EnableMailSecurityDistributionGroup(runSpace, securityGroup, organizationId);
                transaction.RegisterMailEnabledDistributionGroup(securityGroup);
                UpdateSecurityDistributionGroup(runSpace, securityGroup, organizationId, IsConsumer);

                //create GAL
                string galId = CreateGlobalAddressList(runSpace, organizationId);
                transaction.RegisterNewGlobalAddressList(galId);
                ExchangeLog.LogInfo("  Global Address List: {0}", galId);
                UpdateGlobalAddressList(runSpace, galId, securityGroupPath);

                //create AL
                string alId = CreateAddressList(runSpace, organizationId);
                transaction.RegisterNewAddressList(alId);
                ExchangeLog.LogInfo("  Address List: {0}", alId);
                UpdateAddressList(runSpace, alId, securityGroupPath);

                //create RAL
                string ralId = CreateRoomsAddressList(runSpace, organizationId);
                transaction.RegisterNewRoomsAddressList(ralId);
                ExchangeLog.LogInfo("  Rooms Address List: {0}", ralId);
                UpdateAddressList(runSpace, ralId, securityGroupPath);

                //create ActiveSync policy
                string asId = CreateActiveSyncPolicy(runSpace, organizationId);
                transaction.RegisterNewActiveSyncPolicy(asId);
                ExchangeLog.LogInfo("  ActiveSync Policy: {0}", asId);

                //storage group
                string storageGroupId = CreateStorageGroup(runSpace, StorageGroup, server);
                ExchangeLog.LogInfo("  Storage Group: {0}", storageGroupId);

                //mailbox database
                string databaseId = CreateMailboxDatabase(runSpace, MailboxDatabase, storageGroupId);
                ExchangeLog.LogInfo("  Database: {0}", databaseId);

                info.AddressList = alId;
                info.GlobalAddressList = galId;
                info.RoomsAddressList = ralId;
                info.OrganizationId = organizationId;
                info.Database = databaseId;

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreateOrganizationInternal", ex);
                RollbackTransaction(transaction);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CreateOrganizationInternal");
            return info;
        }

        private void CheckServiceSettings()
        {
            if (!ServerSettings.ADEnabled)
                throw new Exception("Active Directory is not enabled. Check server settings.");
            if (string.IsNullOrEmpty(RootDomain))
                throw new Exception("Active Directory root domain is not specified. Check server settings.");
            if (string.IsNullOrEmpty(RootOU))
                throw new Exception("Active Directory root organizational unit is not specified. Check provider settings.");
            if (string.IsNullOrEmpty(PrimaryDomainController))
                throw new Exception("Primary Domain Controller is not specified. Check provider settings.");
        }

        private string GetOABVirtualDirectoryInternal()
        {
            ExchangeLog.LogStart("GetOABVirtualDirectoryInternal");
            Runspace runSpace = null;
            string virtualDir = null;
            try
            {
                runSpace = OpenRunspace();


                string server = GetServerName();
                Command cmd = new Command("Get-OabVirtualDirectory");
                cmd.Parameters.Add("Server", server);

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

                if (result.Count > 0)
                {
                    virtualDir = ObjToString(GetPSObjectProperty(result[0], "Identity"));
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetOABVirtualDirectoryInternal");
            return virtualDir;
        }

        private Organization CreateOrganizationOfflineAddressBookInternal(string organizationId, string securityGroup, string oabVirtualDir)
        {
            ExchangeLog.LogStart("CreateOrganizationOfflineAddressBookInternal");
            ExchangeLog.LogInfo("  Organization Id: {0}", organizationId);
            ExchangeLog.LogInfo("  Security Group: {0}", securityGroup);
            ExchangeLog.LogInfo("  OAB Virtual Dir: {0}", oabVirtualDir);

            ExchangeTransaction transaction = StartTransaction();

            Organization info = new Organization();

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                string server = GetOABGenerationServerName();

                //create OAB
                string oabId = CreateOfflineAddressBook(runSpace, organizationId, server, oabVirtualDir);
                transaction.RegisterNewOfflineAddressBook(oabId);

                string securityGroupId = AddADPrefix(securityGroup);
                UpdateOfflineAddressBook(runSpace, oabId, securityGroupId);
                
                info.OfflineAddressBook = oabId;
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreateOrganizationOfflineAddressBookInternal", ex);
                RollbackTransaction(transaction);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CreateOrganizationOfflineAddressBookInternal");


            return info;
        }

        private string GetOABGenerationServerName()
        {
            string ret = null;
            if (!string.IsNullOrEmpty(OABGenerationServer))
                ret = OABGenerationServer;
            else
                ret = GetServerName();
            return ret;
        }

        private void UpdateOrganizationOfflineAddressBookInternal(string oabId)
        {
            ExchangeLog.LogStart("UpdateOrganizationOfflineAddressBookInternal");
            ExchangeLog.LogInfo("  Id: {0}", oabId);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Update-OfflineAddressBook");
                cmd.Parameters.Add("Identity", oabId);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("UpdateOrganizationOfflineAddressBookInternal");
        }

        internal virtual Organization CreateOrganizationAddressBookPolicyInternal(string organizationId, string gal, string addressBook, string roomList, string oab)
        {
            Organization info = new Organization();
            return info;
        }


        internal virtual bool DeleteOrganizationInternal(string organizationId, string distinguishedName,
            string globalAddressList, string addressList, string roomsAddressList, string offlineAddressBook, string securityGroup, string addressBookPolicy, List<ExchangeDomainName> acceptedDomains)
        {
            ExchangeLog.LogStart("DeleteOrganizationInternal");
            bool ret = true;

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                string ou = ConvertADPathToCanonicalName(distinguishedName);

                //organization cannot be deleted when mailboxes, contacts or distribution groups exist
                /*bool canDelete = CanDeleteOrganization(runSpace, organizationId, ou);
                if (!canDelete)
                {
                    throw new Exception("Organization cannot be deleted as it contains mailboxes, " +
                        "contacts, distribution lists or public folders.\nDelete organization items first.");
                }*/

                if (!DeleteOrganizationMailboxes(runSpace, ou))
                    ret = false;

                if (!DeleteOrganizationContacts(runSpace, ou))
                    ret = false;

                if (!DeleteOrganizationDistributionLists(runSpace, ou))
                    ret = false;

                if (!DeleteOrganizationPublicFolders(runSpace, organizationId))
                    ret = false;


                //delete OAB
                try
                {
                    if (!string.IsNullOrEmpty(offlineAddressBook))
                        DeleteOfflineAddressBook(runSpace, offlineAddressBook);
                }
                catch (Exception ex)
                {
                    ret = false;
                    ExchangeLog.LogError("Could not delete Offline Address Book " + offlineAddressBook, ex);
                }

                //delete AL
                try
                {
                    if (!string.IsNullOrEmpty(addressList))
                        DeleteAddressList(runSpace, addressList);
                }
                catch (Exception ex)
                {
                    ret = false;
                    ExchangeLog.LogError("Could not delete Address List " + addressList, ex);
                }

                //delete RAL (Rooms Address List)
                try
                {
                    if (!string.IsNullOrEmpty(roomsAddressList))
                        DeleteAddressList(runSpace, roomsAddressList);
                }
                catch (Exception ex)
                {
                    ret = false;
                    ExchangeLog.LogError("Could not delete Rooms Address List " + roomsAddressList, ex);
                }

                //delete GAL
                try
                {
                    if (!string.IsNullOrEmpty(globalAddressList))
                        DeleteGlobalAddressList(runSpace, globalAddressList);
                }
                catch (Exception ex)
                {
                    ret = false;
                    ExchangeLog.LogError("Could not delete Global Address List " + globalAddressList, ex);
                }

                //delete ActiveSync policy
                try
                {
                    DeleteActiveSyncPolicy(runSpace, organizationId);
                }
                catch (Exception ex)
                {
                    ret = false;
                    ExchangeLog.LogError("Could not delete ActiveSyncPolicy " + organizationId, ex);
                }

                //disable mail security distribution group
                try
                {
                    DisableMailSecurityDistributionGroup(runSpace, securityGroup);
                }
                catch (Exception ex)
                {
                    ret = false;
                    ExchangeLog.LogError("Could not disable mail security distribution group " + securityGroup, ex);
                }

                if (!DeleteOrganizationAcceptedDomains(runSpace, acceptedDomains))
                    ret = false;
            }
            catch (Exception ex)
            {
                ret = false;
                ExchangeLog.LogError("DeleteOrganizationInternal", ex);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DeleteOrganizationInternal");
            return ret;
        }

        private bool CanDeleteOrganization(Runspace runSpace, string organizationId, string ou)
        {
            ExchangeLog.LogStart("CanDeleteOrganization");
            bool ret = true;

            Command cmd = new Command("Get-Mailbox");
            cmd.Parameters.Add("OrganizationalUnit", ou);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
                ret = false;

            if (ret)
            {
                cmd = new Command("Get-MailContact");
                cmd.Parameters.Add("OrganizationalUnit", ou);
                result = ExecuteShellCommand(runSpace, cmd);
                if (result != null && result.Count > 0)
                    ret = false;
            }

            if (ret)
            {
                cmd = new Command("Get-DistributionGroup");
                cmd.Parameters.Add("OrganizationalUnit", ou);
                cmd.Parameters.Add("RecipientTypeDetails", "MailUniversalDistributionGroup");
                result = ExecuteShellCommand(runSpace, cmd);
                if (result != null && result.Count > 0)
                    ret = false;
            }

            if (ret)
            {
                cmd = new Command("Get-PublicFolder");
                cmd.Parameters.Add("Identity", "\\" + organizationId);
                cmd.Parameters.Add("GetChildren", new SwitchParameter(true));
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);

                result = ExecuteShellCommand(runSpace, cmd);
                if (result != null && result.Count > 0)
                    ret = false;
            }

            ExchangeLog.LogEnd("CanDeleteOrganization");
            return ret;
        }

        internal bool DeleteOrganizationMailboxes(Runspace runSpace, string ou)
        {
            ExchangeLog.LogStart("DeleteOrganizationMailboxes");
            bool ret = true;

            Command cmd = new Command("Get-Mailbox");
            cmd.Parameters.Add("OrganizationalUnit", ou);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                foreach (PSObject obj in result)
                {
                    string id = null;
                    try
                    {
                        id = ObjToString(GetPSObjectProperty(obj, "Identity"));
                        RemoveDevicesInternal(runSpace, id);

                        RemoveMailbox(runSpace, id);
                    }
                    catch (Exception ex)
                    {
                        ret = false;
                        ExchangeLog.LogError(string.Format("Can't delete mailbox {0}", id), ex);
                    }
                }
            }

            ExchangeLog.LogEnd("DeleteOrganizationMailboxes");
            return ret;
        }

        internal bool DeleteOrganizationContacts(Runspace runSpace, string ou)
        {
            ExchangeLog.LogStart("DeleteOrganizationContacts");
            bool ret = true;

            Command cmd = new Command("Get-MailContact");
            cmd.Parameters.Add("OrganizationalUnit", ou);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                foreach (PSObject obj in result)
                {
                    string id = null;
                    try
                    {
                        id = ObjToString(GetPSObjectProperty(obj, "Identity"));
                        RemoveContact(runSpace, id);
                    }
                    catch (Exception ex)
                    {
                        ret = false;
                        ExchangeLog.LogError(string.Format("Can't delete contact {0}", id), ex);
                    }
                }
            }

            ExchangeLog.LogEnd("DeleteOrganizationContacts");
            return ret;
        }

        internal bool DeleteOrganizationDistributionLists(Runspace runSpace, string ou)
        {
            ExchangeLog.LogStart("DeleteOrganizationDistributionLists");
            bool ret = true;

            Command cmd = new Command("Get-DistributionGroup");
            cmd.Parameters.Add("OrganizationalUnit", ou);
            cmd.Parameters.Add("RecipientTypeDetails", "MailUniversalDistributionGroup");
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                foreach (PSObject obj in result)
                {
                    string id = null;
                    try
                    {
                        id = ObjToString(GetPSObjectProperty(obj, "Identity"));
                        RemoveDistributionGroup(runSpace, id);
                    }
                    catch (Exception ex)
                    {
                        ret = false;
                        ExchangeLog.LogError(string.Format("Can't delete distribution list {0}", id), ex);
                    }
                }
            }

            ExchangeLog.LogEnd("DeleteOrganizationDistributionLists");
            return ret;
        }

        private bool DeleteOrganizationPublicFolders(Runspace runSpace, string organizationId)
        {
            ExchangeLog.LogStart("DeleteOrganizationPublicFolders");
            bool ret = true;

            //Delete public folders.
            string publicFolder = "\\" + organizationId;
            try
            {
                RemovePublicFolder(runSpace, publicFolder);
            }
            catch (Exception ex)
            {
                ret = false;
                ExchangeLog.LogError(string.Format("Can't delete public folder {0}", publicFolder), ex);
            }
            ExchangeLog.LogEnd("DeleteOrganizationPublicFolders");
            return ret;
        }


        internal bool DeleteOrganizationAcceptedDomains(Runspace runSpace, List<ExchangeDomainName> acceptedDomains)
        {
            ExchangeLog.LogStart("DeleteOrganizationAcceptedDomains");

            bool ret = true;

            if (acceptedDomains != null)
            {

                foreach (ExchangeDomainName domain in acceptedDomains)
                {
                    try
                    {
                        DeleteAcceptedDomain(runSpace, domain.DomainName);
                    }
                    catch (Exception ex)
                    {
                        ExchangeLog.LogError(string.Format("Failed to delete accepted domain {0}", domain), ex);
                        ret = false;
                    }
                }
            }
            ExchangeLog.LogEnd("DeleteOrganizationAcceptedDomains");
            return ret;
        }

        private void SetOrganizationStorageLimitsInternal(string organizationDistinguishedName, long issueWarningKB,
            long prohibitSendKB, long prohibitSendReceiveKB, int keepDeletedItemsDays)
        {
            ExchangeLog.LogStart("SetOrganizationStorageLimitsInternal");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationDistinguishedName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                string org = ConvertADPathToCanonicalName(organizationDistinguishedName);

                Unlimited<ByteQuantifiedSize> issueWarningQuota = ConvertKBToUnlimited(issueWarningKB);
                Unlimited<ByteQuantifiedSize> prohibitSendQuota = ConvertKBToUnlimited(prohibitSendKB);
                Unlimited<ByteQuantifiedSize> prohibitSendReceiveQuota = ConvertKBToUnlimited(prohibitSendReceiveKB);
                EnhancedTimeSpan retainDeletedItemsFor = ConvertDaysToEnhancedTimeSpan(keepDeletedItemsDays);

                Command cmd = new Command("Get-Mailbox");
                cmd.Parameters.Add("OrganizationalUnit", org);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

                foreach (PSObject obj in result)
                {
                    string id = ObjToString(GetPSObjectProperty(obj, "Identity"));
                    cmd = new Command("Set-Mailbox");
                    cmd.Parameters.Add("Identity", id);
                    cmd.Parameters.Add("IssueWarningQuota", issueWarningQuota);
                    cmd.Parameters.Add("ProhibitSendQuota", prohibitSendQuota);
                    cmd.Parameters.Add("ProhibitSendReceiveQuota", prohibitSendReceiveQuota);
                    cmd.Parameters.Add("RetainDeletedItemsFor", retainDeletedItemsFor);
                    ExecuteShellCommand(runSpace, cmd);
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetOrganizationStorageLimitsInternal");
        }

        private ExchangeItemStatistics[] GetMailboxesStatisticsInternal(string organizationDistinguishedName)
        {
            ExchangeLog.LogStart("GetMailboxesStatisticsInternal");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationDistinguishedName);

            List<ExchangeItemStatistics> ret = new List<ExchangeItemStatistics>();

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                string org = ConvertADPathToCanonicalName(organizationDistinguishedName);

                Command cmd = new Command("Get-Mailbox");
                cmd.Parameters.Add("OrganizationalUnit", org);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

                foreach (PSObject obj in result)
                {
                    string id = ObjToString(GetPSObjectProperty(obj, "Identity"));

                    cmd = new Command("Get-MailboxStatistics");
                    cmd.Parameters.Add("Identity", id);
                    result = ExecuteShellCommand(runSpace, cmd);
                    if (result != null && result.Count > 0)
                    {
                        PSObject mailbox = result[0];
                        ExchangeItemStatistics info = new ExchangeItemStatistics();
                        info.ItemName = (string)GetPSObjectProperty(mailbox, "DisplayName");
                        Unlimited<ByteQuantifiedSize> totalItemSize =
                            (Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "TotalItemSize");
                        info.TotalSizeMB = ConvertUnlimitedToMB(totalItemSize);
                        uint? itemCount = (uint?)GetPSObjectProperty(mailbox, "ItemCount");
                        info.TotalItems = ConvertNullableToInt32(itemCount);
                        DateTime? lastLogoffTime = (DateTime?)GetPSObjectProperty(mailbox, "LastLogoffTime");
                        DateTime? lastLogonTime = (DateTime?)GetPSObjectProperty(mailbox, "LastLogonTime");
                        info.LastLogoff = ConvertNullableToDateTime(lastLogoffTime);
                        info.LastLogon = ConvertNullableToDateTime(lastLogonTime);
                        ret.Add(info);
                    }
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMailboxesStatisticsInternal");
            return ret.ToArray();
        }

        private void ChangeOrganizationState(string organizationDistinguishedName, bool enabled)
        {
            ExchangeLog.LogStart("ChangeOrganizationState");
            ExchangeLog.DebugInfo("Organization: {0}", organizationDistinguishedName);

            Runspace runSpace = null;
            try
            {
                string ouName = ConvertADPathToCanonicalName(organizationDistinguishedName);
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-Mailbox");
                cmd.Parameters.Add("OrganizationalUnit", ouName);
                cmd.Parameters.Add("Filter", "CustomAttribute2 -ne 'disabled'");
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                foreach (PSObject obj in result)
                {
                    string id = (string)GetPSObjectProperty(obj, "DistinguishedName");
                    ChangeMailboxState(id, enabled);
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("ChangeOrganizationState");
        }


        private long CalculateOrganizationDiskSpace(string organizationId, string organizationDistinguishedName)
        {
            ExchangeLog.LogStart("CalculateOrganizationDiskSpace");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationId);

            long size = 0;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                size += CalculateMailboxDiskSpace(runSpace, organizationDistinguishedName);
                size += CalculatePublicFolderDiskSpace(runSpace, "\\" + organizationId);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CalculateOrganizationDiskSpace");
            return size;
        }

        private long CalculateMailboxDiskSpace(Runspace runSpace, string organizationDistinguishedName)
        {
            ExchangeLog.LogStart("CalculateMailboxDiskSpace");
            ExchangeLog.DebugInfo("Organization DN: {0}", organizationDistinguishedName);

            long size = 0;

            string org = ConvertADPathToCanonicalName(organizationDistinguishedName);

            Command cmd = new Command("Get-Mailbox");
            cmd.Parameters.Add("OrganizationalUnit", org);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

            foreach (PSObject obj in result)
            {
                string id = ObjToString(GetPSObjectProperty(obj, "Identity"));
                cmd = new Command("Get-MailboxStatistics");
                cmd.Parameters.Add("Identity", id);
                result = ExecuteShellCommand(runSpace, cmd);
                if (result != null && result.Count > 0)
                {
                    Unlimited<ByteQuantifiedSize> totalItemSize =
                        (Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(result[0], "TotalItemSize");
                    size += ConvertUnlimitedToBytes(totalItemSize);
                }
            }
            ExchangeLog.LogEnd("CalculateMailboxDiskSpace");
            return size;
        }

        internal virtual long CalculatePublicFolderDiskSpace(Runspace runSpace, string folder)
        {
            ExchangeLog.LogStart("CalculatePublicFolderDiskSpace");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            long size = 0;

            Command cmd = new Command("Get-PublicFolderDatabase");
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                cmd = new Command("Get-PublicFolderStatistics");
                cmd.Parameters.Add("Identity", folder);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                result = ExecuteShellCommand(runSpace, cmd);
                if (result != null && result.Count > 0)
                {
                    PSObject obj = result[0];
                    Unlimited<ByteQuantifiedSize> totalItemSize =
                        (Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(obj, "TotalItemSize");
                    size += ConvertUnlimitedToBytes(totalItemSize);
                }

                cmd = new Command("Get-PublicFolder");
                cmd.Parameters.Add("Identity", folder);
                cmd.Parameters.Add("GetChildren", new SwitchParameter(true));
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                result = ExecuteShellCommand(runSpace, cmd);
                foreach (PSObject obj in result)
                {
                    string id = ObjToString(GetPSObjectProperty(obj, "Identity"));
                    size += CalculatePublicFolderDiskSpace(runSpace, id);
                }
            }
            else
                size = 0;
            ExchangeLog.LogEnd("CalculatePublicFolderDiskSpace");
            return size;
        }

        #endregion

        #region Mailboxes


        private void SetExtendedRights(Runspace runSpace, string identity, string user, string rights)
        {
            ExchangeLog.LogStart("SetExtendedRights");

            Command cmd = new Command("Add-ADPermission");
            cmd.Parameters.Add("Identity", identity);
            cmd.Parameters.Add("User", user);

            cmd.Parameters.Add("ExtendedRights", rights);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("SetExtendedRights");
        }

        private void SetMailboxPermission(Runspace runSpace, string accountName, string user, string accessRights)
        {
            ExchangeLog.LogStart("SetMailboxPermission");

            Command cmd = new Command("Add-MailboxPermission");
            cmd.Parameters.Add("Identity", accountName);
            cmd.Parameters.Add("User", user);
            cmd.Parameters.Add("AccessRights", accessRights);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("SetMailboxPermission");
        }

        private ExchangeAccount[] GetMailboxSendAsAccounts(Runspace runSpace, string organizationId, string accountName)
        {
            ExchangeLog.LogStart("GetMailboxSendAsAccounts");

            string cn = GetMailboxCommonName(runSpace, accountName);
            ExchangeAccount[] ret = GetSendAsAccounts(runSpace, organizationId, cn);

            ExchangeLog.LogEnd("GetMailboxSendAsAccounts");
            return ret;
        }

        private ExchangeAccount[] GetSendAsAccounts(Runspace runSpace, string organizationId, string accountId)
        {
            ExchangeLog.LogStart("GetSendAsAccounts");

            Command cmd = new Command("Get-ADPermission");
            cmd.Parameters.Add("Identity", accountId);
            Collection<PSObject> results = ExecuteShellCommand(runSpace, cmd);
            List<ExchangeAccount> accounts = new List<ExchangeAccount>();
            foreach (PSObject current in results)
            {
                string user = GetPSObjectProperty(current, "User").ToString();

                Array extendedRights = GetPSObjectProperty(current, "ExtendedRights") as Array;

                if (extendedRights != null)
                {
                    foreach (object obj in extendedRights)
                    {
                        string strRightName = obj.ToString();
                        if (string.Compare(strRightName, "Send-as", true) == 0)
                        {
                            ExchangeAccount account = GetOrganizationAccount(runSpace, organizationId, user);
                            if (account != null)
                                accounts.Add(account);
                        }
                    }
                }
            }

            ExchangeLog.LogEnd("GetSendAsAccounts");
            return accounts.ToArray();
        }


        private ExchangeAccount[] GetMailBoxFullAccessAcounts(Runspace runSpace, string organizationId, string accountName)
        {
            ExchangeLog.LogStart("GetMailBoxFullAccessAcounts");

            Command cmd = new Command("Get-MailboxPermission");
            cmd.Parameters.Add("Identity", accountName);
            Collection<PSObject> results = ExecuteShellCommand(runSpace, cmd);


            List<ExchangeAccount> accounts = new List<ExchangeAccount>();
            foreach (PSObject current in results)
            {
                string user = GetPSObjectProperty(current, "User").ToString();

                Array accessRights = GetPSObjectProperty(current, "AccessRights") as Array;

                if (accessRights != null)
                {
                    foreach (object obj in accessRights)
                    {
                        string strRightName = obj.ToString();
                        if (string.Compare(strRightName, "FullAccess", true) == 0)
                        {
                            ExchangeAccount account = GetOrganizationAccount(runSpace, organizationId, user);
                            if (account != null)
                                accounts.Add(account);
                        }
                    }
                }
            }

            ExchangeLog.LogEnd("GetMailBoxFullAccessAcounts");
            return accounts.ToArray();
        }

        private ExchangeMailbox GetMailboxPermissionsInternal(string organizationId, string accountName, Runspace runspace)
        {
            ExchangeLog.LogStart("GetMailboxPermissionsInternal");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("accountName");

            ExchangeMailbox exchangeMailbox = null;
            bool closeRunspace = false;

            try
            {
                if (runspace == null)
                {
                    runspace = OpenRunspace();
                    closeRunspace = true;
                }

                exchangeMailbox = new ExchangeMailbox();
                exchangeMailbox.FullAccessAccounts = GetMailBoxFullAccessAcounts(runspace, organizationId, accountName);
                exchangeMailbox.SendAsAccounts = GetMailboxSendAsAccounts(runspace, organizationId, accountName);
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError(ex);
                throw ex;
            }
            finally
            {
                if (closeRunspace)
                    CloseRunspace(runspace);
            }

            ExchangeLog.LogEnd("GetMailboxPermissionsInternal");
            return exchangeMailbox;
        }


        private void SetSendAsPermissions(Runspace runSpace, ExchangeAccount[] existingAccounts, string accountId, string[] accounts)
        {
            ExchangeLog.LogStart("SetSendAsPermissions");

            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentNullException("accountId");

            if (accounts == null)
                throw new ArgumentNullException("accounts");

            ExchangeTransaction transaction = null;
            try
            {
                transaction = StartTransaction();

                string[] resAccounts = MergeADPermission(runSpace, existingAccounts, accountId, accounts, transaction);
                foreach (string id in resAccounts)
                {
                    SetExtendedRights(runSpace, accountId, id, "Send-as");
                    transaction.AddSendAsPermission(accountId, id);
                }

            }
            catch (Exception)
            {
                RollbackTransaction(transaction);
                throw;
            }
            ExchangeLog.LogEnd("SetSendAsPermissions");
        }


        private void SetMailboxPermissionsInternal(string organizationId, string accountName, string[] sendAsAccounts, string[] fullAccessAccounts)
        {
            ExchangeLog.LogStart("SetMailboxPermissionsInternal");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("accountName");

            if (sendAsAccounts == null)
                throw new ArgumentNullException("sendAsAccounts");


            if (fullAccessAccounts == null)
                throw new ArgumentNullException("fullAccessAccounts");

            Runspace runSpace = null;
            try
            {

                runSpace = OpenRunspace();
                string cn = GetMailboxCommonName(runSpace, accountName);
                ExchangeMailbox mailbox = GetMailboxPermissionsInternal(organizationId, accountName, runSpace);
                SetSendAsPermissions(runSpace, mailbox.SendAsAccounts, cn, sendAsAccounts);
                SetMailboxFullAccessPermissions(runSpace, mailbox.FullAccessAccounts, accountName, fullAccessAccounts);

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError(ex);

                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("SetMailboxPermissionsInternal");
        }


        internal void RemoveMailboxAccessPermission(Runspace runSpace, string accountName, string account, string accessRights)
        {
            ExchangeLog.LogStart("RemoveMailboxFullAccessPermission");

            Command cmd = new Command("Remove-MailboxPermission");
            cmd.Parameters.Add("Identity", accountName);
            cmd.Parameters.Add("User", account);
            cmd.Parameters.Add("AccessRights", accessRights);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("RemoveMailboxFullAccessPermission");
        }


        private string[] MergeMailboxFullAccessPermissions(Runspace runSpace, ExchangeAccount[] existingAccounts, string accountName, string[] newAccounts, ExchangeTransaction transaction)
        {
            ExchangeLog.LogStart("MergeMailboxFullAccessPermissions");
            List<string> temp = new List<string>(newAccounts);

            Array.Sort(newAccounts);
            foreach (ExchangeAccount exist in existingAccounts)
            {
                if (Array.BinarySearch<string>(newAccounts, exist.AccountName, StringComparer.Create(CultureInfo.InvariantCulture, true)) >= 0)
                {
                    temp.Remove(exist.AccountName);
                    continue;
                }

                RemoveMailboxAccessPermission(runSpace, accountName, exist.AccountName, "FullAccess");
                transaction.RemoveMailboxFullAccessPermission(accountName, exist.AccountName);
            }

            ExchangeLog.LogEnd("MergeMailboxFullAccessPermissions");
            return temp.ToArray();
        }


        private void SetMailboxFullAccessPermissions(Runspace runSpace, ExchangeAccount[] existingAccounts, string accountName, string[] accounts)
        {
            ExchangeLog.LogStart("SetMailboxFullAccessPermissions");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("accountName");

            if (accounts == null)
                throw new ArgumentNullException("accounts");

            ExchangeTransaction transaction = StartTransaction();

            try
            {

                string[] resAccounts = MergeMailboxFullAccessPermissions(runSpace, existingAccounts, accountName, accounts, transaction);
                foreach (string id in resAccounts)
                {
                    SetMailboxPermission(runSpace, accountName, id, "FullAccess");
                    transaction.AddMailBoxFullAccessPermission(accountName, id);
                }
            }
            catch (Exception)
            {
                RollbackTransaction(transaction);
                throw;
            }
            ExchangeLog.LogEnd("SetMailboxFullAccessPermissions");

        }


        private void RemoveADPermission(Runspace runSpace, string identity, string account, string accessRights, string extendedRights, string properties)
        {
            ExchangeLog.LogStart("RemoveADPermission");

            Command cmd = new Command("Remove-ADPermission");
            cmd.Parameters.Add("Identity", identity);
            cmd.Parameters.Add("User", account);
            cmd.Parameters.Add("InheritanceType", "All");
            cmd.Parameters.Add("AccessRights", accessRights);
            cmd.Parameters.Add("ExtendedRights", extendedRights);
            cmd.Parameters.Add("ChildObjectTypes", null);
            cmd.Parameters.Add("InheritedObjectType", null);
            cmd.Parameters.Add("Properties", properties);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("RemoveADPermission");
        }

        private void AddADPermission(Runspace runSpace, string identity, string user, string accessRights, string extendedRights, string properties)
        {
            ExchangeLog.LogStart("AddADPermission");

            Command cmd = new Command("Add-ADPermission");
            cmd.Parameters.Add("Identity", identity);
            cmd.Parameters.Add("User", user);
            cmd.Parameters.Add("AccessRights", accessRights);
            cmd.Parameters.Add("ExtendedRights", extendedRights);
            cmd.Parameters.Add("Properties", properties);
            //cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("AddADPermission");
        }


        private string[] MergeADPermission(Runspace runSpace, ExchangeAccount[] existingAccounts, string identity, string[] newAccounts, ExchangeTransaction transaction)
        {
            ExchangeLog.LogStart("MergeADPermission");

            List<string> temp = new List<string>(newAccounts);

            Array.Sort<string>(newAccounts);
            foreach (ExchangeAccount current in existingAccounts)
            {
                if (Array.BinarySearch<string>(newAccounts, current.AccountName, StringComparer.Create(CultureInfo.InvariantCulture, true)) >= 0)
                {
                    temp.Remove(current.AccountName);
                    continue;
                }
                RemoveADPermission(runSpace, identity, current.AccountName, null, "Send-as", null);
                transaction.RemoveSendAsPermission(identity, current.AccountName);
            }

            ExchangeLog.LogEnd("MergeADPermission");
            return temp.ToArray();
        }


        public string CreateMailEnableUser(string upn, string organizationId, string organizationDistinguishedName,
            string securityGroup, string organizationDomain,
            ExchangeAccountType accountType,
            string mailboxDatabase, string offlineAddressBook, string addressBookPolicy,
            string accountName, bool enablePOP, bool enableIMAP,
            bool enableOWA, bool enableMAPI, bool enableActiveSync,
            long issueWarningKB, long prohibitSendKB, long prohibitSendReceiveKB, int keepDeletedItemsDays,
            int maxRecipients, int maxSendMessageSizeKB, int maxReceiveMessageSizeKB, bool hideFromAddressBook, bool IsConsumer, bool enabledLitigationHold, long recoverabelItemsSpace, long recoverabelItemsWarning)
        {
            return CreateMailEnableUserInternal(upn, organizationId, organizationDistinguishedName, accountType,
                                                mailboxDatabase, offlineAddressBook, addressBookPolicy,
                                                accountName, enablePOP, enableIMAP,
                                                enableOWA, enableMAPI, enableActiveSync,
                                                issueWarningKB, prohibitSendKB, prohibitSendReceiveKB,
                                                keepDeletedItemsDays, maxRecipients, maxSendMessageSizeKB, maxReceiveMessageSizeKB, hideFromAddressBook, IsConsumer, enabledLitigationHold, recoverabelItemsSpace, recoverabelItemsWarning);
        }

        internal virtual string CreateMailEnableUserInternal(string upn, string organizationId, string organizationDistinguishedName, ExchangeAccountType accountType,
            string mailboxDatabase, string offlineAddressBook, string addressBookPolicy,
            string accountName, bool enablePOP, bool enableIMAP,
            bool enableOWA, bool enableMAPI, bool enableActiveSync,
            long issueWarningKB, long prohibitSendKB, long prohibitSendReceiveKB, int keepDeletedItemsDays,
            int maxRecipients, int maxSendMessageSizeKB, int maxReceiveMessageSizeKB, bool hideFromAddressBook, bool IsConsumer,bool enabledLitigationHold, long recoverabelItemsSpace, long recoverabelItemsWarning)
        {

            ExchangeLog.LogStart("CreateMailEnableUserInternal");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationId);

            string ret = null;
            ExchangeTransaction transaction = StartTransaction();
            Runspace runSpace = null;

            int attempts = 0;
            string id = null;

            Version exchangeVersion = GetExchangeVersion();

            try
            {
                runSpace = OpenRunspace();
                Command cmd = null;
                Collection<PSObject> result = null;

                //try to enable mail user for 10 times
                while (true)
                {
                    try
                    {

                        //create mailbox
                        cmd = new Command("Enable-Mailbox");
                        cmd.Parameters.Add("Identity", upn);
                        cmd.Parameters.Add("Alias", accountName);
                        if (!(mailboxDatabase == "*" && exchangeVersion >= new Version(14, 0)))
                            cmd.Parameters.Add("Database", mailboxDatabase);
                        if (accountType == ExchangeAccountType.Equipment)
                            cmd.Parameters.Add("Equipment");
                        else if (accountType == ExchangeAccountType.Room)
                            cmd.Parameters.Add("Room");

                        result = ExecuteShellCommand(runSpace, cmd);

                        id = CheckResultObjectDN(result);
                    }
                    catch (Exception ex)
                    {
                        ExchangeLog.LogError(ex);
                    }
                    if (id != null)
                        break;

                    if (attempts > 9)
                        throw new Exception(
                            string.Format("Could not enable mail user '{0}' ", accountName));

                    attempts++;
                    ExchangeLog.LogWarning("Attempt #{0} to enable mail user failed!", attempts);
                    // wait 5 sec
                    System.Threading.Thread.Sleep(5000);
                }

                //transaction.RegisterNewMailbox(id);

                string windowsEmailAddress = ObjToString(GetPSObjectProperty(result[0], "WindowsEmailAddress"));

                //update mailbox
                cmd = new Command("Set-Mailbox");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("OfflineAddressBook", offlineAddressBook);
                cmd.Parameters.Add("EmailAddressPolicyEnabled", false);
                cmd.Parameters.Add("CustomAttribute1", organizationId);
                cmd.Parameters.Add("CustomAttribute3", windowsEmailAddress);
                cmd.Parameters.Add("PrimarySmtpAddress", upn);
                cmd.Parameters.Add("WindowsEmailAddress", upn);

                cmd.Parameters.Add("UseDatabaseQuotaDefaults", new bool?(false));
                cmd.Parameters.Add("UseDatabaseRetentionDefaults", false);
                cmd.Parameters.Add("IssueWarningQuota", ConvertKBToUnlimited(issueWarningKB));
                cmd.Parameters.Add("ProhibitSendQuota", ConvertKBToUnlimited(prohibitSendKB));
                cmd.Parameters.Add("ProhibitSendReceiveQuota", ConvertKBToUnlimited(prohibitSendReceiveKB));
                cmd.Parameters.Add("RetainDeletedItemsFor", ConvertDaysToEnhancedTimeSpan(keepDeletedItemsDays));
                cmd.Parameters.Add("RecipientLimits", ConvertInt32ToUnlimited(maxRecipients));
                cmd.Parameters.Add("MaxSendSize", ConvertKBToUnlimited(maxSendMessageSizeKB));
                cmd.Parameters.Add("MaxReceiveSize", ConvertKBToUnlimited(maxReceiveMessageSizeKB));
                cmd.Parameters.Add("HiddenFromAddressListsEnabled", hideFromAddressBook);
                ExecuteShellCommand(runSpace, cmd);

                //update AD object
                string globalAddressListName = this.GetGlobalAddressListName(organizationId);
                string globalAddressListDN = this.GetGlobalAddressListDN(runSpace, globalAddressListName);
                string path = AddADPrefix(id);
                DirectoryEntry mailbox = GetADObject(path);

                // check if msExchQueryBaseDN must be cleared for Exchange 2010 SP1
                bool clearQueryBaseDN = false;
                if (ConfigurationManager.AppSettings[CONFIG_CLEAR_QUERYBASEDN] != null)
                    clearQueryBaseDN = Boolean.Parse(ConfigurationManager.AppSettings[CONFIG_CLEAR_QUERYBASEDN]);

                if (!(clearQueryBaseDN && (exchangeVersion >= new Version(14, 1))))
                    SetADObjectPropertyValue(mailbox, "msExchQueryBaseDN", globalAddressListDN);

                //SetADObjectPropertyValue(mailbox, "msExchUseOAB", offlineAddressBook);
                mailbox.CommitChanges();
                mailbox.Close();

                //Client Access
                cmd = new Command("Set-CASMailbox");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("ActiveSyncEnabled", enableActiveSync);
                if (enableActiveSync)
                {
                    cmd.Parameters.Add("ActiveSyncMailboxPolicy", organizationId);
                }
                cmd.Parameters.Add("OWAEnabled", enableOWA);
                cmd.Parameters.Add("MAPIEnabled", enableMAPI);
                cmd.Parameters.Add("PopEnabled", enablePOP);
                cmd.Parameters.Add("ImapEnabled", enableIMAP);
                ExecuteShellCommand(runSpace, cmd);

                //calendar settings
                if (accountType == ExchangeAccountType.Equipment || accountType == ExchangeAccountType.Room)
                {
                    SetCalendarSettings(runSpace, id);
                }

                ret = string.Format("{0}\\{1}", GetNETBIOSDomainName(), accountName);
                ExchangeLog.LogEnd("CreateMailEnableUserInternal");
                return ret;
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreateMailEnableUserInternal", ex);
                RollbackTransaction(transaction);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }
        }

        /*
		private string CreateMailboxInternal(string organizationId, string organizationDistinguishedName,
			string mailboxDatabase, string securityGroup, string offlineAddressBook, ExchangeAccountType accountType,
			 string displayName,
			string accountName, string name, string domain, string password, bool enablePOP, bool enableIMAP,
			bool enableOWA, bool enableMAPI, bool enableActiveSync,
			int issueWarningKB, int prohibitSendKB, int prohibitSendReceiveKB, int keepDeletedItemsDays)
		{
			ExchangeLog.LogStart("CreateMailboxInternal");
			ExchangeLog.DebugInfo("Organization Id: {0}", organizationId);
			ExchangeLog.DebugInfo("Name: {0}", name);
			ExchangeLog.DebugInfo("Domain: {0}", domain);

			string ret = null;
			ExchangeTransaction transaction = StartTransaction();
			Runspace runSpace = null;

            Version exchangeVersion = GetExchangeVersion();

			try
			{
				runSpace = OpenRunspace();


				string upn = string.Format("{0}@{1}", name, domain);
				SecureString securePassword = SecurityUtils.ConvertToSecureString(password);
				string ouName = ConvertADPathToCanonicalName(organizationDistinguishedName);

				//create mailbox
				Command cmd = new Command("New-Mailbox");
				cmd.Parameters.Add("Name", accountName);
				cmd.Parameters.Add("OrganizationalUnit", ouName);
				cmd.Parameters.Add("UserPrincipalName", upn);
				cmd.Parameters.Add("SamAccountName", accountName);
				cmd.Parameters.Add("Alias", accountName);
				cmd.Parameters.Add("DisplayName", displayName);
				cmd.Parameters.Add("Password", securePassword);
				cmd.Parameters.Add("ResetPasswordOnNextLogon", false);
                if (!(mailboxDatabase == "*" && exchangeVersion >= new Version(14, 0)))
                    cmd.Parameters.Add("Database", mailboxDatabase);
				if (accountType == ExchangeAccountType.Equipment)
					cmd.Parameters.Add("Equipment");
				else if (accountType == ExchangeAccountType.Room)
					cmd.Parameters.Add("Room");

				Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
				string id = GetResultObjectDN(result);

				transaction.RegisterNewMailbox(id);

				string windowsEmailAddress = ObjToString(GetPSObjectProperty(result[0], "WindowsEmailAddress"));
                string adpstring = GetAddressPolicyName(organizationId);

				//update mailbox
				cmd = new Command("Set-Mailbox");
				cmd.Parameters.Add("Identity", id);
				cmd.Parameters.Add("OfflineAddressBook", offlineAddressBook);
				cmd.Parameters.Add("EmailAddressPolicyEnabled", false);
				cmd.Parameters.Add("CustomAttribute1", organizationId);
				cmd.Parameters.Add("CustomAttribute3", windowsEmailAddress);
				cmd.Parameters.Add("PrimarySmtpAddress", upn);
				cmd.Parameters.Add("WindowsEmailAddress", upn);
                
                bool enableSP2abp = false;
                if (ConfigurationManager.AppSettings[CONFIG_ENABLESP2ABP] != null)
                    enableSP2abp = Boolean.Parse(ConfigurationManager.AppSettings[CONFIG_ENABLESP2ABP]);
                if (enableSP2abp && (exchangeVersion >= new Version(14, 2)))
                    cmd.Parameters.Add("AddressBookPolicy", adpstring);

				cmd.Parameters.Add("UseDatabaseQuotaDefaults", new bool?(false));
				cmd.Parameters.Add("UseDatabaseRetentionDefaults", false);
				cmd.Parameters.Add("IssueWarningQuota", ConvertKBToUnlimited(issueWarningKB));
				cmd.Parameters.Add("ProhibitSendQuota", ConvertKBToUnlimited(prohibitSendKB));
				cmd.Parameters.Add("ProhibitSendReceiveQuota", ConvertKBToUnlimited(prohibitSendReceiveKB));
				cmd.Parameters.Add("RetainDeletedItemsFor", ConvertDaysToEnhancedTimeSpan(keepDeletedItemsDays));
				ExecuteShellCommand(runSpace, cmd);

				//update AD object
				string globalAddressListName = GetGlobalAddressListName(organizationId);
				string globalAddressListDN = GetGlobalAddressListDN(runSpace, globalAddressListName);
				string path = AddADPrefix(id);
				DirectoryEntry mailbox = GetADObject(path);
				// check if msExchQueryBaseDN must be cleared for Exchange 2010 SP1
				bool clearQueryBaseDN = false;
				if (ConfigurationManager.AppSettings[CONFIG_CLEAR_QUERYBASEDN] != null)
					clearQueryBaseDN = Boolean.Parse(ConfigurationManager.AppSettings[CONFIG_CLEAR_QUERYBASEDN]);

				if (!(clearQueryBaseDN && (exchangeVersion >= new Version(14, 1))))
					SetADObjectPropertyValue(mailbox, "msExchQueryBaseDN", globalAddressListDN);

				//SetADObjectPropertyValue(mailbox, "msExchUseOAB", offlineAddressBook);
				mailbox.CommitChanges();
				mailbox.Close();

				//Client Access
				cmd = new Command("Set-CASMailbox");
				cmd.Parameters.Add("Identity", id);
				cmd.Parameters.Add("ActiveSyncEnabled", enableActiveSync);
				if (enableActiveSync)
				{
					cmd.Parameters.Add("ActiveSyncMailboxPolicy", organizationId);
				}
				cmd.Parameters.Add("OWAEnabled", enableOWA);
				cmd.Parameters.Add("MAPIEnabled", enableMAPI);
				cmd.Parameters.Add("PopEnabled", enablePOP);
				cmd.Parameters.Add("ImapEnabled", enableIMAP);
				ExecuteShellCommand(runSpace, cmd);

				//add to the security group
				cmd = new Command("Add-DistributionGroupMember");
				cmd.Parameters.Add("Identity", securityGroup);
				cmd.Parameters.Add("Member", id);
				ExecuteShellCommand(runSpace, cmd);

				//calendar settings
				if (accountType == ExchangeAccountType.Equipment || accountType == ExchangeAccountType.Room)
				{
					SetCalendarSettings(runSpace, id);
				}

				ret = string.Format("{0}\\{1}", GetNETBIOSDomainName(), accountName);
				ExchangeLog.LogEnd("CreateMailboxInternal");
				return ret;
			}
			catch (Exception ex)
			{
				ExchangeLog.LogError("CreateMailboxInternal", ex);
				RollbackTransaction(transaction);
				throw;
			}
			finally
			{
				CloseRunspace(runSpace);
			}
		}
*/
        internal virtual void SetCalendarSettings(Runspace runspace, string id)
        {
            ExchangeLog.LogStart("SetCalendarSettings");
            Command cmd = new Command("Set-MailboxCalendarSettings");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("AutomateProcessing", CalendarProcessingFlags.AutoAccept);
            ExecuteShellCommand(runspace, cmd);
            ExchangeLog.LogEnd("SetCalendarSettings");
        }

        internal virtual void DeleteMailboxInternal(string accountName)
        {
            ExchangeLog.LogStart("DeleteMailboxInternal");
            ExchangeLog.DebugInfo("Account Name: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                RemoveDevicesInternal(runSpace, accountName);

                RemoveMailbox(runSpace, accountName);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DeleteMailboxInternal");
        }

        internal void RemoveMailbox(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("RemoveMailbox");
            Command cmd = new Command("Remove-Mailbox");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Permanent", false);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("RemoveMailbox");
        }

        private void DisableMailbox(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DisableMailbox");
            Command cmd = new Command("Disable-Mailbox");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DisableMailbox");
        }

        private string GetMailboxCommonName(Runspace runSpace, string accountName)
        {
            ExchangeLog.LogStart("GetMailboxCommonName");
            Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
            PSObject mailbox = result[0];
            string cn = GetPSObjectProperty(mailbox, "Name") as string;
            ExchangeLog.LogEnd("GetMailboxCommonName");
            return cn;
        }

        internal virtual ExchangeAccount GetManager(DirectoryEntry entry)
        {
            ExchangeAccount retUser = null;
            string path = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Manager);
            if (!string.IsNullOrEmpty(path))
            {
                path = ActiveDirectoryUtils.AddADPrefix(path, PrimaryDomainController);
                if (ActiveDirectoryUtils.AdObjectExists(path))
                {
                    DirectoryEntry user = ActiveDirectoryUtils.GetADObject(path);
                    retUser = new ExchangeAccount();
                    retUser.DisplayName = ActiveDirectoryUtils.GetADObjectStringProperty(user, ADAttributes.DisplayName);
                    retUser.AccountName = ActiveDirectoryUtils.GetADObjectStringProperty(user, ADAttributes.Name);
                }
            }

            return retUser;
        }

        internal virtual ExchangeMailbox GetMailboxGeneralSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetMailboxGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeMailbox info = new ExchangeMailbox();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
                PSObject mailbox = result[0];

                string id = GetResultObjectDN(result);
                string path = AddADPrefix(id);
                DirectoryEntry entry = GetADObject(path);


                //ADAccountOptions userFlags = (ADAccountOptions)entry.Properties["userAccountControl"].Value;
                //info.Disabled = ((userFlags & ADAccountOptions.UF_ACCOUNTDISABLE) != 0);
                info.Disabled = (bool)entry.InvokeGet("AccountDisabled");

                info.DisplayName = (string)GetPSObjectProperty(mailbox, "DisplayName");
                info.HideFromAddressBook = (bool)GetPSObjectProperty(mailbox, "HiddenFromAddressListsEnabled");

                Command cmd = new Command("Get-User");
                cmd.Parameters.Add("Identity", accountName);
                result = ExecuteShellCommand(runSpace, cmd);
                PSObject user = result[0];

                info.FirstName = (string)GetPSObjectProperty(user, "FirstName");
                info.Initials = (string)GetPSObjectProperty(user, "Initials");
                info.LastName = (string)GetPSObjectProperty(user, "LastName");

                info.Address = (string)GetPSObjectProperty(user, "StreetAddress");
                info.City = (string)GetPSObjectProperty(user, "City");
                info.State = (string)GetPSObjectProperty(user, "StateOrProvince");
                info.Zip = (string)GetPSObjectProperty(user, "PostalCode");
                info.Country = CountryInfoToString((CountryInfo)GetPSObjectProperty(user, "CountryOrRegion"));
                info.JobTitle = (string)GetPSObjectProperty(user, "Title");
                info.Company = (string)GetPSObjectProperty(user, "Company");
                info.Department = (string)GetPSObjectProperty(user, "Department");
                info.Office = (string)GetPSObjectProperty(user, "Office");


                info.ManagerAccount = GetManager(entry); //GetExchangeAccount(runSpace, ObjToString(GetPSObjectProperty(user, "Manager")));
                info.BusinessPhone = (string)GetPSObjectProperty(user, "Phone");
                info.Fax = (string)GetPSObjectProperty(user, "Fax");
                info.HomePhone = (string)GetPSObjectProperty(user, "HomePhone");
                info.MobilePhone = (string)GetPSObjectProperty(user, "MobilePhone");
                info.Pager = (string)GetPSObjectProperty(user, "Pager");
                info.WebPage = (string)GetPSObjectProperty(user, "WebPage");
                info.Notes = (string)GetPSObjectProperty(user, "Notes");

            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMailboxGeneralSettingsInternal");
            return info;
        }


        private void SetMailboxGeneralSettingsInternal(string accountName, bool hideFromAddressBook, bool disabled)
        {
            ExchangeLog.LogStart("SetMailboxGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
                PSObject mailbox = result[0];

                string id = GetResultObjectDN(result);
                string path = AddADPrefix(id);
                DirectoryEntry entry = GetADObject(path);
                entry.InvokeSet("AccountDisabled", disabled);
                entry.CommitChanges();

                Command cmd = new Command("Set-Mailbox");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("HiddenFromAddressListsEnabled", hideFromAddressBook);
                cmd.Parameters.Add("CustomAttribute2", (disabled ? "disabled" : null));
                ExecuteShellCommand(runSpace, cmd);

            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetMailboxGeneralSettingsInternal");
        }

        private void ChangeMailboxState(string id, bool enabled)
        {
            string path = AddADPrefix(id);
            DirectoryEntry entry = GetADObject(path);
            entry.InvokeSet("AccountDisabled", !enabled);
            entry.CommitChanges();
        }

        private ExchangeMailbox GetMailboxMailFlowSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetMailboxMailFlowSettings");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeMailbox info = new ExchangeMailbox();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
                PSObject mailbox = result[0];

                string forwardingAddress = ObjToString(GetPSObjectProperty(mailbox, "ForwardingAddress"));
                if (string.IsNullOrEmpty(forwardingAddress))
                {
                    info.EnableForwarding = false;
                    info.ForwardingAccount = null;
                    info.DoNotDeleteOnForward = false;
                }
                else
                {
                    info.EnableForwarding = true;
                    info.ForwardingAccount = GetExchangeAccount(runSpace, forwardingAddress);
                    info.DoNotDeleteOnForward = (bool)GetPSObjectProperty(mailbox, "DeliverToMailboxAndForward");
                }

                info.SendOnBehalfAccounts = GetSendOnBehalfAccounts(runSpace, mailbox);
                info.AcceptAccounts = GetAcceptedAccounts(runSpace, mailbox);
                info.RejectAccounts = GetRejectedAccounts(runSpace, mailbox);
                info.MaxRecipients =
                    ConvertUnlimitedToInt32((Unlimited<int>)GetPSObjectProperty(mailbox, "RecipientLimits"));
                info.MaxSendMessageSizeKB =
                    ConvertUnlimitedToKB((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "MaxSendSize"));
                info.MaxReceiveMessageSizeKB =
                    ConvertUnlimitedToKB((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "MaxReceiveSize"));
                info.RequireSenderAuthentication = (bool)GetPSObjectProperty(mailbox, "RequireSenderAuthenticationEnabled");
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMailboxMailFlowSettings");
            return info;
        }

        private void SetMailboxMailFlowSettingsInternal(string accountName, bool enableForwarding,
            string forwardingAccountName, bool forwardToBoth, string[] sendOnBehalfAccounts,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            ExchangeLog.LogStart("SetMailboxMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Set-Mailbox");
                cmd.Parameters.Add("Identity", accountName);

                if (enableForwarding)
                {
                    cmd.Parameters.Add("ForwardingAddress", forwardingAccountName);
                    cmd.Parameters.Add("DeliverToMailboxAndForward", forwardToBoth);
                }
                else
                {
                    cmd.Parameters.Add("ForwardingAddress", null);
                    cmd.Parameters.Add("DeliverToMailboxAndForward", false);
                }

                cmd.Parameters.Add("GrantSendOnBehalfTo", SetSendOnBehalfAccounts(runSpace, sendOnBehalfAccounts));

                MultiValuedProperty<ADObjectId> ids = null;
                MultiValuedProperty<ADObjectId> dlIds = null;

                SetAccountIds(runSpace, acceptAccounts, out ids, out dlIds);
                cmd.Parameters.Add("AcceptMessagesOnlyFrom", ids);
                cmd.Parameters.Add("AcceptMessagesOnlyFromDLMembers", dlIds);

                SetAccountIds(runSpace, rejectAccounts, out ids, out dlIds);
                cmd.Parameters.Add("RejectMessagesFrom", ids);
                cmd.Parameters.Add("RejectMessagesFromDLMembers", dlIds);

                cmd.Parameters.Add("RequireSenderAuthenticationEnabled", requireSenderAuthentication);

                ExecuteShellCommand(runSpace, cmd);

            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetMailboxMailFlowSettingsInternal");
        }

        internal virtual ExchangeMailbox GetMailboxAdvancedSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetMailboxAdvancedSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeMailbox info = new ExchangeMailbox();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
                PSObject mailbox = result[0];

                info.IssueWarningKB =
                    ConvertUnlimitedToKB((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "IssueWarningQuota"));
                info.ProhibitSendKB =
                    ConvertUnlimitedToKB((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "ProhibitSendQuota"));
                info.ProhibitSendReceiveKB =
                    ConvertUnlimitedToKB((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "ProhibitSendReceiveQuota"));
                info.KeepDeletedItemsDays =
                    ConvertEnhancedTimeSpanToDays((EnhancedTimeSpan)GetPSObjectProperty(mailbox, "RetainDeletedItemsFor"));

                //Client Access
                Command cmd = new Command("Get-CASMailbox");
                cmd.Parameters.Add("Identity", accountName);
                result = ExecuteShellCommand(runSpace, cmd);
                mailbox = result[0];

                info.EnableActiveSync = (bool)GetPSObjectProperty(mailbox, "ActiveSyncEnabled");
                info.EnableOWA = (bool)GetPSObjectProperty(mailbox, "OWAEnabled");
                info.EnableMAPI = (bool)GetPSObjectProperty(mailbox, "MAPIEnabled");
                info.EnablePOP = (bool)GetPSObjectProperty(mailbox, "PopEnabled");
                info.EnableIMAP = (bool)GetPSObjectProperty(mailbox, "ImapEnabled");

                //Statistics
                cmd = new Command("Get-MailboxStatistics");
                cmd.Parameters.Add("Identity", accountName);
                result = ExecuteShellCommand(runSpace, cmd);
                if (result.Count > 0)
                {
                    PSObject statistics = result[0];
                    Unlimited<ByteQuantifiedSize> totalItemSize =
                        (Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(statistics, "TotalItemSize");
                    info.TotalSizeMB = ConvertUnlimitedToMB(totalItemSize);
                    uint? itemCount = (uint?)GetPSObjectProperty(statistics, "ItemCount");
                    info.TotalItems = ConvertNullableToInt32(itemCount);
                    DateTime? lastLogoffTime = (DateTime?)GetPSObjectProperty(statistics, "LastLogoffTime"); ;
                    DateTime? lastLogonTime = (DateTime?)GetPSObjectProperty(statistics, "LastLogonTime"); ;
                    info.LastLogoff = ConvertNullableToDateTime(lastLogoffTime);
                    info.LastLogon = ConvertNullableToDateTime(lastLogonTime);
                }
                else
                {
                    info.TotalSizeMB = 0;
                    info.TotalItems = 0;
                    info.LastLogoff = DateTime.MinValue;
                    info.LastLogon = DateTime.MinValue;
                }

                //domain
                info.Domain = GetNETBIOSDomainName();
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMailboxAdvancedSettingsInternal");
            return info;
        }

        internal virtual void SetMailboxAdvancedSettingsInternal(string organizationId, string accountName, bool enablePOP, bool enableIMAP,
            bool enableOWA, bool enableMAPI, bool enableActiveSync, long issueWarningKB, long prohibitSendKB,
            long prohibitSendReceiveKB, int keepDeletedItemsDays, int maxRecipients, int maxSendMessageSizeKB,
            int maxReceiveMessageSizeKB, bool enabledLitigationHold, long recoverabelItemsSpace, long recoverabelItemsWarning,
            string litigationHoldUrl, string litigationHoldMsg)
        {
            ExchangeLog.LogStart("SetMailboxAdvancedSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Set-Mailbox");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("IssueWarningQuota", ConvertKBToUnlimited(issueWarningKB));
                cmd.Parameters.Add("ProhibitSendQuota", ConvertKBToUnlimited(prohibitSendKB));
                cmd.Parameters.Add("ProhibitSendReceiveQuota", ConvertKBToUnlimited(prohibitSendReceiveKB));
                cmd.Parameters.Add("RetainDeletedItemsFor", ConvertDaysToEnhancedTimeSpan(keepDeletedItemsDays));
                cmd.Parameters.Add("RecipientLimits", ConvertInt32ToUnlimited(maxRecipients));
                cmd.Parameters.Add("MaxSendSize", ConvertKBToUnlimited(maxSendMessageSizeKB));
                cmd.Parameters.Add("MaxReceiveSize", ConvertKBToUnlimited(maxReceiveMessageSizeKB));

                ExecuteShellCommand(runSpace, cmd);

                //Client Access
                cmd = new Command("Set-CASMailbox");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("ActiveSyncEnabled", enableActiveSync);
                if (enableActiveSync)
                {
                    cmd.Parameters.Add("ActiveSyncMailboxPolicy", organizationId);
                }
                cmd.Parameters.Add("OWAEnabled", enableOWA);
                cmd.Parameters.Add("MAPIEnabled", enableMAPI);
                cmd.Parameters.Add("PopEnabled", enablePOP);
                cmd.Parameters.Add("ImapEnabled", enableIMAP);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetMailboxAdvancedSettingsInternal");
        }

        private ExchangeEmailAddress[] GetMailboxEmailAddressesInternal(string accountName)
        {
            ExchangeLog.LogStart("GetMailboxEmailAddressesInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            List<ExchangeEmailAddress> list = new List<ExchangeEmailAddress>();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
                PSObject mailbox = result[0];

                string primaryEmail = null;
                string windowsEmail = null;

                SmtpAddress smtpAddress = (SmtpAddress)GetPSObjectProperty(mailbox, "PrimarySmtpAddress");
                if (smtpAddress != null)
                    primaryEmail = smtpAddress.ToString();

                //SmtpAddress winAddress = (SmtpAddress)GetPSObjectProperty(mailbox, "WindowsEmailAddress");
                windowsEmail = ObjToString(GetPSObjectProperty(mailbox, "CustomAttribute3"));

                ProxyAddressCollection emails = (ProxyAddressCollection)GetPSObjectProperty(mailbox, "EmailAddresses");
                foreach (ProxyAddress email in emails)
                {
                    //skip windows email
                    if (string.Equals(email.AddressString, windowsEmail, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ExchangeEmailAddress item = new ExchangeEmailAddress();
                    item.Email = email.AddressString;
                    item.Primary = string.Equals(item.Email, primaryEmail, StringComparison.OrdinalIgnoreCase);
                    list.Add(item);
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMailboxEmailAddressesInternal");
            return list.ToArray();
        }

        private void SetMailboxEmailAddressesInternal(string accountName, string[] emailAddresses)
        {
            ExchangeLog.LogStart("SetMailboxEmailAddressesInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, accountName);
                PSObject mailbox = result[0];

                //SmtpAddress winAddress = (SmtpAddress)GetPSObjectProperty(mailbox, "WindowsEmailAddress");
                //if (winAddress != null)
                //	windowsEmail = winAddress.ToString();

                string windowsEmail = ObjToString(GetPSObjectProperty(mailbox, "CustomAttribute3"));

                ProxyAddressCollection emails = new ProxyAddressCollection();
                ProxyAddress proxy = null;
                string upn = null;
                if (emailAddresses != null)
                {
                    foreach (string email in emailAddresses)
                    {
                        proxy = ProxyAddress.Parse(email);
                        emails.Add(proxy);
                        if (proxy.IsPrimaryAddress)
                        {
                            upn = proxy.AddressString;
                        }
                    }
                }
                //add system windows email
                if (!string.IsNullOrEmpty(windowsEmail))
                {
                    emails.Add(ProxyAddress.Parse(windowsEmail));
                }

                Command cmd = new Command("Set-Mailbox");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("EmailAddresses", emails);
                if (!string.IsNullOrEmpty(upn))
                {
                    cmd.Parameters.Add("UserPrincipalName", upn);
                    cmd.Parameters.Add("WindowsEmailAddress", upn);
                }
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetMailboxEmailAddressesInternal");
        }

        private void SetMailboxPrimaryEmailAddressInternal(string accountName, string emailAddress)
        {
            ExchangeLog.LogStart("SetMailboxPrimaryEmailAddressInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                SmtpAddress primaryEmail = new SmtpAddress(emailAddress);
                Command cmd = new Command("Set-Mailbox");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("PrimarySmtpAddress", primaryEmail);
				//cmd.Parameters.Add("UserPrincipalName", primaryEmail);
				//cmd.Parameters.Add("WindowsEmailAddress", primaryEmail);

                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetMailboxPrimaryEmailAddressInternal");
        }


        private ExchangeAccount[] GetSendOnBehalfAccounts(Runspace runSpace, PSObject exchangeObject)
        {
            List<ExchangeAccount> list = new List<ExchangeAccount>();

            IList<ADObjectId> ids =
                (IList<ADObjectId>)GetPSObjectProperty(exchangeObject, "GrantSendOnBehalfTo");

            foreach (ADObjectId id in ids)
            {
                ExchangeAccount account = GetExchangeAccount(runSpace, id.ToString());
                if (account != null)
                    list.Add(account);
            }
            return list.ToArray();
        }

        internal MultiValuedProperty<ADObjectId> SetSendOnBehalfAccounts(Runspace runspace, string[] accounts)
        {
            if (accounts == null || accounts.Length == 0)
                return MultiValuedProperty<ADObjectId>.Empty;
            else
            {
                MultiValuedProperty<ADObjectId> ids = new MultiValuedProperty<ADObjectId>();
                string dn = null;
                foreach (string account in accounts)
                {
                    dn = GetRecipientDistinguishedName(runspace, account);
                    ids.Add(new ADObjectId(dn));
                }
                return ids;
            }
        }

        private ExchangeAccount[] GetAcceptedAccounts(Runspace runSpace, PSObject mailbox)
        {
            List<ExchangeAccount> list = new List<ExchangeAccount>();
            ExchangeAccount account = null;
            IList<ADObjectId> ids =
                (IList<ADObjectId>)GetPSObjectProperty(mailbox, "AcceptMessagesOnlyFrom");
            foreach (ADObjectId id in ids)
            {
                account = GetExchangeAccount(runSpace, id.ToString());
                if (account != null)
                    list.Add(account);
            }
            ids = (IList<ADObjectId>)GetPSObjectProperty(mailbox, "AcceptMessagesOnlyFromDLMembers");
            foreach (ADObjectId id in ids)
            {
                account = GetExchangeAccount(runSpace, id.ToString());
                if (account != null)
                    list.Add(account);
            }

            return list.ToArray();
        }

        private void SetAccountIds(Runspace runspace, string[] accounts,
            out MultiValuedProperty<ADObjectId> ids, out MultiValuedProperty<ADObjectId> dlIds)
        {
            ids = MultiValuedProperty<ADObjectId>.Empty;
            dlIds = MultiValuedProperty<ADObjectId>.Empty;

            if (accounts == null || accounts.Length == 0)
                return;

            ids = new MultiValuedProperty<ADObjectId>();
            dlIds = new MultiValuedProperty<ADObjectId>();

            string dn = null;
            string type = null;
            foreach (string account in accounts)
            {
                type = GetRecipientType(runspace, account, out dn);
                if (type == "MailUniversalDistributionGroup")
                    dlIds.Add(new ADObjectId(dn));
                else
                    ids.Add(new ADObjectId(dn));
            }
            if (ids.Count == 0)
                ids = MultiValuedProperty<ADObjectId>.Empty;
            if (dlIds.Count == 0)
                dlIds = MultiValuedProperty<ADObjectId>.Empty;
        }


        private ExchangeAccount[] GetRejectedAccounts(Runspace runSpace, PSObject mailbox)
        {
            List<ExchangeAccount> list = new List<ExchangeAccount>();
            ExchangeAccount account = null;
            IList<ADObjectId> ids =
                (IList<ADObjectId>)GetPSObjectProperty(mailbox, "RejectMessagesFrom");
            foreach (ADObjectId id in ids)
            {
                account = GetExchangeAccount(runSpace, id.ToString());
                if (account != null)
                    list.Add(account);
            }
            ids = (IList<ADObjectId>)GetPSObjectProperty(mailbox, "RejectMessagesFromDLMembers");
            foreach (ADObjectId id in ids)
            {
                account = GetExchangeAccount(runSpace, id.ToString());
                if (account != null)
                    list.Add(account);
            }
            return list.ToArray();
        }

        internal ExchangeAccount GetExchangeAccount(Runspace runSpace, string id)
        {
            return GetOrganizationAccount(runSpace, null, id);
        }

        internal ExchangeAccount GetOrganizationAccount(Runspace runSpace, string organizationId, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            ExchangeAccount ret = null;
            Command cmd = new Command("Get-Recipient");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                //check for organization
                if (!string.IsNullOrEmpty(organizationId))
                {
                    string attr1 = (string)GetPSObjectProperty(result[0], "CustomAttribute1");
                    if (!string.Equals(organizationId, attr1, StringComparison.InvariantCultureIgnoreCase))
                        return ret;
                }
                ret = new ExchangeAccount();
                ret.AccountName = (string)GetPSObjectProperty(result[0], "Alias");
                ret.DisplayName = (string)GetPSObjectProperty(result[0], "DisplayName");
                ret.PrimaryEmailAddress = ObjToString(GetPSObjectProperty(result[0], "PrimarySmtpAddress"));
                ret.AccountType = ParseAccountType(ObjToString(GetPSObjectProperty(result[0], "RecipientType")));
                if (ret.AccountType == ExchangeAccountType.Contact)
                {
                    string email = ObjToString(GetPSObjectProperty(result[0], "ExternalEmailAddress"));
                    if (!string.IsNullOrEmpty(email) && email.StartsWith("SMTP:"))
                        ret.PrimaryEmailAddress = email.Substring(5);
                }
            }
            return ret;
        }



        private ExchangeAccountType ParseAccountType(string type)
        {
            ExchangeAccountType ret = ExchangeAccountType.Mailbox;
            switch (type)
            {
                case "UserMailbox":
                    ret = ExchangeAccountType.Mailbox;
                    break;
                case "MailContact":
                    ret = ExchangeAccountType.Contact;
                    break;
                case "MailUniversalDistributionGroup":
                    ret = ExchangeAccountType.DistributionList;
                    break;
                case "PublicFolder":
                    ret = ExchangeAccountType.PublicFolder;
                    break;
            }
            return ret;
        }

        private string GetRecipientDistinguishedName(Runspace runSpace, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            string dn = null;
            Command cmd = new Command("Get-Recipient");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                dn = (string)GetPSObjectProperty(result[0], "DistinguishedName");
            }
            return dn;
        }

        private string GetRecipientType(Runspace runSpace, string id, out string name)
        {
            name = null;
            if (string.IsNullOrEmpty(id))
                return null;

            string type = null;
            Command cmd = new Command("Get-Recipient");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                name = (string)GetPSObjectProperty(result[0], "DistinguishedName");
                type = GetPSObjectProperty(result[0], "RecipientType").ToString();
            }
            return type;
        }

        virtual internal ExchangeMailboxStatistics GetMailboxStatisticsInternal(string id)
        {
            ExchangeLog.LogStart("GetMailboxStatisticsInternal");
            ExchangeLog.DebugInfo("Account: {0}", id);

            ExchangeMailboxStatistics info = new ExchangeMailboxStatistics();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetMailboxObject(runSpace, id);
                PSObject mailbox = result[0];

                string dn = GetResultObjectDN(result);
                string path = AddADPrefix(dn);
                DirectoryEntry entry = GetADObject(path);
                info.Enabled = !(bool)entry.InvokeGet("AccountDisabled");

                info.DisplayName = (string)GetPSObjectProperty(mailbox, "DisplayName");
                SmtpAddress smtpAddress = (SmtpAddress)GetPSObjectProperty(mailbox, "PrimarySmtpAddress");
                if (smtpAddress != null)
                    info.PrimaryEmailAddress = smtpAddress.ToString();

                info.MaxSize = ConvertUnlimitedToBytes((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(mailbox, "ProhibitSendReceiveQuota"));
                DateTime? whenCreated = (DateTime?)GetPSObjectProperty(mailbox, "WhenCreated");
                info.AccountCreated = ConvertNullableToDateTime(whenCreated);
                //Client Access
                Command cmd = new Command("Get-CASMailbox");
                cmd.Parameters.Add("Identity", id);
                result = ExecuteShellCommand(runSpace, cmd);
                mailbox = result[0];

                info.ActiveSyncEnabled = (bool)GetPSObjectProperty(mailbox, "ActiveSyncEnabled");
                info.OWAEnabled = (bool)GetPSObjectProperty(mailbox, "OWAEnabled");
                info.MAPIEnabled = (bool)GetPSObjectProperty(mailbox, "MAPIEnabled");
                info.POPEnabled = (bool)GetPSObjectProperty(mailbox, "PopEnabled");
                info.IMAPEnabled = (bool)GetPSObjectProperty(mailbox, "ImapEnabled");

                //Statistics
                cmd = new Command("Get-MailboxStatistics");
                cmd.Parameters.Add("Identity", id);
                result = ExecuteShellCommand(runSpace, cmd);
                if (result.Count > 0)
                {
                    PSObject statistics = result[0];
                    Unlimited<ByteQuantifiedSize> totalItemSize =
                        (Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(statistics, "TotalItemSize");
                    info.TotalSize = ConvertUnlimitedToBytes(totalItemSize);
                    uint? itemCount = (uint?)GetPSObjectProperty(statistics, "ItemCount");
                    info.TotalItems = ConvertNullableToInt32(itemCount);
                    DateTime? lastLogoffTime = (DateTime?)GetPSObjectProperty(statistics, "LastLogoffTime");
                    DateTime? lastLogonTime = (DateTime?)GetPSObjectProperty(statistics, "LastLogonTime");
                    info.LastLogoff = ConvertNullableToDateTime(lastLogoffTime);
                    info.LastLogon = ConvertNullableToDateTime(lastLogonTime);
                }
                else
                {
                    info.TotalSize = 0;
                    info.TotalItems = 0;
                    info.LastLogoff = DateTime.MinValue;
                    info.LastLogon = DateTime.MinValue;
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMailboxStatisticsInternal");
            return info;
        }

        virtual internal Collection<PSObject> GetMailboxObject(Runspace runSpace, string id)
        {
            Command cmd = new Command("Get-Mailbox");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            return result;
        }

        #endregion

        #region Contacts
        private void CreateContactInternal(
            string organizationId,
            string organizationDistinguishedName,
            string contactDisplayName,
            string contactAccountName,
            string contactEmail,
            string defaultOrganizationDomain)
        {
            ExchangeLog.LogStart("CreateContactInternal");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationId);
            ExchangeLog.DebugInfo("Name: {0}", contactDisplayName);
            ExchangeLog.DebugInfo("Account: {0}", contactAccountName);
            ExchangeLog.DebugInfo("Email: {0}", contactEmail);

            ExchangeTransaction transaction = StartTransaction();

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                string ouName = ConvertADPathToCanonicalName(organizationDistinguishedName);
                string tempEmail = string.Format("{0}@{1}", Guid.NewGuid().ToString("N"), defaultOrganizationDomain);
                //create contact
                Command cmd = new Command("New-MailContact");
                cmd.Parameters.Add("Name", contactAccountName);
                cmd.Parameters.Add("DisplayName", contactDisplayName);
                cmd.Parameters.Add("OrganizationalUnit", ouName);
                cmd.Parameters.Add("Alias", contactAccountName);
                cmd.Parameters.Add("ExternalEmailAddress", tempEmail);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                string id = GetResultObjectDN(result);

                transaction.RegisterNewContact(id);

                //update contact
                cmd = new Command("Set-MailContact");
                cmd.Parameters.Add("Identity", contactAccountName);
                cmd.Parameters.Add("EmailAddressPolicyEnabled", false);
                cmd.Parameters.Add("CustomAttribute1", organizationId);
                cmd.Parameters.Add("WindowsEmailAddress", tempEmail);
                ExecuteShellCommand(runSpace, cmd);

                SetContactEmail(id, contactEmail);

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreateContactInternal", ex);
                RollbackTransaction(transaction);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("CreateContactInternal");
        }

        private void DeleteContactInternal(string accountName)
        {
            ExchangeLog.LogStart("DeleteContactInternal");
            ExchangeLog.DebugInfo("Account Name: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                RemoveContact(runSpace, accountName);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DeleteContactInternal");
        }

        private void RemoveContact(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("RemoveContact");
            Command cmd = new Command("Remove-MailContact");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("RemoveContact");
        }

        private ExchangeContact GetContactGeneralSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetContactGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeContact info = new ExchangeContact();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-MailContact");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject contact = result[0];
                string id = GetResultObjectDN(result);

                info.DisplayName = (string)GetPSObjectProperty(contact, "DisplayName");
                info.HideFromAddressBook = (bool)GetPSObjectProperty(contact, "HiddenFromAddressListsEnabled");
                info.EmailAddress = GetContactEmail(id);
                info.UseMapiRichTextFormat = (int)GetPSObjectProperty(contact, "UseMapiRichTextFormat");

                cmd = new Command("Get-Contact");
                cmd.Parameters.Add("Identity", accountName);
                result = ExecuteShellCommand(runSpace, cmd);
                PSObject user = result[0];

                info.FirstName = (string)GetPSObjectProperty(user, "FirstName");
                info.Initials = (string)GetPSObjectProperty(user, "Initials");
                info.LastName = (string)GetPSObjectProperty(user, "LastName");

                info.Address = (string)GetPSObjectProperty(user, "StreetAddress");
                info.City = (string)GetPSObjectProperty(user, "City");
                info.State = (string)GetPSObjectProperty(user, "StateOrProvince");
                info.Zip = (string)GetPSObjectProperty(user, "PostalCode");
                info.Country = CountryInfoToString((CountryInfo)GetPSObjectProperty(user, "CountryOrRegion"));
                info.JobTitle = (string)GetPSObjectProperty(user, "Title");
                info.Company = (string)GetPSObjectProperty(user, "Company");
                info.Department = (string)GetPSObjectProperty(user, "Department");
                info.Office = (string)GetPSObjectProperty(user, "Office");
                info.ManagerAccount = GetExchangeAccount(runSpace, ObjToString(GetPSObjectProperty(user, "Manager")));
                info.BusinessPhone = (string)GetPSObjectProperty(user, "Phone");
                info.Fax = (string)GetPSObjectProperty(user, "Fax");
                info.HomePhone = (string)GetPSObjectProperty(user, "HomePhone");
                info.MobilePhone = (string)GetPSObjectProperty(user, "MobilePhone");
                info.Pager = (string)GetPSObjectProperty(user, "Pager");
                info.WebPage = (string)GetPSObjectProperty(user, "WebPage");
                info.Notes = (string)GetPSObjectProperty(user, "Notes");

                info.SAMAccountName = string.Format("{0}\\{1}", GetNETBIOSDomainName(), (string)GetPSObjectProperty(user, "Name"));

            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetContactGeneralSettingsInternal");
            return info;
        }

        private void SetContactEmail(string id, string email)
        {
            string cn = ActiveDirectoryUtils.AddADPrefix(id, PrimaryDomainController);
            DirectoryEntry de = ActiveDirectoryUtils.GetADObject(cn);
            ActiveDirectoryUtils.SetADObjectPropertyValue(de, "targetAddress", "SMTP:" + email);
            //ActiveDirectoryUtils.SetADObjectPropertyValue(de, "mail", email);
            de.CommitChanges();

        }

        private string GetContactEmail(string id)
        {
            string cn = ActiveDirectoryUtils.AddADPrefix(id, PrimaryDomainController);
            DirectoryEntry de = ActiveDirectoryUtils.GetADObject(cn);
            string email = ActiveDirectoryUtils.GetADObjectStringProperty(de, "targetAddress");
            if (email != null && email.ToLower().StartsWith("smtp:"))
                email = email.Substring(5);
            return email;
        }

        private void SetContactGeneralSettingsInternal(string accountName, string displayName, string email,
            bool hideFromAddressBook, string firstName, string initials, string lastName, string address,
            string city, string state, string zip, string country, string jobTitle, string company,
            string department, string office, string managerAccountName, string businessPhone, string fax,
            string homePhone, string mobilePhone, string pager, string webPage, string notes, int useMapiRichTextFormat, string defaultDomain)
        {
            ExchangeLog.LogStart("SetContactGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-MailContact");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

                string id = GetResultObjectDN(result);
                string tempEmail = SmtpAddressToString((SmtpAddress)GetPSObjectProperty(result[0], "PrimarySmtpAddress"));
                string[] parts = tempEmail.Split('@');
                if (parts != null && parts.Length > 0)
                    tempEmail = parts[0] + '@' + defaultDomain;

                cmd = new Command("Set-MailContact");
                cmd.Parameters.Add("Identity", accountName);

                cmd.Parameters.Add("DisplayName", displayName);
                cmd.Parameters.Add("HiddenFromAddressListsEnabled", hideFromAddressBook);
                cmd.Parameters.Add("ExternalEmailAddress", tempEmail);
                cmd.Parameters.Add("UseMapiRichTextFormat", (UseMapiRichTextFormat)useMapiRichTextFormat);
                cmd.Parameters.Add("WindowsEmailAddress", tempEmail);
                ExecuteShellCommand(runSpace, cmd);


                cmd = new Command("Set-Contact");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("FirstName", firstName);
                cmd.Parameters.Add("Initials", initials);
                cmd.Parameters.Add("LastName", lastName);
                cmd.Parameters.Add("StreetAddress", address);
                cmd.Parameters.Add("City", city);
                cmd.Parameters.Add("StateOrProvince", state);
                cmd.Parameters.Add("PostalCode", zip);
                cmd.Parameters.Add("CountryOrRegion", ParseCountryInfo(country));
                cmd.Parameters.Add("Title", jobTitle);
                cmd.Parameters.Add("Company", company);
                cmd.Parameters.Add("Department", department);
                cmd.Parameters.Add("Office", office);
                cmd.Parameters.Add("Manager", managerAccountName);
                cmd.Parameters.Add("Phone", businessPhone);
                cmd.Parameters.Add("Fax", fax);
                cmd.Parameters.Add("HomePhone", homePhone);
                cmd.Parameters.Add("MobilePhone", mobilePhone);
                cmd.Parameters.Add("Pager", pager);
                cmd.Parameters.Add("WebPage", webPage);
                cmd.Parameters.Add("Notes", notes);

                ExecuteShellCommand(runSpace, cmd);

                SetContactEmail(id, email);

            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetContactGeneralSettingsInternal");
        }

        private ExchangeContact GetContactMailFlowSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetContactMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeContact info = new ExchangeContact();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-MailContact");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject contact = result[0];

                info.AcceptAccounts = GetAcceptedAccounts(runSpace, contact);
                info.RejectAccounts = GetRejectedAccounts(runSpace, contact);
                info.RequireSenderAuthentication = (bool)GetPSObjectProperty(contact, "RequireSenderAuthenticationEnabled");
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetContactMailFlowSettingsInternal");
            return info;
        }

        private void SetContactMailFlowSettingsInternal(string accountName, string[] acceptAccounts,
            string[] rejectAccounts, bool requireSenderAuthentication)
        {
            ExchangeLog.LogStart("SetContactMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Set-MailContact");
                cmd.Parameters.Add("Identity", accountName);

                MultiValuedProperty<ADObjectId> ids = null;
                MultiValuedProperty<ADObjectId> dlIds = null;

                SetAccountIds(runSpace, acceptAccounts, out ids, out dlIds);
                cmd.Parameters.Add("AcceptMessagesOnlyFrom", ids);
                cmd.Parameters.Add("AcceptMessagesOnlyFromDLMembers", dlIds);

                SetAccountIds(runSpace, rejectAccounts, out ids, out dlIds);
                cmd.Parameters.Add("RejectMessagesFrom", ids);
                cmd.Parameters.Add("RejectMessagesFromDLMembers", dlIds);
                cmd.Parameters.Add("RequireSenderAuthenticationEnabled", requireSenderAuthentication);

                ExecuteShellCommand(runSpace, cmd);

            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetContactMailFlowSettingsInternal");
        }

        #endregion

        #region Distribution groups
        /// <summary>
        /// Creates Security Distribution Group 
        /// </summary>
        /// <param name="runSpace"></param>
        /// <param name="ouName"></param>
        /// <param name="groupName"></param>
        /// <returns>LDAP path</returns>
        private string CreateSecurityDistributionGroup(Runspace runSpace, string ouName, string groupName)
        {
            ExchangeLog.LogStart("CreateSecurityDistributionGroup");

            Command cmd = new Command("New-DistributionGroup");
            cmd.Parameters.Add("Name", groupName);
            cmd.Parameters.Add("Type", "Security");
            cmd.Parameters.Add("OrganizationalUnit", ouName);
            cmd.Parameters.Add("SamAccountName", groupName);
            cmd.Parameters.Add("Alias", groupName);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = CheckResultObjectDN(result);

            ExchangeLog.LogEnd("CreateSecurityDistributionGroup");
            return id;
        }

        internal string EnableMailSecurityDistributionGroup(Runspace runSpace, string distName, string groupName)
        {
            ExchangeLog.LogStart("EnableMailSecurityDistributionGroup");
            ExchangeLog.DebugInfo("Group Distinguished Name: {0}", distName);
            ExchangeLog.DebugInfo("Group Name: {0}", groupName);

            int attempts = 0;
            string securityGroupId = null;

            while (true)
            {
                try
                {
                    Command cmd = new Command("Enable-DistributionGroup");
                    cmd.Parameters.Add("Identity", distName);
                    cmd.Parameters.Add("Alias", groupName);

                    Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

                    securityGroupId = CheckResultObjectDN(result);
                    ExchangeLog.DebugInfo("Result: {0}", securityGroupId);
                }
                catch (Exception ex)
                {
                    Log.WriteError(ex);
                }

                if (securityGroupId != null)
                    break;

                if (attempts > 3)
                    throw new Exception(
                        string.Format("Could not enable mail Security Distribution Group '{0}' ", groupName));

                attempts++;
                ExchangeLog.LogWarning("Attempt #{0} to enable mail Security Distribution Group failed!", attempts);
                // wait 5 sec
                System.Threading.Thread.Sleep(5000);
            }


            ExchangeLog.LogEnd("EnableMailSecurityDistributionGroup");
            return securityGroupId;
        }

        internal void DisableMailSecurityDistributionGroup(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DisableMailSecurityDistributionGroup");
            ExchangeLog.DebugInfo("Group Id: {0}", id);
            Command cmd = new Command("Disable-DistributionGroup");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DisableMailSecurityDistributionGroup");
        }


        internal void UpdateSecurityDistributionGroup(Runspace runSpace, string id, string groupName, bool isConsumer)
        {
            ExchangeLog.LogStart("UpdateSecurityDistributionGroup");

            Command cmd = new Command("Set-DistributionGroup");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("EmailAddressPolicyEnabled", false);
            cmd.Parameters.Add("CustomAttribute1", groupName);
            cmd.Parameters.Add("HiddenFromAddressListsEnabled", !isConsumer);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("UpdateSecurityDistributionGroup");
        }

        private void CreateDistributionListInternal(
            string organizationId,
            string organizationDistinguishedName,
            string displayName,
            string accountName,
            string name,
            string domain,
            string managedBy,
            string[] addressLists)
        {
            ExchangeLog.LogStart("CreateDistributionListInternal");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationId);
            ExchangeLog.DebugInfo("Name: {0}", name);
            ExchangeLog.DebugInfo("Domain: {0}", domain);

            ExchangeTransaction transaction = StartTransaction();

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                string email = string.Format("{0}@{1}", name, domain);
                string ouName = ConvertADPathToCanonicalName(organizationDistinguishedName);

                Command cmd = new Command("New-DistributionGroup");
                cmd.Parameters.Add("Name", accountName);
                cmd.Parameters.Add("DisplayName", displayName);
                cmd.Parameters.Add("Type", "Security");
                cmd.Parameters.Add("OrganizationalUnit", ouName);
                cmd.Parameters.Add("SamAccountName", accountName);
                cmd.Parameters.Add("Alias", accountName);
                cmd.Parameters.Add("ManagedBy", managedBy);

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                string id = GetResultObjectDN(result);

                transaction.RegisterNewDistributionGroup(id);

                //add manager permissions
                if (!string.IsNullOrEmpty(managedBy))
                {
                    AddADPermission(runSpace, accountName, managedBy, "WriteProperty", null, "Member");
                }

                string windowsEmailAddress = ObjToString(GetPSObjectProperty(result[0], "WindowsEmailAddress"));

                //update
                cmd = new Command("Set-DistributionGroup");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("EmailAddressPolicyEnabled", false);
                cmd.Parameters.Add("CustomAttribute1", organizationId);
                cmd.Parameters.Add("CustomAttribute3", windowsEmailAddress);
                cmd.Parameters.Add("PrimarySmtpAddress", email);
                cmd.Parameters.Add("WindowsEmailAddress", email);
                cmd.Parameters.Add("RequireSenderAuthenticationEnabled", false);
                ExecuteShellCommand(runSpace, cmd);

                //fix showInAddressBook Attribute
                if (addressLists.Length > 0)
                    FixShowInAddressBook(runSpace, email, addressLists, false);

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreateDistributionListInternal", ex);
                RollbackTransaction(transaction);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CreateDistributionListInternal");
        }

        private void FixShowInAddressBook(Runspace runSpace, string accountName, string[] addressLists, bool HideFromAddressList)
        {
            Command cmd = new Command("Get-DistributionGroup");
            cmd.Parameters.Add("Identity", accountName);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = GetResultObjectDN(result);

            DirectoryEntry dlDEEntry = GetADObject(AddADPrefix(id));
            dlDEEntry.Properties["showInAddressBook"].Clear();
            if (!HideFromAddressList)
            {
                foreach (string addressList in addressLists)
                {
                    dlDEEntry.Properties["showInAddressBook"].Add(addressList);
                }
            }
            dlDEEntry.CommitChanges();
        }

        private void DeleteDistributionListInternal(string accountName)
        {
            ExchangeLog.LogStart("DeleteDistributionListInternal");
            ExchangeLog.DebugInfo("Account Name: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                RemoveDistributionGroup(runSpace, accountName);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DeleteDistributionListInternal");
        }

        internal virtual void RemoveDistributionGroup(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("RemoveDistributionGroup");
            Command cmd = new Command("Remove-DistributionGroup");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("RemoveDistributionGroup");
        }

        private ExchangeDistributionList GetDistributionListGeneralSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetDistributionListGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeDistributionList info = new ExchangeDistributionList();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject distributionGroup = result[0];

                info.DisplayName = (string)GetPSObjectProperty(distributionGroup, "DisplayName");
                info.HideFromAddressBook =
                    (bool)GetPSObjectProperty(distributionGroup, "HiddenFromAddressListsEnabled");

                info.SAMAccountName = string.Format("{0}\\{1}", GetNETBIOSDomainName(), (string)GetPSObjectProperty(distributionGroup, "SamAccountName"));

                cmd = new Command("Get-Group");
                cmd.Parameters.Add("Identity", accountName);
                result = ExecuteShellCommand(runSpace, cmd);
                PSObject group = result[0];

                info.ManagerAccount = GetGroupManagerAccount(runSpace, group);
                info.MembersAccounts = GetGroupMembers(runSpace, accountName);
                info.Notes = (string)GetPSObjectProperty(group, "Notes");
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetDistributionListGeneralSettingsInternal");
            return info;
        }

        internal virtual ExchangeAccount GetGroupManagerAccount(Runspace runSpace, PSObject group)
        {
            return GetExchangeAccount(runSpace, GetGroupManager(group));
        }

        internal virtual string GetGroupManager(PSObject group)
        {
            return ObjToString(GetPSObjectProperty(group, "ManagedBy"));
        }

        private void SetDistributionListGeneralSettingsInternal(string accountName, string displayName,
            bool hideFromAddressBook, string managedBy, string[] memberAccounts, string notes, string[] addressLists)
        {
            ExchangeLog.LogStart("SetDistributionListGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                SetDistributionGroup(runSpace, accountName, displayName, hideFromAddressBook);

                //get old values
                Command cmd = new Command("Get-Group");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject group = result[0];
                string manager = GetGroupManager(group);

                //set members
                ExchangeAccount[] accounts = GetGroupMembers(runSpace, accountName);
                Dictionary<string, string> existingMembers = new Dictionary<string, string>();
                Dictionary<string, string> newMembers = new Dictionary<string, string>();
                List<string> membersToDelete = new List<string>();
                List<string> membersToAdd = new List<string>();

                foreach (ExchangeAccount account in accounts)
                {
                    existingMembers.Add(account.AccountName.ToLower(), account.AccountName);
                }

                foreach (string member in memberAccounts)
                {
                    newMembers.Add(member.ToLower(), member);
                    if (!existingMembers.ContainsKey(member.ToLower()))
                    {
                        membersToAdd.Add(member);
                    }
                }

                foreach (string delAccount in existingMembers.Keys)
                {
                    if (!newMembers.ContainsKey(delAccount))
                    {
                        membersToDelete.Add(existingMembers[delAccount]);
                    }
                }

                foreach (string member in membersToAdd)
                {
                    AddDistributionGroupMember(runSpace, accountName, member);
                }

                foreach (string member in membersToDelete)
                {
                    RemoveDistributionGroupMember(runSpace, accountName, member);
                }

                //remove old manager rights
                if (!string.IsNullOrEmpty(manager))
                {
                    RemoveADPermission(runSpace, accountName, manager, "WriteProperty", null, "Member");
                }

                SetGroup(runSpace, accountName, managedBy, notes);

                if (!string.IsNullOrEmpty(managedBy))
                {
                    AddADPermission(runSpace, accountName, managedBy, "WriteProperty", null, "Member");
                }

                if (addressLists.Length > 0)
                    FixShowInAddressBook(runSpace, accountName, addressLists, hideFromAddressBook);

            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetDistributionListGeneralSettingsInternal");
        }

        internal virtual void RemoveDistributionGroupMember(Runspace runSpace, string group, string member)
        {
            Command cmd = new Command("Remove-DistributionGroupMember");
            cmd.Parameters.Add("Identity", group);
            cmd.Parameters.Add("Member", member);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
        }

        internal virtual void AddDistributionGroupMember(Runspace runSpace, string group, string member)
        {
            Command cmd = new Command("Add-DistributionGroupMember");
            cmd.Parameters.Add("Identity", group);
            cmd.Parameters.Add("Member", member);
            ExecuteShellCommand(runSpace, cmd);
        }

        internal virtual void SetGroup(Runspace runSpace, string id, string managedBy, string notes)
        {
            Command cmd = new Command("Set-Group");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("ManagedBy", managedBy);
            cmd.Parameters.Add("Notes", notes);
            ExecuteShellCommand(runSpace, cmd);
        }

        internal virtual void SetDistributionGroup(Runspace runSpace, string id, string displayName, bool hideFromAddressBook)
        {
            Command cmd = new Command("Set-DistributionGroup");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("DisplayName", displayName);
            cmd.Parameters.Add("HiddenFromAddressListsEnabled", hideFromAddressBook);
            ExecuteShellCommand(runSpace, cmd);
        }


        private void AddDistributionListMembersInternal(string accountName, string[] memberAccounts, string[] addressLists)
        {
            ExchangeLog.LogStart("AddDistributionListMembersInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            if (memberAccounts != null && memberAccounts.Length > 0)
            {
                Runspace runSpace = null;
                try
                {
                    runSpace = OpenRunspace();

                    Command cmd = null;

                    foreach (string member in memberAccounts)
                    {
                        cmd = new Command("Add-DistributionGroupMember");
                        cmd.Parameters.Add("Identity", accountName);
                        cmd.Parameters.Add("Member", member);
                        cmd.Parameters.Add("BypassSecurityGroupManagerCheck", true);
                        ExecuteShellCommand(runSpace, cmd);
                    }

                    if (addressLists.Length > 0)
                    {
                        cmd = new Command("Get-DistributionGroup");
                        cmd.Parameters.Add("Identity", accountName);
                        Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                        PSObject distributionGroup = result[0];

                        FixShowInAddressBook(runSpace, accountName, addressLists, (bool)GetPSObjectProperty(distributionGroup, "HiddenFromAddressListsEnabled"));
                    }

                }
                finally
                {

                    CloseRunspace(runSpace);
                }
            }
            ExchangeLog.LogEnd("AddDistributionListMembersInternal");
        }

        private void RemoveDistributionListMembersInternal(string accountName, string[] memberAccounts, string[] addressLists)
        {
            ExchangeLog.LogStart("RemoveDistributionListMembersInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            if (memberAccounts != null && memberAccounts.Length > 0)
            {
                Runspace runSpace = null;
                try
                {
                    runSpace = OpenRunspace();

                    Command cmd = null;

                    foreach (string member in memberAccounts)
                    {
                        cmd = new Command("Remove-DistributionGroupMember");
                        cmd.Parameters.Add("Identity", accountName);
                        cmd.Parameters.Add("Member", member);
                        cmd.Parameters.Add("Confirm", false);
                        ExecuteShellCommand(runSpace, cmd);
                    }

                    if (addressLists.Length > 0)
                    {
                        cmd = new Command("Get-DistributionGroup");
                        cmd.Parameters.Add("Identity", accountName);
                        Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                        PSObject distributionGroup = result[0];

                        FixShowInAddressBook(runSpace, accountName, addressLists, (bool)GetPSObjectProperty(distributionGroup, "HiddenFromAddressListsEnabled"));
                    }

                }
                finally
                {

                    CloseRunspace(runSpace);
                }
            }
            ExchangeLog.LogEnd("RemoveDistributionListMembersInternal");
        }

        private ExchangeDistributionList GetDistributionListMailFlowSettingsInternal(string accountName)
        {
            ExchangeLog.LogStart("GetDistributionListMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            ExchangeDistributionList info = new ExchangeDistributionList();
            info.AccountName = accountName;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject group = result[0];

                info.AcceptAccounts = GetAcceptedAccounts(runSpace, group);
                info.RejectAccounts = GetRejectedAccounts(runSpace, group);
                info.RequireSenderAuthentication = (bool)GetPSObjectProperty(group, "RequireSenderAuthenticationEnabled");
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetDistributionListMailFlowSettingsInternal");
            return info;
        }

        private void SetDistributionListMailFlowSettingsInternal(string accountName,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication, string[] addressLists)
        {
            ExchangeLog.LogStart("SetDistributionListMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Set-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);

                MultiValuedProperty<ADObjectId> ids = null;
                MultiValuedProperty<ADObjectId> dlIds = null;

                SetAccountIds(runSpace, acceptAccounts, out ids, out dlIds);
                cmd.Parameters.Add("AcceptMessagesOnlyFrom", ids);
                cmd.Parameters.Add("AcceptMessagesOnlyFromDLMembers", dlIds);

                SetAccountIds(runSpace, rejectAccounts, out ids, out dlIds);
                cmd.Parameters.Add("RejectMessagesFrom", ids);
                cmd.Parameters.Add("RejectMessagesFromDLMembers", dlIds);
                cmd.Parameters.Add("RequireSenderAuthenticationEnabled", requireSenderAuthentication);

                ExecuteShellCommand(runSpace, cmd);

                if (addressLists.Length > 0)
                {
                    cmd = new Command("Get-DistributionGroup");
                    cmd.Parameters.Add("Identity", accountName);
                    Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                    PSObject distributionGroup = result[0];

                    FixShowInAddressBook(runSpace, accountName, addressLists, (bool)GetPSObjectProperty(distributionGroup, "HiddenFromAddressListsEnabled"));
                }

            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetDistributionListMailFlowSettingsInternal");
        }

        private ExchangeAccount[] GetGroupMembers(Runspace runSpace, string groupId)
        {
            ExchangeLog.LogStart("GetGroupMembers");
            List<ExchangeAccount> list = new List<ExchangeAccount>();
            Command cmd = new Command("Get-DistributionGroupMember");
            cmd.Parameters.Add("Identity", groupId);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

            ExchangeAccount account = null;
            string id = null;

            foreach (PSObject obj in result)
            {
                id = GetPSObjectIdentity(obj);
                account = GetExchangeAccount(runSpace, id);
                if (account != null)
                {
                    list.Add(account);
                }
                else
                {
                    string distinguishedName = (string)GetPSObjectProperty(obj, "DistinguishedName");
                    string path = ActiveDirectoryUtils.AddADPrefix(distinguishedName, PrimaryDomainController);

                    if (ActiveDirectoryUtils.AdObjectExists(path))
                    {
                        DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

                        list.Add(new ExchangeAccount
                            {
                                AccountName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.SAMAccountName),
                                AccountType = ExchangeAccountType.SecurityGroup
                            });
                    }
                }
            }
            ExchangeLog.LogEnd("GetGroupMembers");
            return list.ToArray();
        }

        private ExchangeEmailAddress[] GetDistributionListEmailAddressesInternal(string accountName)
        {
            ExchangeLog.LogStart("GetDistributionListEmailAddressesInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            List<ExchangeEmailAddress> list = new List<ExchangeEmailAddress>();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject group = result[0];

                string primaryEmail = null;
                string windowsEmail = null;

                SmtpAddress smtpAddress = (SmtpAddress)GetPSObjectProperty(group, "PrimarySmtpAddress");
                if (smtpAddress != null)
                    primaryEmail = smtpAddress.ToString();

                //SmtpAddress winAddress = (SmtpAddress)GetPSObjectProperty(group, "WindowsEmailAddress");
                //if (winAddress != null)
                //	windowsEmail = winAddress.ToString();
                windowsEmail = ObjToString(GetPSObjectProperty(group, "CustomAttribute3"));


                ProxyAddressCollection emails = (ProxyAddressCollection)GetPSObjectProperty(group, "EmailAddresses");
                foreach (ProxyAddress email in emails)
                {
                    //skip windows email
                    if (string.Equals(email.AddressString, windowsEmail, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ExchangeEmailAddress item = new ExchangeEmailAddress();
                    item.Email = email.AddressString;
                    item.Primary = string.Equals(item.Email, primaryEmail, StringComparison.OrdinalIgnoreCase);
                    list.Add(item);
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetDistributionListEmailAddressesInternal");
            return list.ToArray();
        }

        private void SetDistributionListEmailAddressesInternal(string accountName, string[] emailAddresses, string[] addressLists)
        {
            ExchangeLog.LogStart("SetDistributionListEmailAddressesInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject group = result[0];

                //SmtpAddress winAddress = (SmtpAddress)GetPSObjectProperty(group, "WindowsEmailAddress");
                //string windowsEmail = ObjToString(winAddress);
                string windowsEmail = ObjToString(GetPSObjectProperty(group, "CustomAttribute3"));

                ProxyAddressCollection emails = new ProxyAddressCollection();
                ProxyAddress proxy = null;
                string primaryEmail = null;

                if (emailAddresses != null)
                {
                    foreach (string email in emailAddresses)
                    {
                        proxy = ProxyAddress.Parse(email);
                        emails.Add(proxy);
                        if (proxy.IsPrimaryAddress)
                        {
                            primaryEmail = proxy.AddressString;
                        }
                    }
                }
                //add system windows email
                if (!string.IsNullOrEmpty(windowsEmail))
                {
                    emails.Add(ProxyAddress.Parse(windowsEmail));
                }

                cmd = new Command("Set-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("EmailAddresses", emails);
                if (!string.IsNullOrEmpty(primaryEmail))
                {
                    cmd.Parameters.Add("WindowsEmailAddress", primaryEmail);
                }
                ExecuteShellCommand(runSpace, cmd);

                if (addressLists.Length > 0)
                {
                    cmd = new Command("Get-DistributionGroup");
                    cmd.Parameters.Add("Identity", accountName);
                    Collection<PSObject> r = ExecuteShellCommand(runSpace, cmd);
                    PSObject distributionGroup = r[0];

                    FixShowInAddressBook(runSpace, accountName, addressLists, (bool)GetPSObjectProperty(distributionGroup, "HiddenFromAddressListsEnabled"));
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetDistributionListEmailAddressesInternal");
        }

        private void SetDistributionListPrimaryEmailAddressInternal(string accountName, string emailAddress, string[] addressLists)
        {
            ExchangeLog.LogStart("SetDistributionListPrimaryEmailAddressInternal");
            ExchangeLog.DebugInfo("Account: {0}", accountName);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                SmtpAddress primaryEmail = new SmtpAddress(emailAddress);
                Command cmd = new Command("Set-DistributionGroup");
                cmd.Parameters.Add("Identity", accountName);
                cmd.Parameters.Add("PrimarySmtpAddress", primaryEmail);
                cmd.Parameters.Add("WindowsEmailAddress", primaryEmail);

                ExecuteShellCommand(runSpace, cmd);


            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetDistributionListPrimaryEmailAddressInternal");
        }

        private ExchangeDistributionList GetDistributionListPermissionsInternal(string organizationId, string accountName, Runspace runspace)
        {
            ExchangeLog.LogStart("GetDistributionListPermissionsInternal");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("accountName");

            ExchangeDistributionList exchangeDistributionList = null;
            bool closeRunspace = false;

            try
            {
                if (runspace == null)
                {
                    runspace = OpenRunspace();
                    closeRunspace = true;
                }

                Command cmd = new Command("Get-DistributionGroup");

                cmd.Parameters.Add("Identity", accountName);
                Collection<PSObject> result = ExecuteShellCommand(runspace, cmd);
                PSObject distributionGroup = result[0];
                string cn = GetPSObjectProperty(distributionGroup, "Name") as string;


                exchangeDistributionList = new ExchangeDistributionList();
                exchangeDistributionList.AccountName = accountName;
                exchangeDistributionList.SendOnBehalfAccounts = GetSendOnBehalfAccounts(runspace, distributionGroup);
                exchangeDistributionList.SendAsAccounts = GetSendAsAccounts(runspace, organizationId, cn);
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError(ex);
                throw ex;
            }
            finally
            {
                if (closeRunspace)
                    CloseRunspace(runspace);
            }

            ExchangeLog.LogEnd("GetDistributionListPermissionsInternal");
            return exchangeDistributionList;
        }

        private void SetDistributionListPermissionsInternal(string organizationId, string accountName, string[] sendAsAccounts, string[] sendOnBehalfAccounts, string[] addressLists)
        {
            ExchangeLog.LogStart("SetDistributionListPermissionsInternal");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("accountName");

            if (sendAsAccounts == null)
                throw new ArgumentNullException("sendAsAccounts");


            if (sendOnBehalfAccounts == null)
                throw new ArgumentNullException("sendOnBehalfAccounts");

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                string cn = GetDistributionListCommonName(runSpace, accountName);
                ExchangeDistributionList distributionList = GetDistributionListPermissionsInternal(organizationId, accountName, runSpace);
                SetSendAsPermissions(runSpace, distributionList.SendAsAccounts, cn, sendAsAccounts);
                SetDistributionListSendOnBehalfAccounts(runSpace, accountName, sendOnBehalfAccounts);

                if (addressLists.Length > 0)
                {
                    Command cmd = new Command("Get-DistributionGroup");
                    cmd.Parameters.Add("Identity", accountName);
                    Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                    PSObject distributionGroup = result[0];

                    FixShowInAddressBook(runSpace, accountName, addressLists, (bool)GetPSObjectProperty(distributionGroup, "HiddenFromAddressListsEnabled"));
                }

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError(ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("SetDistributionListPermissionsInternal");
        }

        internal virtual void SetDistributionListSendOnBehalfAccounts(Runspace runspace, string accountName, string[] sendOnBehalfAccounts)
        {
            ExchangeLog.LogStart("SetDistributionListSendOnBehalfAccounts");
            Command cmd = new Command("Set-DistributionGroup");
            cmd.Parameters.Add("Identity", accountName);
            cmd.Parameters.Add("GrantSendOnBehalfTo", SetSendOnBehalfAccounts(runspace, sendOnBehalfAccounts));
            ExecuteShellCommand(runspace, cmd);
            ExchangeLog.LogEnd("SetDistributionListSendOnBehalfAccounts");
        }

        private string GetDistributionListCommonName(Runspace runSpace, string accountName)
        {
            ExchangeLog.LogStart("GetDistributionListCommonName");
            Command cmd = new Command("Get-DistributionGroup");
            cmd.Parameters.Add("Identity", accountName);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            PSObject obj = result[0];
            string cn = GetPSObjectProperty(obj, "Name") as string;
            ExchangeLog.LogEnd("GeDistributionListCommonName");
            return cn;
        }

        #endregion

        #region Public folders

        private void CreatePublicFolderInternal(string organizationId, string securityGroup, string parentFolder,
            string folderName, bool mailEnabled, string accountName, string name, string domain)
        {
            ExchangeLog.LogStart("CreatePublicFolderInternal");
            ExchangeLog.DebugInfo("Organization Id: {0}", organizationId);
            ExchangeLog.DebugInfo("Parent: {0}", parentFolder);
            ExchangeLog.DebugInfo("Name: {0}", folderName);

            ExchangeTransaction transaction = StartTransaction();

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                //create organization root folder if required
                CheckOrganizationRootFolder(runSpace, organizationId, securityGroup);

                string id = AddPublicFolder(runSpace, folderName, parentFolder);
                transaction.RegisterNewPublicFolder(string.Empty, id);

                SetPublicFolderPermissions(runSpace, id, securityGroup);

                if (mailEnabled)
                {
                    EnableMailPublicFolderInternal(organizationId, id, accountName, name, domain);
                }

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreatePublicFolderInternal", ex);
                RollbackTransaction(transaction);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CreatePublicFolderInternal");
        }

        private void CheckOrganizationRootFolder(Runspace runSpace, string folder, string user)
        {
            ExchangeLog.LogStart("CheckOrganizationRootFolder");

            Collection<PSObject> result = GetPublicFolderObject(runSpace, "\\" + folder);
            if (result == null || result.Count == 0)
            {
                ExchangeTransaction transaction = StartTransaction();
                try
                {
                    string rootId = AddPublicFolder(runSpace, folder, "\\");
                    transaction.RegisterNewPublicFolder(string.Empty, rootId);
                    SetPublicFolderPermissions(runSpace, rootId, user);
                }
                catch
                {
                    RollbackTransaction(transaction);
                    throw;
                }
            }
            ExchangeLog.LogEnd("CheckOrganizationRootFolder");
        }

        private string AddPublicFolder(Runspace runSpace, string name, string path)
        {
            ExchangeLog.LogStart("CreatePublicFolder");
            Command cmd = new Command("New-PublicFolder");
            cmd.Parameters.Add("Name", name);
            cmd.Parameters.Add("Path", path);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = GetResultObjectIdentity(result);
            ExchangeLog.LogEnd("CreatePublicFolder");
            return id;
        }

        private void DeletePublicFolderInternal(string folder)
        {
            ExchangeLog.LogStart("DeletePublicFolderInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                RemovePublicFolder(runSpace, folder);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DeletePublicFolderInternal");
        }

        private Collection<PSObject> GetPublicFolderObject(Runspace runSpace, string id)
        {
            Command cmd = new Command("Get-PublicFolder");
            cmd.Parameters.Add("Identity", id);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            return result;
        }

        private bool PublicFolderExists(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("PublicFolderExists");

            Collection<PSObject> result = GetPublicFolderObject(runSpace, id);
            bool ret = (result != null && result.Count == 1);
            ExchangeLog.LogEnd("PublicFolderExists");
            return ret;
        }

        private void RemovePublicFolder(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("RemovePublicFolder");

            Command cmd = new Command("Remove-PublicFolder");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Recurse", new SwitchParameter(true));
            cmd.Parameters.Add("Confirm", false);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            ExecuteShellCommand(runSpace, cmd);

            ExchangeLog.LogEnd("RemovePublicFolder");
        }

        private void SetPublicFolderPermissions(Runspace runSpace, string folder, string securityGroup)
        {
            //set the default Permission to 'Reviewer'
            RemovePublicFolderClientPermission(runSpace, folder, "Default", "Author");
            AddPublicFolderClientPermission(runSpace, folder, securityGroup, "Reviewer");
        }

        private void RemovePublicFolderClientPermission(Runspace runSpace, string id, string user,
            string permission)
        {
            ExchangeLog.LogStart("RemovePublicFolderClientPermission");

            Command cmd = new Command("Remove-PublicFolderClientPermission");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("User", user);
            cmd.Parameters.Add("AccessRights", permission);
            cmd.Parameters.Add("Confirm", false);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("RemovePublicFolderClientPermission");
        }

        private void AddPublicFolderClientPermission(Runspace runSpace, string id, string user, string permission)
        {
            ExchangeLog.LogStart("AddPublicFolderClientPermission");

            Command cmd = new Command("Add-PublicFolderClientPermission");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("User", user);
            cmd.Parameters.Add("AccessRights", permission);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("AddPublicFolderClientPermission");
        }

        private void GetPublicFolderClientPermission(Runspace runSpace, string id)
        {
            Command cmd = new Command("Get-PublicFolderClientPermission");
            cmd.Parameters.Add("Identity", id);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            PSObject obj = result[0];
        }


        private void EnableMailPublicFolderInternal(string organizationId, string folder, string accountName,
            string name, string domain)
        {
            ExchangeLog.LogStart("EnableMailPublicFolderInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Enable-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                ExecuteShellCommand(runSpace, cmd);
                string id = null;

                //try to avoid message: "The Active Directory proxy object for the public folder 'XXX'
                // is being generated. Please try again later."
                int attempts = 0;
                string windowsEmailAddress = null;
                while (true)
                {
                    cmd = new Command("Get-MailPublicFolder");
                    cmd.Parameters.Add("Identity", folder);
                    if (!string.IsNullOrEmpty(PublicFolderServer))
                        cmd.Parameters.Add("Server", PublicFolderServer);
                    Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                    if (result != null && result.Count > 0)
                    {
                        id = GetResultObjectIdentity(result);
                        windowsEmailAddress = ObjToString(GetPSObjectProperty(result[0], "WindowsEmailAddress"));
                        break;
                    }

                    if (attempts > 9)
                    {
                        string error = string.Format("Active Directory proxy object for the public folder '{0}' was not found or not generated yet.", folder);
                        throw new Exception(error);
                    }

                    attempts++;
                    ExchangeLog.LogWarning("Attemp {0} to create mail public folder {1}", attempts, folder);
                    System.Threading.Thread.Sleep(5000);
                }

                string email = string.Format("{0}@{1}", name, domain);
                // fix issue with 2 DC
                attempts = 0;
                bool success = false;
                object[] errors;
                while (true)
                {
                    try
                    {
                        cmd = new Command("Set-MailPublicFolder");
                        cmd.Parameters.Add("Identity", id);
                        cmd.Parameters.Add("Alias", accountName);
                        cmd.Parameters.Add("EmailAddressPolicyEnabled", false);
                        cmd.Parameters.Add("CustomAttribute1", organizationId);
                        cmd.Parameters.Add("CustomAttribute3", windowsEmailAddress);
                        cmd.Parameters.Add("PrimarySmtpAddress", email);
                        ExecuteShellCommand(runSpace, cmd, out errors);

                        if (errors.Length == 0)
                            success = true;
                    }
                    catch (Exception ex)
                    {
                        ExchangeLog.LogError(ex);
                    }

                    if (success)
                        break;

                    if (attempts > 9)
                    {
                        string error = string.Format("Mail public folder '{0}' was not found or not generated yet.", id);
                        throw new Exception(error);
                    }

                    attempts++;
                    ExchangeLog.LogWarning("Attemp {0} to update mail public folder {1}", attempts, folder);
                    System.Threading.Thread.Sleep(5000);
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("EnableMailPublicFolderInternal");
        }

        private void DisableMailPublicFolderInternal(string folder)
        {
            ExchangeLog.LogStart("DisableMailPublicFolderInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Disable-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                cmd.Parameters.Add("Confirm", false);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("DisableMailPublicFolderInternal");
        }

        private ExchangePublicFolder GetPublicFolderGeneralSettingsInternal(string folder)
        {
            ExchangeLog.LogStart("GetPublicFolderGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            ExchangePublicFolder info = new ExchangePublicFolder();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetPublicFolderObject(runSpace, folder);
                PSObject publicFolder = result[0];

                info.Name = (string)GetPSObjectProperty(publicFolder, "Name");
                info.MailEnabled = (bool)GetPSObjectProperty(publicFolder, "MailEnabled");
                info.HideFromAddressBook = (bool)GetPSObjectProperty(publicFolder, "HiddenFromAddressListsEnabled");
                info.NETBIOS = GetNETBIOSDomainName();
                info.Accounts = GetPublicFolderAccounts(runSpace, folder);

                if (info.MailEnabled)
                {
                    Command cmd = new Command("Get-MailPublicFolder");
                    cmd.Parameters.Add("Identity", folder);
                    if (!string.IsNullOrEmpty(PublicFolderServer))
                        cmd.Parameters.Add("Server", PublicFolderServer);
                    result = ExecuteShellCommand(runSpace, cmd);
                    if (result.Count > 0)
                    {
                        publicFolder = result[0];
                        info.SAMAccountName = string.Format("{0}\\{1}", GetNETBIOSDomainName(), (string)GetPSObjectProperty(publicFolder, "Alias"));
                    }
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetPublicFolderGeneralSettingsInternal");
            return info;
        }

        private ExchangeAccount[] GetPublicFolderAccounts(Runspace runSpace, string folder)
        {
            ExchangeLog.LogStart("GetPublicFolderAccounts");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            List<ExchangeAccount> list = new List<ExchangeAccount>();
            ExchangeAccount account = null;

            Command cmd = new Command("Get-PublicFolderClientPermission");
            cmd.Parameters.Add("Identity", folder);
            if (!string.IsNullOrEmpty(PublicFolderServer))
                cmd.Parameters.Add("Server", PublicFolderServer);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

            foreach (PSObject obj in result)
            {
                string userId = ObjToString(GetPSObjectProperty(obj, "User"));
                if (userId == "Default" || userId == "Anonymous" || userId.StartsWith("NT User:") == true)
                    continue;

                object rights = GetPSObjectProperty(obj, "AccessRights");
                int count = (int)GetObjectPropertyValue(rights, "Count");
                for (int i = 0; i < count; i++)
                {
                    account = GetExchangeAccount(runSpace, userId);
                    string permission = ObjToString(GetObjectIndexerValue(rights, i));
                    if (account != null)
                        account.PublicFolderPermission = permission;
                    list.Add(account);
                    break;
                }
            }
            ExchangeLog.LogEnd("GetPublicFolderAccounts");
            return list.ToArray();
        }

        private void SetPublicFolderGeneralSettingsInternal(string folder, string newFolderName,
            bool hideFromAddressBook, ExchangeAccount[] accounts)
        {
            ExchangeLog.LogStart("SetPublicFolderGeneralSettingsInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            ExchangePublicFolder info = new ExchangePublicFolder();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Collection<PSObject> result = GetPublicFolderObject(runSpace, folder);
                PSObject publicFolder = result[0];
                string folderName = (string)GetPSObjectProperty(publicFolder, "Name");
                ExchangeAccount[] allAccounts = GetPublicFolderAccounts(runSpace, folder);

                //Remove all accounts and re-apply               
                List<ExchangeAccount> accountsToDelete = new List<ExchangeAccount>();
                List<ExchangeAccount> accountsToAdd = new List<ExchangeAccount>();

                foreach (ExchangeAccount existingAccount in allAccounts)
                {
                    try
                    {
                        RemovePublicFolderClientPermission(runSpace,
                                                            folder,
                                                            existingAccount.AccountName.Contains("@") ? existingAccount.AccountName : @"\" + existingAccount.AccountName,
                                                            existingAccount.PublicFolderPermission);
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }

                foreach (ExchangeAccount newAccount in accounts)
                {
                    try
                    {
                        AddPublicFolderClientPermission(runSpace,
                                                        folder,
                                                        newAccount.AccountName.Contains("@") ? newAccount.AccountName : @"\" + newAccount.AccountName,
                                                        newAccount.PublicFolderPermission);
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }

                //general settings
                Command cmd = new Command("Set-PublicFolder");
                cmd.Parameters.Add("Identity", folder);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                if (!folderName.Equals(newFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    cmd.Parameters.Add("Name", newFolderName);
                }
                cmd.Parameters.Add("HiddenFromAddressListsEnabled", hideFromAddressBook);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetPublicFolderGeneralSettingsInternal");
        }

        private ExchangePublicFolder GetPublicFolderMailFlowSettingsInternal(string folder)
        {
            ExchangeLog.LogStart("GetPublicFolderMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            ExchangePublicFolder info = new ExchangePublicFolder();

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject publicFolder = result[0];

                info.AcceptAccounts = GetAcceptedAccounts(runSpace, publicFolder);
                info.RejectAccounts = GetRejectedAccounts(runSpace, publicFolder);
                info.RequireSenderAuthentication = (bool)GetPSObjectProperty(publicFolder, "RequireSenderAuthenticationEnabled");
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetPublicFolderMailFlowSettingsInternal");
            return info;
        }

        private void SetPublicFolderMailFlowSettingsInternal(string folder,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            ExchangeLog.LogStart("SetPublicFolderMailFlowSettingsInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Set-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);

                MultiValuedProperty<ADObjectId> ids = null;
                MultiValuedProperty<ADObjectId> dlIds = null;

                SetAccountIds(runSpace, acceptAccounts, out ids, out dlIds);
                cmd.Parameters.Add("AcceptMessagesOnlyFrom", ids);
                cmd.Parameters.Add("AcceptMessagesOnlyFromDLMembers", dlIds);

                SetAccountIds(runSpace, rejectAccounts, out ids, out dlIds);
                cmd.Parameters.Add("RejectMessagesFrom", ids);
                cmd.Parameters.Add("RejectMessagesFromDLMembers", dlIds);
                cmd.Parameters.Add("RequireSenderAuthenticationEnabled", requireSenderAuthentication);

                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetPublicFolderMailFlowSettingsInternal");

        }


        private ExchangeEmailAddress[] GetPublicFolderEmailAddressesInternal(string folder)
        {
            ExchangeLog.LogStart("GetPublicFolderEmailAddressesInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            List<ExchangeEmailAddress> list = new List<ExchangeEmailAddress>();
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject publicFolder = result[0];

                string primaryEmail = null;
                string windowsEmail = null;

                SmtpAddress smtpAddress = (SmtpAddress)GetPSObjectProperty(publicFolder, "PrimarySmtpAddress");
                if (smtpAddress != null)
                    primaryEmail = smtpAddress.ToString();

                //SmtpAddress winAddress = (SmtpAddress)GetPSObjectProperty(publicFolder, "WindowsEmailAddress");
                //if (winAddress != null)
                //    windowsEmail = winAddress.ToString();
                windowsEmail = ObjToString(GetPSObjectProperty(publicFolder, "CustomAttribute3"));


                ProxyAddressCollection emails = (ProxyAddressCollection)GetPSObjectProperty(publicFolder, "EmailAddresses");
                foreach (ProxyAddress email in emails)
                {
                    //skip windows email
                    if (string.Equals(email.AddressString, windowsEmail, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ExchangeEmailAddress item = new ExchangeEmailAddress();
                    item.Email = email.AddressString;
                    item.Primary = string.Equals(item.Email, primaryEmail, StringComparison.OrdinalIgnoreCase);
                    list.Add(item);
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetPublicFolderEmailAddressesInternal");
            return list.ToArray();
        }

        private void SetPublicFolderEmailAddressesInternal(string folder, string[] emailAddresses)
        {
            ExchangeLog.LogStart("SetDistributionListEmailAddressesInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                Command cmd = new Command("Get-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                PSObject publicFolder = result[0];

                //SmtpAddress winAddress = (SmtpAddress)GetPSObjectProperty(publicFolder, "WindowsEmailAddress");
                string windowsEmail = ObjToString(GetPSObjectProperty(publicFolder, "CustomAttribute3"));
                //string windowsEmail = ObjToString(winAddress);

                ProxyAddressCollection emails = new ProxyAddressCollection();
                ProxyAddress proxy = null;
                if (emailAddresses != null)
                {
                    foreach (string email in emailAddresses)
                    {
                        proxy = ProxyAddress.Parse(email);
                        emails.Add(proxy);
                    }
                }
                //add system windows email
                if (!string.IsNullOrEmpty(windowsEmail))
                {
                    emails.Add(ProxyAddress.Parse(windowsEmail));
                }

                cmd = new Command("Set-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                cmd.Parameters.Add("EmailAddresses", emails);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetPublicFolderEmailAddressesInternal");
        }

        private void SetPublicFolderPrimaryEmailAddressInternal(string folder, string emailAddress)
        {
            ExchangeLog.LogStart("SetPublicFolderPrimaryEmailAddressInternal");
            ExchangeLog.DebugInfo("Folder: {0}", folder);
            ExchangeLog.DebugInfo("Email: {0}", emailAddress);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                SmtpAddress primaryEmail = new SmtpAddress(emailAddress);
                Command cmd = new Command("Set-MailPublicFolder");
                cmd.Parameters.Add("Identity", folder);
                cmd.Parameters.Add("PrimarySmtpAddress", primaryEmail);

                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetPublicFolderPrimaryEmailAddressInternal");
        }

        private ExchangeItemStatistics[] GetPublicFoldersStatisticsInternal(string[] folders)
        {
            ExchangeLog.LogStart("GetPublicFoldersStatisticsInternal");

            Runspace runSpace = null;
            List<ExchangeItemStatistics> ret = new List<ExchangeItemStatistics>();
            try
            {
                runSpace = OpenRunspace();

                PSObject obj = null;
                foreach (string folder in folders)
                {
                    Command cmd = new Command("Get-PublicFolderStatistics");
                    cmd.Parameters.Add("Identity", folder);
                    if (!string.IsNullOrEmpty(PublicFolderServer))
                        cmd.Parameters.Add("Server", PublicFolderServer);
                    Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                    if (result != null && result.Count > 0)
                    {
                        obj = result[0];
                        ExchangeItemStatistics info = new ExchangeItemStatistics();
                        info.ItemName = (string)GetPSObjectProperty(obj, "FolderPath");
                        Unlimited<ByteQuantifiedSize> totalItemSize =
                            (Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(obj, "TotalItemSize");
                        info.TotalSizeMB = ConvertUnlimitedToMB(totalItemSize);
                        uint? itemCount = (uint?)GetPSObjectProperty(obj, "ItemCount");
                        info.TotalItems = ConvertNullableToInt32(itemCount);

                        DateTime? lastAccessTime = (DateTime?)GetPSObjectProperty(obj, "LastAccessTime"); ;
                        DateTime? lastModificationTime = (DateTime?)GetPSObjectProperty(obj, "LastModificationTime"); ;
                        info.LastAccessTime = ConvertNullableToDateTime(lastAccessTime);
                        info.LastModificationTime = ConvertNullableToDateTime(lastModificationTime);
                        ret.Add(info);
                    }
                }
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetPublicFoldersStatisticsInternal");
            return ret.ToArray();
        }

        private string[] GetPublicFoldersRecursiveInternal(string parent)
        {
            ExchangeLog.LogStart("GetPublicFoldersRecursiveInternal");

            Runspace runSpace = null;
            List<string> ret = new List<string>();
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-PublicFolder");
                cmd.Parameters.Add("Identity", parent);
                cmd.Parameters.Add("Recurse", true);
                if (!string.IsNullOrEmpty(PublicFolderServer))
                    cmd.Parameters.Add("Server", PublicFolderServer);

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                if (result != null)
                {
                    foreach (PSObject obj in result)
                    {
                        ret.Add(GetPSObjectIdentity(obj));
                    }
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetPublicFoldersRecursiveInternal");
            return ret.ToArray();
        }

        private long GetPublicFolderSizeInternal(string folder)
        {
            ExchangeLog.LogStart("GetPublicFolderSizeInternal");
            long size = 0;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                size += CalculatePublicFolderDiskSpace(runSpace, folder);
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetPublicFolderSizeInternal");
            return size;
        }

        public string CreateOrganizationRootPublicFolder(string organizationId, string organizationDistinguishedName, string securityGroup, string organizationDomain)
        {
            // not implemented
            return null;
        }

        public string[] SetDefaultPublicFolderMailbox(string id, string organizationId, string organizationDistinguishedName)
        {
            // not implemented
            return null;
        }


        #endregion

        #region Address Lists (GAL, AL, RAL, OAB, ABP)

        private string GetAddressListDN(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("GetAddressListDN");
            string resultObjectDN = null;
            Command cmd = new Command("Get-AddressList");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = this.ExecuteShellCommand(runSpace, cmd);
            if ((result != null) && (result.Count > 0))
            {
                resultObjectDN = this.GetResultObjectDN(result);
                ExchangeLog.DebugInfo("AL DN: {0}", new object[] { resultObjectDN });
            }
            ExchangeLog.LogEnd("GetAddressListDN");
            return resultObjectDN;
        }

        internal string CreateAddressList(Runspace runSpace, string organizationId)
        {
            ExchangeLog.LogStart("CreateAddressList");
            string addressListName = this.GetAddressListName(organizationId);
            string addressListDN = this.GetAddressListDN(runSpace, addressListName);
            if (!string.IsNullOrEmpty(addressListDN))
            {
                //address list already exists - we will use it
                ExchangeLog.LogWarning("Address List '{0}' already exists", new object[] { addressListName });
            }
            else
            {
                //try to create a new address list (10 attempts)
                int attempts = 0;
                Command cmd = null;
                Collection<PSObject> result = null;

                while (true)
                {
                    try
                    {
                        //try to create address list
                        cmd = new Command("New-AddressList");
                        cmd.Parameters.Add("Name", addressListName);
                        cmd.Parameters.Add("IncludedRecipients", "AllRecipients");
                        cmd.Parameters.Add("ConditionalCustomAttribute1", organizationId);

                        result = ExecuteShellCommand(runSpace, cmd);
                        addressListDN = CheckResultObjectDN(result);
                    }
                    catch (Exception ex)
                    {
                        ExchangeLog.LogError(ex);
                    }
                    if (addressListDN != null)
                        break;

                    if (attempts > 9)
                        throw new Exception(
                            string.Format("Could not create Address List '{0}' ", addressListName));

                    attempts++;
                    ExchangeLog.LogWarning("Attempt #{0} to create address list failed!", attempts);
                    // wait 1 sec
                    System.Threading.Thread.Sleep(1000);
                }
            }

            ExchangeLog.LogEnd("CreateAddressList");
            return addressListDN;
        }

        internal string CreateRoomsAddressList(Runspace runSpace, string organizationId)
        {
            ExchangeLog.LogStart("CreateRoomList");
            string addressListName = this.GetRoomsAddressListName(organizationId);
            string addressListDN = this.GetAddressListDN(runSpace, addressListName);
            if (!string.IsNullOrEmpty(addressListDN))
            {
                //address list already exists - we will use it
                ExchangeLog.LogWarning("Room List '{0}' already exists", new object[] { addressListName });
            }
            else
            {
                //try to create a new address list (10 attempts)
                int attempts = 0;
                Command cmd = null;
                Collection<PSObject> result = null;

                while (true)
                {
                    try
                    {
                        //try to create address list
                        cmd = new Command("New-AddressList");
                        cmd.Parameters.Add("Name", addressListName);
                        cmd.Parameters.Add("IncludedRecipients", "Resources");
                        cmd.Parameters.Add("ConditionalCustomAttribute1", organizationId);

                        result = ExecuteShellCommand(runSpace, cmd);
                        addressListDN = CheckResultObjectDN(result);
                    }
                    catch (Exception ex)
                    {
                        ExchangeLog.LogError(ex);
                    }
                    if (addressListDN != null)
                        break;

                    if (attempts > 9)
                        throw new Exception(
                            string.Format("Could not create Room List '{0}' ", addressListName));

                    attempts++;
                    ExchangeLog.LogWarning("Attempt #{0} to create room list failed!", attempts);
                    // wait 1 sec
                    System.Threading.Thread.Sleep(1000);
                }
            }

            ExchangeLog.LogEnd("CreateRoomList");
            return addressListDN;
        }


        internal void UpdateAddressList(Runspace runSpace, string id, string securityGroup)
        {
            ExchangeLog.LogStart("UpdateAddressList");

            string path = AddADPrefix(id);
            Command cmd = new Command("Update-AddressList");
            cmd.Parameters.Add("Identity", id);
            ExecuteShellCommand(runSpace, cmd);

            AdjustADSecurity(path, securityGroup, false);

            ExchangeLog.LogEnd("UpdateAddressList");
        }

        internal void DeleteAddressList(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DeleteAddressList");
            Command cmd = new Command("Remove-AddressList");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DeleteAddressList");
        }


        internal virtual void DeleteAddressBookPolicy(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DeleteAddressBookPolicy");
            ExchangeLog.LogEnd("DeleteAddressBookPolicy");
        }



        internal string GetGlobalAddressListDN(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("GetGlobalAddressListDN");
            string resultObjectDN = null;
            Command cmd = new Command("Get-GlobalAddressList");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = this.ExecuteShellCommand(runSpace, cmd);
            if ((result != null) && (result.Count > 0))
            {
                resultObjectDN = this.GetResultObjectDN(result);
                ExchangeLog.DebugInfo("GAL DN: {0}", new object[] { resultObjectDN });
            }
            ExchangeLog.LogEnd("GetGlobalAddressListDN");
            return resultObjectDN;
        }


        internal string CreateGlobalAddressList(Runspace runSpace, string organizationId)
        {
            ExchangeLog.LogStart("CreateGlobalAddressList");

            string name = GetGlobalAddressListName(organizationId);

            Command cmd = new Command("New-GlobalAddressList");
            cmd.Parameters.Add("Name", name);
            cmd.Parameters.Add("RecipientFilter", string.Format("(Alias -ne $null -and CustomAttribute1 -eq '{0}')", organizationId));

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = GetResultObjectDN(result);

            ExchangeLog.LogEnd("CreateGlobalAddressList");
            return id;
        }

        internal void UpdateGlobalAddressList(Runspace runSpace, string id, string securityGroup)
        {
            ExchangeLog.LogStart("UpdateGlobalAddressList");


            Command cmd = new Command("Update-GlobalAddressList");
            cmd.Parameters.Add("Identity", id);
            ExecuteShellCommand(runSpace, cmd);

            string path = AddADPrefix(id);
            AdjustADSecurity(path, securityGroup, false);

            ExchangeLog.LogEnd("UpdateGlobalAddressList");
        }

        internal void DeleteGlobalAddressList(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DeleteGlobalAddressList");
            Command cmd = new Command("Remove-GlobalAddressList");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DeleteGlobalAddressList");
        }

        private string CreateOfflineAddressBook(Runspace runSpace, string organizationId, string server, string oabVirtualDirs)
        {
            ExchangeLog.LogStart("CreateOfflineAddressBook");

            string oabName = GetOfflineAddressBookName(organizationId);
            string addressListName = GetAddressListName(organizationId);

            Command cmd = new Command("New-OfflineAddressBook");
            cmd.Parameters.Add("Name", oabName);
            cmd.Parameters.Add("Server", server);
            cmd.Parameters.Add("AddressLists", addressListName);
            cmd.Parameters.Add("PublicFolderDistributionEnabled", PublicFolderDistributionEnabled);
            cmd.Parameters.Add("IsDefault", false);


            //TODO: fix web distribution
            if (!string.IsNullOrEmpty(oabVirtualDirs))
            {
                ArrayList virtualDirs = new ArrayList();
                string[] strTmp = oabVirtualDirs.Split(',');
                foreach (string s in strTmp)
                {
                    virtualDirs.Add(s);
                }

                cmd.Parameters.Add("VirtualDirectories", (String[])virtualDirs.ToArray(typeof(string)));
            }

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = GetResultObjectDN(result);

            ExchangeLog.LogEnd("CreateOfflineAddressBook");

            return id;
        }

        private void UpdateOfflineAddressBook(Runspace runSpace, string id, string securityGroup)
        {
            ExchangeLog.LogStart("UpdateOfflineAddressBook");

            string path = AddADPrefix(id);

			//Command cmd = new Command("Update-OfflineAddressBook");
			//cmd.Parameters.Add("Identity", id);
			//ExecuteShellCommand(runSpace, cmd);

            AdjustADSecurity(path, securityGroup, true);

            ExchangeLog.LogEnd("UpdateOfflineAddressBook");
        }


        internal void DeleteOfflineAddressBook(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DeleteOfflineAddressBook");
            Command cmd = new Command("Remove-OfflineAddressBook");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DeleteOfflineAddressBook");
        }

        private void DeleteAddressPolicy(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DeleteAddressPolicy");
            Command cmd = new Command("Remove-AddressBookPolicy");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DeleteAddressPolicy");
        }

        private string GetAddressListName(string orgName)
        {
            return orgName + " Address List";
        }

        internal string GetGlobalAddressListName(string orgName)
        {
            return orgName + " Global Address List";
        }

        private string GetOfflineAddressBookName(string orgName)
        {
            return orgName + " Offline Address Book";
        }

        internal string GetAddressBookPolicyName(string orgName)
        {
            return orgName + " Address Policy";
        }

        private string GetRoomsAddressListName(string orgName)
        {
            return orgName + " Rooms";
        }

        #endregion

        #region Active Directory
        private string CreateOrganizationalUnit(string name)
        {
            ExchangeLog.LogStart("CreateOrganizationalUnit");

            string ret = null;
            DirectoryEntry ou = null;
            DirectoryEntry rootOU = null;
            try
            {
                rootOU = GetRootOU();
                ou = rootOU.Children.Add(
                     string.Format("OU={0}", name),
                     rootOU.SchemaClassName);

                ret = ou.Path;
                ou.CommitChanges();
            }
            finally
            {
                if (ou != null)
                    ou.Close();
                if (rootOU != null)
                    rootOU.Close();
            }

            ExchangeLog.LogEnd("CreateOrganizationalUnit");
            return ret;
        }

        private void DeleteADObject(string id)
        {
            ExchangeLog.LogStart("DeleteADObject");

            string path = AddADPrefix(id);

            DirectoryEntry ou = GetADObject(path);
            DirectoryEntry parent = ou.Parent;
            if (parent != null)
            {
                parent.Children.Remove(ou);
                parent.CommitChanges();
            }

            ExchangeLog.LogEnd("DeleteADObject");
        }

        private DirectoryEntry GetRootOU()
        {
            ExchangeLog.LogStart("GetRootOU");
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            DirectoryEntry de = GetADObject(sb.ToString());
            ExchangeLog.LogEnd("GetRootOU");
            return de;
        }

        private void SetADObjectProperty(DirectoryEntry oDE, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (oDE.Properties.Contains(name))
                {
                    oDE.Properties[name][0] = value;
                }
                else
                {
                    oDE.Properties[name].Add(value);
                }
            }
        }

        internal void SetADObjectPropertyValue(DirectoryEntry oDE, string name, string value)
        {
            PropertyValueCollection collection = oDE.Properties[name];
            collection.Value = value;
        }


        internal void AddADObjectProperty(DirectoryEntry oDE, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                oDE.Properties[name].Add(value);

            }
        }

        internal DirectoryEntry GetADObject(string path)
        {
            DirectoryEntry de = null;
            if (path.StartsWith("LDAP://" + PrimaryDomainController + "/", true, CultureInfo.InvariantCulture))
            {
                ExchangeLog.LogInfo("Get Active Directory Object {0} from {1}", path, PrimaryDomainController);
                de = new DirectoryEntry(path, null, null, AuthenticationTypes.ServerBind);
            }
            else
            {
                ExchangeLog.LogInfo("Get Active Directory Object {0}", path);
                de = new DirectoryEntry(path);
            }
            if (de != null)
                de.RefreshCache();
            return de;
        }

        internal object GetADObjectProperty(DirectoryEntry entry, string name)
        {
            if (entry.Properties.Contains(name))
                return entry.Properties[name][0];
            else
                return String.Empty;
        }


        private string GetOrganizationPath(string organizationId)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private void AppendProtocol(StringBuilder sb)
        {
            sb.Append("LDAP://");
        }

        private void AppendDomainController(StringBuilder sb)
        {
            sb.Append(PrimaryDomainController + "/");
        }

        private void AppendOUPath(StringBuilder sb, string ou)
        {
            if (string.IsNullOrEmpty(ou))
                return;

            string path = ou.Replace("/", "\\");
            string[] parts = path.Split('\\');
            for (int i = parts.Length - 1; i != -1; i--)
                sb.Append("OU=").Append(parts[i]).Append(",");
        }

        private void AppendDomainPath(StringBuilder sb, string domain)
        {
            if (string.IsNullOrEmpty(domain))
                return;

            string[] parts = domain.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                sb.Append("DC=").Append(parts[i]);

                if (i < (parts.Length - 1))
                    sb.Append(",");
            }
        }

        internal string AddADPrefix(string path)
        {
            string dn = path;
            if (!dn.ToUpper().StartsWith("LDAP://"))
            {
                dn = string.Format("LDAP://{0}/{1}", PrimaryDomainController, dn);
            }
            return dn;
        }

        private string RemoveADPrefix(string path)
        {
            string dn = path;
            if (dn.ToUpper().StartsWith("LDAP://"))
            {
                dn = dn.Substring(7);
            }
            int index = dn.IndexOf("/");

            if (index != -1)
            {
                dn = dn.Substring(index + 1);
            }
            return dn;
        }

        internal string ConvertADPathToCanonicalName(string name)
        {

            if (string.IsNullOrEmpty(name))
                return null;

            StringBuilder ret = new StringBuilder();
            List<string> cn = new List<string>();
            List<string> dc = new List<string>();

            name = RemoveADPrefix(name);

            string[] parts = name.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("DC="))
                {
                    dc.Add(parts[i].Substring(3));
                }
                else if (parts[i].StartsWith("OU=") || parts[i].StartsWith("CN="))
                {
                    cn.Add(parts[i].Substring(3));
                }
            }

            for (int i = 0; i < dc.Count; i++)
            {
                ret.Append(dc[i]);
                if (i < dc.Count - 1)
                    ret.Append(".");
            }
            for (int i = cn.Count - 1; i != -1; i--)
            {
                ret.Append("/");
                ret.Append(cn[i]);
            }
            return ret.ToString();
        }

        private string ConvertDomainName(string name)
        {

            if (string.IsNullOrEmpty(name))
                return null;

            StringBuilder ret = new StringBuilder("LDAP://");

            string[] parts = name.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                ret.Append("DC=");
                ret.Append(parts[i]);
                if (i < parts.Length - 1)
                    ret.Append(",");
            }
            return ret.ToString();
        }


        internal virtual void AdjustADSecurity(string objPath, string securityGroupPath, bool isAddressBook)
        {
            ExchangeLog.LogStart("AdjustADSecurity");
            ExchangeLog.DebugInfo("  Active Direcory object: {0}", objPath);
            ExchangeLog.DebugInfo("  Security Group: {0}", securityGroupPath);

            //"Open Address Book" security permission for address lists or
            //"Download Address Book" security permission for offline address book
            Guid openAddressBookGuid = new Guid("{a1990816-4298-11d1-ade2-00c04fd8d5cd}");
            if (isAddressBook)
                openAddressBookGuid = new Guid("{bd919c7c-2d79-4950-bc9c-e16fd99285e8}");


            DirectoryEntry groupEntry = GetADObject(securityGroupPath);
            byte[] byteSid = (byte[])GetADObjectProperty(groupEntry, "objectSid");

            DirectoryEntry objEntry = GetADObject(objPath);
            ActiveDirectorySecurity security = objEntry.ObjectSecurity;

            // Create a SecurityIdentifier object for "AuthenticatedUsers".
            SecurityIdentifier auSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            // Create a SecurityIdentifier object for security group.
            SecurityIdentifier groupSid = new SecurityIdentifier(byteSid, 0);

            // Create an access rule to allow AuthenticatedUsers to open address book. 
            // This is used to remove any existing access rules.
            ActiveDirectoryAccessRule allowOpenAddressBook =
                new ActiveDirectoryAccessRule(
                    auSid,
                    ActiveDirectoryRights.ExtendedRight,
                    AccessControlType.Allow,
                    openAddressBookGuid);

            // Create an access rule to allow AuthenticatedUsers to read object. 
            // This is used to remove any existing access rules.
            ActiveDirectoryAccessRule allowRead =
                new ActiveDirectoryAccessRule(
                    auSid,
                    ActiveDirectoryRights.GenericRead,
                    AccessControlType.Allow);


            // Remove any existing rule that gives "AuthenticatedUsers" the right to open address book.
            security.RemoveAccessRuleSpecific(allowOpenAddressBook);
            // Remove any existing rule that gives "AuthenticatedUsers" the right to read object.
            security.RemoveAccessRuleSpecific(allowRead);

            // Create an access rule to allow users in Security Group to open address book. 
            allowOpenAddressBook =
                new ActiveDirectoryAccessRule(
                    groupSid,
                    ActiveDirectoryRights.ExtendedRight,
                    AccessControlType.Allow,
                    openAddressBookGuid);

            // Create an access rule to allow users in Security Group to read object. 
            allowRead =
                new ActiveDirectoryAccessRule(
                    groupSid,
                    ActiveDirectoryRights.GenericRead,
                    AccessControlType.Allow);


            // Remove existing rules if exist
            security.RemoveAccessRuleSpecific(allowOpenAddressBook);
            security.RemoveAccessRuleSpecific(allowRead);

            // Add a new access rule to allow users in Security Group to open address book. 
            security.AddAccessRule(allowOpenAddressBook);
            // Add a new access rule to allow users in Security Group to read object. 
            security.AddAccessRule(allowRead);

            // Commit the changes.
            objEntry.CommitChanges();
            ExchangeLog.LogEnd("AdjustADSecurity");
        }

        private void AddUPNSuffix(string ouPath, string suffix)
        {
            ExchangeLog.LogStart("AddUPNSuffix");
            //Add UPN Suffix to the OU
            DirectoryEntry ou = GetADObject(ouPath);
            AddADObjectProperty(ou, "uPNSuffixes", suffix);
            ou.CommitChanges();
            ou.Close();
            ExchangeLog.LogEnd("AddUPNSuffix");
        }

        private void RemoveUPNSuffix(string ouPath, string suffix)
        {
            ExchangeLog.LogStart("RemoveUPNSuffix");

            if (DirectoryEntry.Exists(ouPath))
            {
                DirectoryEntry ou = GetADObject(ouPath);
                PropertyValueCollection prop = null;
                try
                {
                    prop = ou.Properties["uPNSuffixes"];
                }
                catch (Exception ex)
                {
                    ExchangeLog.LogWarning("AD object or property not found: {0}", ex);
                }

                if (prop != null)
                {
                    if (ou.Properties["uPNSuffixes"].Contains(suffix))
                    {
                        ou.Properties["uPNSuffixes"].Remove(suffix);
                        ou.CommitChanges();
                    }
                    ou.Close();
                }
            }
            ExchangeLog.LogEnd("RemoveUPNSuffix");
        }

        /*private void AddGlobalUPNSuffix(string name)
        {
            ExchangeLog.LogStart("AddGlobalUPNSuffix");
            string path = string.Format("LDAP://{0}/RootDSE", RootDomain);
            DirectoryEntry rootDSE = GetADObject(path);
            string contextPath = GetADObjectProperty(rootDSE, "ConfigurationNamingContext").ToString();
            DirectoryEntry partitions = GetADObject("LDAP://cn=Partitions," + contextPath);
            partitions.Properties["uPNSuffixes"].Add(name);
            partitions.CommitChanges();
            partitions.Close();
            rootDSE.Close();
            ExchangeLog.LogEnd("AddGlobalUPNSuffix");
        }*/

        internal string GetNETBIOSDomainName()
        {
            ExchangeLog.LogStart("GetNETBIOSDomainName");
            string ret = string.Empty;

            string path = string.Format("LDAP://{0}/RootDSE", RootDomain);
            DirectoryEntry rootDSE = GetADObject(path);
            string contextPath = GetADObjectProperty(rootDSE, "ConfigurationNamingContext").ToString();
            string defaultContext = GetADObjectProperty(rootDSE, "defaultNamingContext").ToString();
            DirectoryEntry partitions = GetADObject("LDAP://cn=Partitions," + contextPath);

            DirectorySearcher searcher = new DirectorySearcher();
            searcher.SearchRoot = partitions;
            searcher.Filter = string.Format("(&(objectCategory=crossRef)(nCName={0}))", defaultContext);
            searcher.SearchScope = SearchScope.OneLevel;

            //find the first instance
            SearchResult result = searcher.FindOne();
            if (result != null)
            {
                DirectoryEntry partition = GetADObject(result.Path);
                ret = GetADObjectProperty(partition, "nETBIOSName").ToString();
                partition.Close();
            }
            partitions.Close();
            rootDSE.Close();
            ExchangeLog.LogEnd("GetNETBIOSDomainName");
            return ret;
        }

        /*private void RemoveGlobalUPNSuffix(string name)
        {
            ExchangeLog.LogStart("RemoveGlobalUPNSuffix");
            string path = string.Format("LDAP://{0}/RootDSE", RootDomain);
            DirectoryEntry rootDSE = GetADObject(path);
            string contextPath = GetADObjectProperty(rootDSE, "ConfigurationNamingContext").ToString();
            DirectoryEntry partitions = GetADObject("LDAP://cn=Partitions," + contextPath);
            if (partitions.Properties["uPNSuffixes"].Contains(name))
            {
                partitions.Properties["uPNSuffixes"].Remove(name);
                partitions.CommitChanges();
            }
            partitions.Close();
            rootDSE.Close();
            ExchangeLog.LogEnd("RemoveGlobalUPNSuffix");
        }*/


        #endregion

        #region PowerShell integration
        /*private Collection<PSObject> ExecuteShellCommand2(Runspace runSpace, string cmd)
		{
			ExchangeLog.LogStart("ExecuteShellCommand");
			RunspaceInvoke invoker = new RunspaceInvoke(runSpace);
			System.Collections.IList errors = null;
			Collection<PSObject> results = invoker.Invoke(cmd, null, out errors);
			if (errors != null && errors.Count > 0)
			{
				foreach (PSObject err in errors)
				{
					string errorMessage = string.Format("Invoke error: {0}", err.ToString());
					ExchangeLog.LogError(errorMessage, null);
				}
			}
			invoker = null;
			ExchangeLog.LogEnd("ExecuteShellCommand");
			return results;
		}*/

        private static RunspaceConfiguration runspaceConfiguration = null;
        private static string ExchangePath = null;

        internal static string GetExchangePath()
        {
            if (string.IsNullOrEmpty(ExchangePath))
            {
                RegistryKey root = Registry.LocalMachine;
                RegistryKey rk = root.OpenSubKey(ExchangeRegistryPath);
                if (rk != null)
                {
                    string value = (string)rk.GetValue("MsiInstallPath", null);
                    rk.Close();
                    if (!string.IsNullOrEmpty(value))
                        ExchangePath = Path.Combine(value, "bin");
                }
            }
            return ExchangePath;
        }

        private static Assembly ResolveExchangeAssembly(object p, ResolveEventArgs args)
        {
            //Add path for the Exchange 2007 DLLs
            if (args.Name.Contains("Microsoft.Exchange"))
            {
                string exchangePath = GetExchangePath();
                if (string.IsNullOrEmpty(exchangePath))
                    return null;

                string path = Path.Combine(exchangePath, args.Name.Split(',')[0] + ".dll");
                if (!File.Exists(path))
                    return null;

                ExchangeLog.DebugInfo("Resolved assembly: {0}", path);
                return Assembly.LoadFrom(path);
            }
            else
            {
                return null;
            }
        }

        internal virtual Runspace OpenRunspace()
        {
            ExchangeLog.LogStart("OpenRunspace");

            if (runspaceConfiguration == null)
            {
                runspaceConfiguration = RunspaceConfiguration.Create();
                PSSnapInException exception = null;

                PSSnapInInfo info = runspaceConfiguration.AddPSSnapIn(ExchangeSnapInName, out exception);

                if (exception != null)
                {
                    ExchangeLog.LogWarning("SnapIn error", exception);
                }
            }
            Runspace runSpace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            //
            runSpace.Open();
            //
            runSpace.SessionStateProxy.SetVariable("ConfirmPreference", "none");
            ExchangeLog.LogEnd("OpenRunspace");
            return runSpace;
        }

        internal void CloseRunspace(Runspace runspace)
        {
            try
            {
                if (runspace != null && runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    runspace.Close();
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("Runspace error", ex);
            }
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd)
        {
            return ExecuteShellCommand(runSpace, cmd, false);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, bool useDomainController)
        {
            object[] errors;
            return ExecuteShellCommand(runSpace, cmd, useDomainController, out errors);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, out object[] errors)
        {
            return ExecuteShellCommand(runSpace, cmd, true, out errors);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, bool useDomainController, out object[] errors)
        {
            ExchangeLog.LogStart("ExecuteShellCommand");
            List<object> errorList = new List<object>();

            if (useDomainController)
            {
                CommandParameter dc = new CommandParameter("DomainController", PrimaryDomainController);
                if (!cmd.Parameters.Contains(dc))
                {
                    cmd.Parameters.Add(dc);
                }
            }

            ExchangeLog.DebugCommand(cmd);
            Collection<PSObject> results = null;
            // Create a pipeline
            Pipeline pipeLine = runSpace.CreatePipeline();
            using (pipeLine)
            {
                // Add the command
                pipeLine.Commands.Add(cmd);
                // Execute the pipeline and save the objects returned.
                results = pipeLine.Invoke();

                // Log out any errors in the pipeline execution
                // NOTE: These errors are NOT thrown as exceptions! 
                // Be sure to check this to ensure that no errors 
                // happened while executing the command.
                if (pipeLine.Error != null && pipeLine.Error.Count > 0)
                {
                    foreach (object item in pipeLine.Error.ReadToEnd())
                    {
                        errorList.Add(item);
                        string errorMessage = string.Format("Invoke error: {0}", item);
                        ExchangeLog.LogWarning(errorMessage);
                    }
                }
            }
            pipeLine = null;
            errors = errorList.ToArray();
            ExchangeLog.LogEnd("ExecuteShellCommand");
            return results;
        }

        /// <summary>
        /// Returns the distinguished name of the object from the shell execution result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal string GetResultObjectDN(Collection<PSObject> result)
        {
            ExchangeLog.LogStart("GetResultObjectDN");
            if (result == null)
                throw new ArgumentNullException("result", "Execution result is not specified");

            if (result.Count < 1)
                throw new ArgumentException("Execution result does not contain any object");

            if (result.Count > 1)
                throw new ArgumentException("Execution result contains more than one object");

            PSMemberInfo info = result[0].Members["DistinguishedName"];
            if (info == null)
                throw new ArgumentException("Execution result does not contain DistinguishedName property", "result");

            string ret = info.Value.ToString();
            ExchangeLog.LogEnd("GetResultObjectDN");
            return ret;
        }

        /// <summary>
        /// Checks the object from the shell execution result.
        /// </summary>
        /// <param name="result"></param>
        /// <returns>Distinguished name of the object if object exists or null otherwise.</returns>
        internal string CheckResultObjectDN(Collection<PSObject> result)
        {
            ExchangeLog.LogStart("CheckResultObjectDN");

            if (result == null)
                return null;

            if (result.Count < 1)
                return null;

            PSMemberInfo info = result[0].Members["DistinguishedName"];
            if (info == null)
                throw new ArgumentException("Execution result does not contain DistinguishedName property", "result");

            string ret = info.Value.ToString();
            ExchangeLog.LogEnd("CheckResultObjectDN");
            return ret;
        }

        /// <summary>
        /// Returns the identity of the object from the shell execution result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal string GetResultObjectIdentity(Collection<PSObject> result)
        {
            ExchangeLog.LogStart("GetResultObjectIdentity");
            if (result == null)
                throw new ArgumentNullException("result", "Execution result is not specified");

            if (result.Count < 1)
                throw new ArgumentException("Execution result is empty", "result");

            if (result.Count > 1)
                throw new ArgumentException("Execution result contains more than one object", "result");

            PSMemberInfo info = result[0].Members["Identity"];
            if (info == null)
                throw new ArgumentException("Execution result does not contain Identity property", "result");

            string ret = info.Value.ToString();
            ExchangeLog.LogEnd("GetResultObjectIdentity");
            return ret;
        }

        /// <summary>
        /// Returns the identity of the PS object 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal string GetPSObjectIdentity(PSObject obj)
        {
            ExchangeLog.LogStart("GetPSObjectIdentity");
            if (obj == null)
                throw new ArgumentNullException("obj", "PSObject is not specified");


            PSMemberInfo info = obj.Members["Identity"];
            if (info == null)
                throw new ArgumentException("PSObject does not contain Identity property", "obj");

            string ret = info.Value.ToString();
            ExchangeLog.LogEnd("GetPSObjectIdentity");
            return ret;
        }

        internal object GetPSObjectProperty(PSObject obj, string name)
        {
            return obj.Members[name].Value;
        }

        #endregion

        #region Storage
        internal virtual string CreateStorageGroup(Runspace runSpace, string name, string server)
        {
            ExchangeLog.LogStart("CreateStorageGroup");

            if (name.StartsWith(server + "\\"))
                name = name.Substring(server.Length + 1);

            string id = string.Format("{0}\\{1}", server, name);

            Command cmd = new Command("Get-StorageGroup");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                id = GetResultObjectIdentity(result);
            }
            else
            {
                cmd = new Command("New-StorageGroup");
                cmd.Parameters.Add("Name", name);
                cmd.Parameters.Add("Server", server);

                result = ExecuteShellCommand(runSpace, cmd);
                id = GetResultObjectIdentity(result);
            }
            ExchangeLog.LogEnd("CreateStorageGroup");
            return id;
        }

        internal virtual string CreateMailboxDatabase(Runspace runSpace, string name, string storageGroup)
        {
            ExchangeLog.LogStart("CreateMailboxDatabase");

            string id = string.Format("{0}\\{1}", storageGroup, name);
            Command cmd = new Command("Get-MailboxDatabase");
            cmd.Parameters.Add("Identity", id);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            if (result != null && result.Count > 0)
            {
                id = GetResultObjectIdentity(result);
            }
            else
            {
                cmd = new Command("New-MailboxDatabase");
                cmd.Parameters.Add("Name", name);
                cmd.Parameters.Add("StorageGroup", storageGroup);

                result = ExecuteShellCommand(runSpace, cmd);
                id = GetResultObjectIdentity(result);

                cmd = new Command("Set-MailboxDatabase");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("IssueWarningQuota", Unlimited<ByteQuantifiedSize>.UnlimitedValue);
                cmd.Parameters.Add("ProhibitSendQuota", Unlimited<ByteQuantifiedSize>.UnlimitedValue);
                cmd.Parameters.Add("ProhibitSendReceiveQuota", Unlimited<ByteQuantifiedSize>.UnlimitedValue);
                cmd.Parameters.Add("DeletedItemRetention", KeepDeletedItemsDays);
                cmd.Parameters.Add("MailboxRetention", KeepDeletedMailboxesDays);
                ExecuteShellCommand(runSpace, cmd);

                cmd = new Command("Mount-Database");
                cmd.Parameters.Add("Identity", id);
                ExecuteShellCommand(runSpace, cmd);
            }

            ExchangeLog.LogEnd("CreateMailboxDatabase");

            return id;
        }
        #endregion

        #region Domains


        private string[] GetAuthoritativeDomainsInternal()
        {
            ExchangeLog.LogStart("GetAuthoritativeDomainsInternal");

            Runspace runSpace = null;
            List<string> domains = new List<string>();
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-AcceptedDomain");

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                foreach (PSObject current in result)
                {
                    domains.Add(GetPSObjectProperty(current, "DomainName").ToString());
                }

            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("GetAuthoritativeDomainsInternal", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("GetAuthoritativeDomainsInternal");
            return domains.ToArray();

        }



        /// <summary>
        /// Creates Authoritative Domain on Hub Transport Server
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private void CreateAuthoritativeDomainInternal(string domain)
        {
            ExchangeLog.LogStart("CreateAuthoritativeDomainInternal");

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                CreateAcceptedDomain(runSpace, domain);
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("CreateAuthoritativeDomainInternal", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CreateAuthoritativeDomainInternal");
        }

        private void ChangeAcceptedDomainTypeInternal(string domainName, ExchangeAcceptedDomainType domainType)
        {
            ExchangeLog.LogStart("ChangeAcceptedDomainType");

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                SetAcceptedDomainType(runSpace, domainName, domainType);
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("ChangeAcceptedDomainType", ex);
                throw;
            }
            finally
            {

                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("ChangeAcceptedDomainType");
        }

        private void DeleteAcceptedDomain(Runspace runSpace, string domainName)
        {
            ExchangeLog.LogStart("DeleteAcceptedDomain");

            bool bCloseRunSpace = false;

            try
            {
                if (runSpace == null)
                {
                    bCloseRunSpace = true;
                    runSpace = OpenRunspace();
                }

                RemoveAcceptedDomain(runSpace, domainName);
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("DeleteAcceptedDomain", ex);
                throw;
            }
            finally
            {

                if (bCloseRunSpace) CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("DeleteAcceptedDomain");
        }

        /// <summary>
        /// Deletes Authoritative Domain on Hub Transport Server
        /// </summary>
        /// <param name="domain"></param>
        private void DeleteAuthoritativeDomainInternal(string domain)
        {
            ExchangeLog.LogStart("DeleteDomainInternal");
            //Delete accepted domain
            DeleteAcceptedDomain(null, domain);
            ExchangeLog.LogEnd("DeleteDomainInternal");
        }

        private string CreateAcceptedDomain(Runspace runSpace, string name)
        {
            ExchangeLog.LogStart("CreateAcceptedDomain");

            Command cmd = new Command("New-AcceptedDomain");
            cmd.Parameters.Add("Name", name);
            cmd.Parameters.Add("DomainName", name);
            cmd.Parameters.Add("DomainType", "Authoritative");

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = GetResultObjectDN(result);

            ExchangeLog.LogEnd("CreateAcceptedDomain");

            return id;
        }

        private void RemoveAcceptedDomain(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("RemoveAcceptedDomain");
            Command cmd = new Command("Remove-AcceptedDomain");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("RemoveAcceptedDomain");
        }

        private void SetAcceptedDomainType(Runspace runSpace, string id, ExchangeAcceptedDomainType domainType)
        {
            ExchangeLog.LogStart("SetAcceptedDomainType");
            Command cmd = new Command("Set-AcceptedDomain");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("DomainType", domainType.ToString());
            cmd.Parameters.Add("AddressBookEnabled", !(domainType == ExchangeAcceptedDomainType.InternalRelay));
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("SetAcceptedDomainType");
        }

        #endregion

        #region ActiveSync
        private void CreateOrganizationActiveSyncPolicyInternal(string organizationId)
        {
            ExchangeLog.LogStart("CreateOrganizationActiveSyncPolicyInternal");
            ExchangeLog.DebugInfo("  Organization Id: {0}", organizationId);
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                //create ActiveSync policy
                CreateActiveSyncPolicy(runSpace, organizationId);
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("CreateOrganizationActiveSyncPolicyInternal");
        }

        internal string CreateActiveSyncPolicy(Runspace runSpace, string organizationId)
        {
            ExchangeLog.LogStart("CreateActiveSyncPolicy");
            Command cmd = new Command("New-ActiveSyncMailboxPolicy");
            cmd.Parameters.Add("Name", organizationId);
            cmd.Parameters.Add("AllowNonProvisionableDevices", true);
            cmd.Parameters.Add("Confirm", false);
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
            string id = GetResultObjectIdentity(result);
            ExchangeLog.LogEnd("CreateActiveSyncPolicy");
            return id;
        }

        internal void DeleteActiveSyncPolicy(Runspace runSpace, string id)
        {
            ExchangeLog.LogStart("DeleteActiveSyncPolicy");
            Command cmd = new Command("Remove-ActiveSyncMailboxPolicy");
            cmd.Parameters.Add("Identity", id);
            cmd.Parameters.Add("Confirm", false);
            ExecuteShellCommand(runSpace, cmd);
            ExchangeLog.LogEnd("DeleteActiveSyncPolicy");
        }

        private ExchangeActiveSyncPolicy GetActiveSyncPolicyInternal(string id)
        {
            ExchangeLog.LogStart("GetActiveSyncPolicyInternal");
            ExchangeLog.DebugInfo("Id: {0}", id);
            ExchangeActiveSyncPolicy info = null;

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-ActiveSyncMailboxPolicy");
                cmd.Parameters.Add("Identity", id);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);

                if (result != null && result.Count > 0)
                {
                    PSObject policy = result[0];
                    info = new ExchangeActiveSyncPolicy();
                    info.AllowNonProvisionableDevices = (bool)GetPSObjectProperty(policy, "AllowNonProvisionableDevices");
                    info.AttachmentsEnabled = (bool)GetPSObjectProperty(policy, "AttachmentsEnabled");
                    info.MaxAttachmentSizeKB =
                        ConvertUnlimitedToKB((Unlimited<ByteQuantifiedSize>)GetPSObjectProperty(policy, "MaxAttachmentSize"));
                    info.UNCAccessEnabled = (bool)GetPSObjectProperty(policy, "UNCAccessEnabled");
                    info.WSSAccessEnabled = (bool)GetPSObjectProperty(policy, "WSSAccessEnabled");

                    info.DevicePasswordEnabled = (bool)GetPSObjectProperty(policy, "DevicePasswordEnabled");
                    info.AlphanumericPasswordRequired = (bool)GetPSObjectProperty(policy, "AlphanumericDevicePasswordRequired");
                    info.PasswordRecoveryEnabled = (bool)GetPSObjectProperty(policy, "PasswordRecoveryEnabled");
                    info.DeviceEncryptionEnabled = (bool)GetPSObjectProperty(policy, "DeviceEncryptionEnabled");
                    info.AllowSimplePassword = (bool)GetPSObjectProperty(policy, "AllowSimpleDevicePassword");

                    info.MaxPasswordFailedAttempts =
                        ConvertUnlimitedToInt32((Unlimited<int>)GetPSObjectProperty(policy, "MaxDevicePasswordFailedAttempts"));
                    int? passwordLength = (int?)GetPSObjectProperty(policy, "MinDevicePasswordLength");
                    info.MinPasswordLength = ConvertNullableToInt32(passwordLength);

                    info.InactivityLockMin = ConvertUnlimitedToMinutes((Unlimited<EnhancedTimeSpan>)GetPSObjectProperty(policy, "MaxInactivityTimeDeviceLock"));
                    info.PasswordExpirationDays = ConvertUnlimitedTimeSpanToDays((Unlimited<EnhancedTimeSpan>)GetPSObjectProperty(policy, "DevicePasswordExpiration"));
                    info.PasswordHistory = (int)GetPSObjectProperty(policy, "DevicePasswordHistory");


                    info.RefreshInterval = ConvertUnlimitedToHours((Unlimited<EnhancedTimeSpan>)GetPSObjectProperty(policy, "DevicePolicyRefreshInterval"));
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetActiveSyncPolicyInternal");
            return info;
        }

        private void SetActiveSyncPolicyInternal(
            string id,
            bool allowNonProvisionableDevices,
            bool attachmentsEnabled,
            int maxAttachmentSizeKB,
            bool uncAccessEnabled,
            bool wssAccessEnabled,
            bool devicePasswordEnabled,
            bool alphanumericPasswordRequired,
            bool passwordRecoveryEnabled,
            bool deviceEncryptionEnabled,
            bool allowSimplePassword,
            int maxPasswordFailedAttempts,
            int minPasswordLength,
            int inactivityLockMin,
            int passwordExpirationDays,
            int passwordHistory,
            int refreshInterval)
        {
            ExchangeLog.LogStart("SetActiveSyncPolicyInternal");
            ExchangeLog.DebugInfo("Id: {0}", id);

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Set-ActiveSyncMailboxPolicy");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("AllowNonProvisionableDevices", allowNonProvisionableDevices);
                cmd.Parameters.Add("AttachmentsEnabled", attachmentsEnabled);
                cmd.Parameters.Add("MaxAttachmentSize", ConvertKBToUnlimited(maxAttachmentSizeKB));
                cmd.Parameters.Add("UNCAccessEnabled", uncAccessEnabled);
                cmd.Parameters.Add("WSSAccessEnabled", wssAccessEnabled);

                cmd.Parameters.Add("DevicePasswordEnabled", devicePasswordEnabled);
                cmd.Parameters.Add("AlphanumericDevicePasswordRequired", alphanumericPasswordRequired);
                cmd.Parameters.Add("PasswordRecoveryEnabled", passwordRecoveryEnabled);
                cmd.Parameters.Add("DeviceEncryptionEnabled", deviceEncryptionEnabled);
                cmd.Parameters.Add("AllowSimpleDevicePassword", allowSimplePassword);

                Unlimited<int> attempts = ConvertInt32ToUnlimited(maxPasswordFailedAttempts);
                cmd.Parameters.Add("MaxDevicePasswordFailedAttempts", attempts);

                int? passwordLength = ConvertInt32ToNullable(minPasswordLength);
                cmd.Parameters.Add("MinDevicePasswordLength", passwordLength);


                Unlimited<EnhancedTimeSpan> inactivityLock = ConvertMinutesToUnlimitedTimeSpan(inactivityLockMin);
                cmd.Parameters.Add("MaxInactivityTimeDeviceLock", inactivityLock);

                Unlimited<EnhancedTimeSpan> passwordExpiration = ConvertDaysToUnlimitedTimeSpan(passwordExpirationDays);
                cmd.Parameters.Add("DevicePasswordExpiration", passwordExpiration);

                cmd.Parameters.Add("DevicePasswordHistory", passwordHistory);

                Unlimited<EnhancedTimeSpan> refInter = ConvertHoursToUnlimitedTimeSpan(refreshInterval);
                cmd.Parameters.Add("DevicePolicyRefreshInterval", refInter);

                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetActiveSyncPolicyInternal");
        }
        #endregion

        #region Mobile devices

        private ExchangeMobileDevice[] GetMobileDevicesInternal(string accountName)
        {
            ExchangeLog.LogStart("GetMobileDevicesInternal");
            ExchangeLog.DebugInfo("Account name: {0}", accountName);

            List<ExchangeMobileDevice> devices = new List<ExchangeMobileDevice>();
            ExchangeMobileDevice device = null;

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-ActiveSyncDeviceStatistics");
                cmd.Parameters.Add("Mailbox", accountName);

                Collection<PSObject> result = null;
                try
                {
                    result = ExecuteShellCommand(runSpace, cmd);
                }
                catch (Exception)
                {
                }

                if (result != null)
                {
                    foreach (PSObject obj in result)
                    {
                        device = GetMobileDeviceObject(obj);
                        devices.Add(device);
                    }
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMobileDevicesInternal");
            return devices.ToArray();
        }

        private ExchangeMobileDevice GetMobileDeviceObject(PSObject obj)
        {
            ExchangeMobileDevice device = new ExchangeMobileDevice();
            device.Id = GetPSObjectIdentity(obj);
            device.FirstSyncTime = ConvertNullableToDateTime((DateTime?)GetPSObjectProperty(obj, "FirstSyncTime"));
            device.LastPolicyUpdateTime = ConvertNullableToDateTime((DateTime?)GetPSObjectProperty(obj, "LastPolicyUpdateTime"));
            device.LastSyncAttemptTime = ConvertNullableToDateTime((DateTime?)GetPSObjectProperty(obj, "LastSyncAttemptTime"));
            device.LastSuccessSync = ConvertNullableToDateTime((DateTime?)GetPSObjectProperty(obj, "LastSuccessSync"));
            device.DeviceType = (string)GetPSObjectProperty(obj, "DeviceType");
            device.DeviceID = (string)GetPSObjectProperty(obj, "DeviceID");
            device.DeviceUserAgent = (string)GetPSObjectProperty(obj, "DeviceUserAgent");
            DateTime? wipeSentTime = (DateTime?)GetPSObjectProperty(obj, "DeviceWipeSentTime");
            device.DeviceWipeSentTime = ConvertNullableToDateTime(wipeSentTime);
            DateTime? wipeRequestTime = (DateTime?)GetPSObjectProperty(obj, "DeviceWipeRequestTime");
            device.DeviceWipeRequestTime = ConvertNullableToDateTime(wipeRequestTime);
            DateTime? wipeAckTime = (DateTime?)GetPSObjectProperty(obj, "DeviceWipeAckTime");
            device.DeviceWipeAckTime = ConvertNullableToDateTime(wipeAckTime);
            device.LastPingHeartbeat = ConvertNullableToInt32((UInt32?)GetPSObjectProperty(obj, "LastPingHeartbeat"));
            device.RecoveryPassword = (string)GetPSObjectProperty(obj, "RecoveryPassword");
            device.DeviceModel = (string)GetPSObjectProperty(obj, "DeviceModel");
            device.DeviceIMEI = (string)GetPSObjectProperty(obj, "DeviceIMEI");
            device.DeviceFriendlyName = (string)GetPSObjectProperty(obj, "DeviceFriendlyName");
            device.DeviceOS = (string)GetPSObjectProperty(obj, "DeviceOS");
            device.DeviceOSLanguage = (string)GetPSObjectProperty(obj, "DeviceOSLanguage");
            device.DevicePhoneNumber = (string)GetPSObjectProperty(obj, "DevicePhoneNumber");
            //status
            if (wipeAckTime.HasValue)
            {
                //green
                device.Status = MobileDeviceStatus.WipeSuccessful;
            }
            else
            {
                if (wipeRequestTime.HasValue || wipeSentTime.HasValue)
                {
                    //red
                    device.Status = MobileDeviceStatus.PendingWipe;
                }
                else
                {
                    //black
                    device.Status = MobileDeviceStatus.OK;
                }
            }

            return device;
        }

        private ExchangeMobileDevice GetMobileDeviceInternal(string id)
        {
            ExchangeLog.LogStart("GetMobileDeviceInternal");
            ExchangeMobileDevice device = null;
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-ActiveSyncDeviceStatistics");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("ShowRecoveryPassword", true);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd);
                if (result != null && result.Count > 0)
                {
                    device = GetMobileDeviceObject(result[0]);
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("GetMobileDeviceInternal");
            return device;
        }

        private void WipeDataFromDeviceInternal(string id)
        {
            ExchangeLog.LogStart("WipeDataFromDeviceInternal");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Clear-ActiveSyncDevice");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("Confirm", false);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("WipeDataFromDeviceInternal");
        }

        private void CancelRemoteWipeRequestInternal(string id)
        {
            ExchangeLog.LogStart("CancelRemoteWipeRequestInternal");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Clear-ActiveSyncDevice");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("Cancel", true);
                cmd.Parameters.Add("Confirm", false);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("CancelRemoteWipeRequestInternal");
        }


        internal void RemoveDevicesInternal(Runspace runSpace, string accountName)
        {
            ExchangeLog.LogStart("RemoveDevicesInternal");
            ExchangeLog.DebugInfo("Account name: {0}", accountName);

            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-ActiveSyncDeviceStatistics");
                cmd.Parameters.Add("Mailbox", accountName);

                Collection<PSObject> result = null;
                try
                {
                    result = ExecuteShellCommand(runSpace, cmd);
                }
                catch (Exception)
                {
                }

                if (result != null)
                {
                    foreach (PSObject obj in result)
                    {
                        ExchangeMobileDevice device = GetMobileDeviceObject(obj);

                        cmd = new Command("Remove-ActiveSyncDevice");
                        cmd.Parameters.Add("Identity", device.DeviceID);
                        cmd.Parameters.Add("Confirm", false);
                        ExecuteShellCommand(runSpace, cmd);
                    }
                }
            }
            finally
            {

            }
            ExchangeLog.LogEnd("RemoveDevicesInternal");
        }


        private void RemoveDeviceInternal(string id)
        {
            ExchangeLog.LogStart("RemoveDeviceInternal");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Remove-ActiveSyncDevice");
                cmd.Parameters.Add("Identity", id);
                cmd.Parameters.Add("Confirm", false);
                ExecuteShellCommand(runSpace, cmd);
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            ExchangeLog.LogEnd("RemoveDeviceInternal");
        }
        #endregion

        #region Convert Utils
        internal int ConvertUnlimitedToInt32(Unlimited<int> value)
        {
            int ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = value.Value;
            }
            return ret;
        }

        internal Unlimited<int> ConvertInt32ToUnlimited(int value)
        {
            if (value == -1)
                return Unlimited<int>.UnlimitedValue;
            else
            {
                Unlimited<int> ret = new Unlimited<int>();
                ret.Value = value;
                return ret;
            }
        }

        internal long ConvertUnlimitedToBytes(Unlimited<ByteQuantifiedSize> value)
        {
            long ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = Convert.ToInt64(value.Value.ToBytes());
            }
            return ret;
        }

        internal int ConvertUnlimitedToKB(Unlimited<ByteQuantifiedSize> value)
        {
            int ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = Convert.ToInt32(value.Value.ToKB());
            }
            return ret;
        }

        internal int ConvertUnlimitedToMB(Unlimited<ByteQuantifiedSize> value)
        {
            int ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = Convert.ToInt32(value.Value.ToMB());
            }
            return ret;
        }


        internal int ConvertUnlimitedToHours(Unlimited<EnhancedTimeSpan> value)
        {
            int ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = Convert.ToInt32(value.Value.TotalHours);
            }
            return ret;
        }

        internal int ConvertUnlimitedToMinutes(Unlimited<EnhancedTimeSpan> value)
        {
            int ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = Convert.ToInt32(value.Value.TotalMinutes);
            }
            return ret;
        }

        internal Unlimited<EnhancedTimeSpan> ConvertHoursToUnlimitedTimeSpan(int value)
        {
            if (value == -1)
                return Unlimited<EnhancedTimeSpan>.UnlimitedValue;
            else
            {
                Unlimited<EnhancedTimeSpan> ret = new Unlimited<EnhancedTimeSpan>();
                ret.Value = EnhancedTimeSpan.FromHours(Convert.ToDouble(value));
                return ret;
            }

        }

        internal Unlimited<EnhancedTimeSpan> ConvertMinutesToUnlimitedTimeSpan(int value)
        {
            if (value == -1)
                return Unlimited<EnhancedTimeSpan>.UnlimitedValue;
            else
            {
                Unlimited<EnhancedTimeSpan> ret = new Unlimited<EnhancedTimeSpan>();
                ret.Value = EnhancedTimeSpan.FromMinutes(Convert.ToDouble(value));
                return ret;
            }
        }

        internal Unlimited<EnhancedTimeSpan> ConvertDaysToUnlimitedTimeSpan(int value)
        {
            if (value == -1)
                return Unlimited<EnhancedTimeSpan>.UnlimitedValue;
            else
            {
                Unlimited<EnhancedTimeSpan> ret = new Unlimited<EnhancedTimeSpan>();
                ret.Value = EnhancedTimeSpan.FromDays(Convert.ToDouble(value));
                return ret;
            }
        }


        internal int ConvertUnlimitedTimeSpanToDays(Unlimited<EnhancedTimeSpan> value)
        {
            int ret = 0;
            if (value.IsUnlimited)
            {
                ret = -1;
            }
            else
            {
                ret = value.Value.Days;
            }
            return ret;
        }


        internal int ConvertNullableToInt32<T>(Nullable<T> value) where T : struct
        {
            int ret = 0;
            if (value.HasValue)
            {
                ret = Convert.ToInt32(value.Value);
            }
            return ret;
        }


        internal int? ConvertInt32ToNullable(int value)
        {
            int? ret = null;
            if (value != 0)
            {
                ret = new int?(value);
            }
            return ret;
        }



        internal DateTime ConvertNullableToDateTime(DateTime? value)
        {
            DateTime ret = DateTime.MinValue;
            if (value.HasValue)
            {
                ret = value.Value;
            }
            return ret;
        }

        internal bool ConvertNullableToBoolean(bool? value)
        {
            bool ret = false;
            if (value.HasValue)
            {
                ret = value.Value;
            }
            return ret;
        }

        internal int ConvertEnhancedTimeSpanToDays(EnhancedTimeSpan value)
        {
            return value.Days;
        }

        internal EnhancedTimeSpan ConvertDaysToEnhancedTimeSpan(int days)
        {
            return EnhancedTimeSpan.FromDays(Convert.ToDouble(days));
        }

        internal Unlimited<ByteQuantifiedSize> ConvertKBToUnlimited(int kb)
        {
            if (kb == -1)
                return Unlimited<ByteQuantifiedSize>.UnlimitedValue;
            else
            {
                Unlimited<ByteQuantifiedSize> ret = new Unlimited<ByteQuantifiedSize>();
                ret.Value = ByteQuantifiedSize.FromKB(Convert.ToUInt64(kb));
                return ret;
            }
        }

        internal Unlimited<ByteQuantifiedSize> ConvertKBToUnlimited(long kb)
        {
            if (kb == -1)
                return Unlimited<ByteQuantifiedSize>.UnlimitedValue;
            else
            {
                Unlimited<ByteQuantifiedSize> ret = new Unlimited<ByteQuantifiedSize>();
                ret.Value = ByteQuantifiedSize.FromKB(Convert.ToUInt64(kb));
                return ret;
            }
        }


        internal string ProxyAddressToString(ProxyAddress proxyAddress)
        {
            string ret = null;
            if (proxyAddress != null)
                ret = proxyAddress.AddressString;
            return ret;
        }

        internal string SmtpAddressToString(SmtpAddress smtpAddress)
        {
            string ret = null;
            if (smtpAddress != null)
                ret = smtpAddress.ToString();
            return ret;
        }

        internal string CountryInfoToString(CountryInfo countryInfo)
        {
            string ret = null;
            if (countryInfo != null)
                ret = countryInfo.Name;
            return ret;
        }

        internal CountryInfo ParseCountryInfo(string country)
        {
            CountryInfo ret = null;
            if (!string.IsNullOrEmpty(country))
            {
                ret = CountryInfo.Parse(country);
            }
            return ret;
        }


        internal string ObjToString(object obj)
        {
            string ret = null;
            if (obj != null)
                ret = obj.ToString();
            return ret;
        }


        #endregion

        #region Utils
        private object GetObjectPropertyValue(object obj, string property)
        {
            PropertyInfo pinfo = obj.GetType().GetProperty(property);
            return pinfo.GetValue(obj, null);
        }

        private object GetObjectIndexerValue(object obj, object index)
        {
            Type t = obj.GetType();
            object ret = t.InvokeMember("Item", BindingFlags.GetProperty, null, obj, new object[] { index });
            return ret;
        }

        internal string GetServerName()
        {
            string ret = null;
            if (!string.IsNullOrEmpty(MailboxCluster))
                ret = MailboxCluster;
            else
                ret = System.Environment.MachineName;
            return ret;
        }

        public override bool IsInstalled()
        {
            int value = 0;
            RegistryKey root = Registry.LocalMachine;
            RegistryKey rk = root.OpenSubKey(ExchangeRegistryPath);
            if (rk != null)
            {
                value = (int)rk.GetValue("MsiProductMajor", null);
                rk.Close();
            }

            return value == 8;
        }

        public Version GetExchangeVersion()
        {
            int major = 0;
            int minor = 0;
            RegistryKey root = Registry.LocalMachine;
            RegistryKey rk = root.OpenSubKey(ExchangeRegistryPath);
            if (rk != null)
            {
                major = (int)rk.GetValue("MsiProductMajor", 0);
                minor = (int)rk.GetValue("MsiProductMinor", 0);
                rk.Close();
            }

            return new Version(major, minor);
        }
        #endregion

        #region Transactions

        internal ExchangeTransaction StartTransaction()
        {
            return new ExchangeTransaction();
        }

        internal void RollbackTransaction(ExchangeTransaction transaction)
        {
            ExchangeLog.LogStart("RollbackTransaction");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();


                for (int i = transaction.Actions.Count - 1; i > -1; i--)
                {
                    //reverse order
                    try
                    {
                        RollbackAction(transaction.Actions[i], runSpace);
                    }
                    catch (Exception ex)
                    {
                        ExchangeLog.LogError("Rollback error", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.LogError("Rollback error", ex);
            }
            finally
            {

                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("RollbackTransaction");
        }

        private void RollbackAction(TransactionAction action, Runspace runspace)
        {
            ExchangeLog.LogInfo("Rollback action: {0}", action.ActionType);
            switch (action.ActionType)
            {
                case TransactionAction.TransactionActionTypes.CreateOrganizationUnit:
                    DeleteADObject(action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateDistributionGroup:
                    RemoveDistributionGroup(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.EnableDistributionGroup:
                    DisableMailSecurityDistributionGroup(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateGlobalAddressList:
                    DeleteGlobalAddressList(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateAddressList:
                    DeleteAddressList(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateAddressBookPolicy:
                    DeleteAddressBookPolicy(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateOfflineAddressBook:
                    DeleteOfflineAddressBook(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateActiveSyncPolicy:
                    DeleteActiveSyncPolicy(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateAcceptedDomain:
                    RemoveAcceptedDomain(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.AddUPNSuffix:
                    RemoveUPNSuffix(action.Id, action.Suffix);
                    break;
                case TransactionAction.TransactionActionTypes.CreateMailbox:
                    RemoveMailbox(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.EnableMailbox:
                    DisableMailbox(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreateContact:
                    RemoveContact(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.CreatePublicFolder:
                    RemovePublicFolder(runspace, action.Id);
                    break;
                case TransactionAction.TransactionActionTypes.AddMailboxFullAccessPermission:
                    RemoveMailboxAccessPermission(runspace, action.Account, action.Id, "FullAccess");
                    break;
                case TransactionAction.TransactionActionTypes.AddSendAsPermission:
                    RemoveADPermission(runspace, action.Account, action.Id, null, "Send-as", null);
                    break;
                case TransactionAction.TransactionActionTypes.RemoveMailboxFullAccessPermission:
                    SetMailboxPermission(runspace, action.Account, action.Id, "FullAccess");
                    break;
                case TransactionAction.TransactionActionTypes.RemoveSendAsPermission:
                    SetExtendedRights(runspace, action.Account, action.Id, "Send-as");
                    break;
            }
        }
        #endregion

        #region Disclaimers

        private const string disclamerMemberPostfix = "_members";

        public int SetDisclaimer(string name, string text)
        {
            ExchangeLog.LogStart("SetDisclaimer");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd;

                bool distributionGroupExist = false;
                bool transportRuleExist = false;

                cmd = new Command("Get-DistributionGroup");
                cmd.Parameters.Add("Identity", name + disclamerMemberPostfix);
                Collection<PSObject> res = ExecuteShellCommand(runSpace, cmd);
                distributionGroupExist = (res.Count > 0);

                cmd = new Command("Get-TransportRule");
                cmd.Parameters.Add("Identity", name);
                res = ExecuteShellCommand(runSpace, cmd);
                transportRuleExist = (res.Count > 0);

                if (!distributionGroupExist)
                {
                    cmd = new Command("New-DistributionGroup");
                    cmd.Parameters.Add("Name", name + disclamerMemberPostfix);
                    ExecuteShellCommand(runSpace, cmd);
                }

                if (transportRuleExist)
                {
                    cmd = new Command("Set-TransportRule");
                    cmd.Parameters.Add("Identity", name);
                }
                else
                {
                    cmd = new Command("New-TransportRule");
                    cmd.Parameters.Add("Name", name);
                }
                cmd.Parameters.Add("FromMemberOf", name + disclamerMemberPostfix);
                cmd.Parameters.Add("Enabled", true);
                cmd.Parameters.Add("ApplyHtmlDisclaimerLocation", "Append");
                cmd.Parameters.Add("ApplyHtmlDisclaimerText", text);
                cmd.Parameters.Add("ApplyHtmlDisclaimerFallbackAction", "Wrap");
                ExecuteShellCommand(runSpace, cmd);

            }
            catch (Exception exc)
            {
                ExchangeLog.LogError(exc);
                return -1;
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetDisclaimer");
            return 0;
        }

        public int RemoveDisclaimer(string name)
        {
            ExchangeLog.LogStart("RemoveDisclaimer");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-DistributionGroupMember");
                cmd.Parameters.Add("Identity", name + disclamerMemberPostfix);
                Collection<PSObject> res = ExecuteShellCommand(runSpace, cmd);
                if (res.Count > 0)
                    return -1;

                cmd = new Command("Remove-TransportRule");
                cmd.Parameters.Add("Identity", name);
                cmd.Parameters.Add("Confirm", new SwitchParameter(false));
                ExecuteShellCommand(runSpace, cmd);

                cmd = new Command("Remove-DistributionGroup");
                cmd.Parameters.Add("Identity", name + disclamerMemberPostfix);
                cmd.Parameters.Add("Confirm", new SwitchParameter(false));
                ExecuteShellCommand(runSpace, cmd);

            }
            catch (Exception exc)
            {
                ExchangeLog.LogError(exc);
                return -1;
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("RemoveDisclaimer");
            return 0;
        }

        public int AddDisclamerMember(string name, string member)
        {
            ExchangeLog.LogStart("SetDisclamerMember");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Add-DistributionGroupMember");
                cmd.Parameters.Add("Identity", name + disclamerMemberPostfix);
                cmd.Parameters.Add("Member", member);
                ExecuteShellCommand(runSpace, cmd);

            }
            catch (Exception exc)
            {
                ExchangeLog.LogError(exc);
                return -1;
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("SetDisclamerMember");
            return 0;
        }

        public int RemoveDisclamerMember(string name, string member)
        {
            ExchangeLog.LogStart("RemoveDisclamerMember");
            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Remove-DistributionGroupMember");
                cmd.Parameters.Add("Identity", name + disclamerMemberPostfix);
                cmd.Parameters.Add("Member", member);
                ExecuteShellCommand(runSpace, cmd);
            }
            catch (Exception exc)
            {
                ExchangeLog.LogError(exc);
                return -1;
            }
            finally
            {
                CloseRunspace(runSpace);
            }
            ExchangeLog.LogEnd("RemoveDisclamerMember");
            return 0;
        }


        #endregion

        #region Archiving

        public virtual ResultObject ExportMailBox(string organizationId, string accountName, string storagePath)
        {
            // not implemented
            return null;
        }

        public virtual ResultObject SetMailBoxArchiving(string organizationId, string accountName, bool archive, long archiveQuotaKB, long archiveWarningQuotaKB, string RetentionPolicy)
        {
            // not implemented
            return null;
        }
        #endregion

        #region Retention policy

        public virtual ResultObject SetRetentionPolicyTag(string Identity, ExchangeRetentionPolicyTagType Type, int AgeLimitForRetention, ExchangeRetentionPolicyTagAction RetentionAction)
        {
            // not implemented
            return null;
        }

        public virtual ResultObject RemoveRetentionPolicyTag(string Identity)
        {
            // not implemented
            return null;
        }

        public virtual ResultObject SetRetentionPolicy(string Identity, string[] RetentionPolicyTagLinks)
        {
            // not implemented
            return null;
        }

        public virtual ResultObject RemoveRetentionPolicy(string Identity)
        {
            // not implemented
            return null;
        }


        #endregion

    }
}