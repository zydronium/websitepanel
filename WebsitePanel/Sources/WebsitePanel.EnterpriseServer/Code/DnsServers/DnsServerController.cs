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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Serialization;
using WebsitePanel.Providers;
using WebsitePanel.Providers.DNS;

namespace WebsitePanel.EnterpriseServer
{
    public class DnsServerController : IImportController, IBackupController
    {
		private static DNSServer GetDNSServer(int serviceId)
		{
			DNSServer dns = new DNSServer();
			ServiceProviderProxy.Init(dns, serviceId);
			return dns;
		}

		public static int AddZone(int packageId, int serviceId, string zoneName)
		{
			return AddZone(packageId, serviceId, zoneName, true);
		}

		public static int AddZone(int packageId, int serviceId, string zoneName, bool addPackageItem)
		{
			// get DNS provider
			DNSServer dns = GetDNSServer(serviceId);

			// check if zone already exists
			if (dns.ZoneExists(zoneName))
				return BusinessErrorCodes.ERROR_DNS_ZONE_EXISTS;

			//
			TaskManager.StartTask("DNS_ZONE", "ADD", zoneName);
			//
			int zoneItemId = default(int);
			//
			try
			{
				// get secondary DNS services
				StringDictionary primSettings = ServerController.GetServiceSettings(serviceId);
				string[] primaryIPAddresses = GetExternalIPAddressesFromString(primSettings["ListeningIPAddresses"]);

				List<string> secondaryIPAddresses = new List<string>();
				List<int> secondaryServiceIds = new List<int>();
				string strSecondaryServices = primSettings["SecondaryDNSServices"];
				if (!String.IsNullOrEmpty(strSecondaryServices))
				{
					string[] secondaryServices = strSecondaryServices.Split(',');
					foreach (string strSecondaryId in secondaryServices)
					{
						int secondaryId = Utils.ParseInt(strSecondaryId, 0);
						if (secondaryId == 0)
							continue;

						secondaryServiceIds.Add(secondaryId);
						StringDictionary secondarySettings = ServerController.GetServiceSettings(secondaryId);

						// add secondary IPs to the master array
						secondaryIPAddresses.AddRange(
							GetExternalIPAddressesFromString(secondarySettings["ListeningIPAddresses"]));
					}
				}

				// add "Allow zone transfers"
				string allowTransfers = primSettings["AllowZoneTransfers"];
				if (!String.IsNullOrEmpty(allowTransfers))
				{
					string[] ips = Utils.ParseDelimitedString(allowTransfers, '\n', ' ', ',', ';');
					foreach (string ip in ips)
					{
						if (!secondaryIPAddresses.Contains(ip))
							secondaryIPAddresses.Add(ip);
					}
				}

				// add primary zone
				dns.AddPrimaryZone(zoneName, secondaryIPAddresses.ToArray());

				// get DNS zone records
				List<GlobalDnsRecord> records = ServerController.GetDnsRecordsTotal(packageId);

				// get name servers
				PackageSettings packageSettings = PackageController.GetPackageSettings(packageId, PackageSettings.NAME_SERVERS);
				string[] nameServers = new string[] { };
				if (!String.IsNullOrEmpty(packageSettings["NameServers"]))
					nameServers = packageSettings["NameServers"].Split(';');

				// build records list
				List<DnsRecord> zoneRecords = new List<DnsRecord>();

				string primaryNameServer = "ns." + zoneName;

				if (nameServers.Length > 0)
					primaryNameServer = nameServers[0];

				// update SOA record

				string hostmaster = primSettings["ResponsiblePerson"];
				if (String.IsNullOrEmpty(hostmaster))
				{
					hostmaster = "hostmaster." + zoneName;
				}
				else
				{
					hostmaster = Utils.ReplaceStringVariable(hostmaster, "domain_name", zoneName);
				}

				dns.UpdateSoaRecord(zoneName, "", primaryNameServer, hostmaster);

				// add name servers
				foreach (string nameServer in nameServers)
				{
					DnsRecord ns = new DnsRecord();
					ns.RecordType = DnsRecordType.NS;
					ns.RecordName = "";
					ns.RecordData = nameServer;

					zoneRecords.Add(ns);
				}

				// add all other records
				zoneRecords.AddRange(
					BuildDnsResourceRecords(records, zoneName, ""));

				// add zone records
				dns.AddZoneRecords(zoneName, zoneRecords.ToArray());


				// add secondary zones
				foreach (int secondaryId in secondaryServiceIds)
				{
					try
					{
						// add secondary zone
						DNSServer secDns = GetDNSServer(secondaryId);
						secDns.AddSecondaryZone(zoneName, primaryIPAddresses);
						RegisterZoneItems(packageId, secondaryId, zoneName, false);
					}
					catch (Exception ex)
					{
						TaskManager.WriteError(ex, "Error adding secondary zone (service ID = " + secondaryId + ")");
					}
				}

				if (!addPackageItem)
					return 0;
				// add service item
				zoneItemId = RegisterZoneItems(packageId, serviceId, zoneName, true);
				//
				TaskManager.ItemId = zoneItemId;
			}
			catch (Exception ex)
			{
				TaskManager.WriteError(ex);
			}
			finally
			{
				TaskManager.CompleteTask();
			}
			//
			return zoneItemId;
		}


        private static int RegisterZoneItems(int spaceId, int serviceId, string zoneName, bool primaryZone)
        {
            // zone item
            DnsZone zone = primaryZone ? new DnsZone() : new SecondaryDnsZone();
            zone.Name = zoneName;
            zone.PackageId = spaceId;
            zone.ServiceId = serviceId;
            int zoneItemId = PackageController.AddPackageItem(zone);
            return zoneItemId;
        }
        
        public static int DeleteZone(int zoneItemId)
		{
		    // delete DNS zone if applicable
            DnsZone zoneItem = (DnsZone)PackageController.GetPackageItem(zoneItemId);
			//
			if (zoneItem != null)
			{
				TaskManager.StartTask("DNS_ZONE", "DELETE", zoneItem.Name);
				//
				try
				{
					//
					TaskManager.ItemId = zoneItemId;
					// delete DNS zone
					DNSServer dns = new DNSServer();
					ServiceProviderProxy.Init(dns, zoneItem.ServiceId);

					// delete secondary zones
					StringDictionary primSettings = ServerController.GetServiceSettings(zoneItem.ServiceId);
					string strSecondaryServices = primSettings["SecondaryDNSServices"];
					if (!String.IsNullOrEmpty(strSecondaryServices))
					{
						string[] secondaryServices = strSecondaryServices.Split(',');
						foreach (string strSecondaryId in secondaryServices)
						{
							try
							{
								int secondaryId = Utils.ParseInt(strSecondaryId, 0);
								if (secondaryId == 0)
									continue;

								DNSServer secDns = new DNSServer();
								ServiceProviderProxy.Init(secDns, secondaryId);

								secDns.DeleteZone(zoneItem.Name);
							}
							catch (Exception ex1)
							{
								// problem when deleting secondary zone
								TaskManager.WriteError(ex1, "Error deleting secondary DNS zone");
							}
						}
					}

					try
					{
						dns.DeleteZone(zoneItem.Name);
					}
					catch (Exception ex2)
					{
						TaskManager.WriteError(ex2, "Error deleting primary DNS zone");
					}

					// delete service item
					PackageController.DeletePackageItem(zoneItemId);
				}
				catch (Exception ex)
				{
					TaskManager.WriteError(ex);
				}
				finally
				{
					TaskManager.CompleteTask();
				}
			}
			//
			return 0;
		}

		public static List<DnsRecord> BuildDnsResourceRecords(List<GlobalDnsRecord> records,
			string domainName, string serviceIP)
		{
			List<DnsRecord> zoneRecords = new List<DnsRecord>();

			foreach (GlobalDnsRecord record in records)
			{
				DnsRecord rr = new DnsRecord();
				rr.RecordType = (DnsRecordType)Enum.Parse(typeof(DnsRecordType), record.RecordType, true);
				rr.RecordName = record.RecordName;

				if (record.RecordType == "A" || record.RecordType == "AAAA")
				{
					rr.RecordData = String.IsNullOrEmpty(record.RecordData) ? record.ExternalIP : record.RecordData;
					rr.RecordData = Utils.ReplaceStringVariable(rr.RecordData, "ip", record.ExternalIP);

					if (String.IsNullOrEmpty(rr.RecordData) && !String.IsNullOrEmpty(serviceIP))
						rr.RecordData = serviceIP;
				}
				else
				{
					rr.RecordData = record.RecordData;
				}

				// substitute variables
				rr.RecordData = Utils.ReplaceStringVariable(rr.RecordData, "domain_name", domainName);

				// add MX priority
				if (record.RecordType == "MX")
					rr.MxPriority = record.MxPriority;

				if (!String.IsNullOrEmpty(rr.RecordData))
					zoneRecords.Add(rr);
			}

			return zoneRecords;
		}

		public static string[] GetExternalIPAddressesFromString(string str)
		{
			List<string> ips = new List<string>();

			if (str != null && str.Trim() != "")
			{
				string[] sips = str.Split(',');
				foreach (string sip in sips)
				{
					IPAddressInfo ip = ServerController.GetIPAddress(Int32.Parse(sip));
					if (ip != null)
						ips.Add(ip.ExternalIP);
				}
			}

			return ips.ToArray();
		}

        #region IImportController Members

        public List<string> GetImportableItems(int packageId, int itemTypeId, Type itemType, ResourceGroupInfo group)
        {
            List<string> items = new List<string>();

            // get service id
            int serviceId = PackageController.GetPackageServiceId(packageId, group.GroupName);
            if (serviceId == 0)
                return items;

            // Mail provider
            DNSServer dns = new DNSServer();
            ServiceProviderProxy.Init(dns, serviceId);

            if (itemType == typeof(DnsZone))
                items.AddRange(dns.GetZones());

            return items;
        }

        public void ImportItem(int packageId, int itemTypeId, Type itemType,
			ResourceGroupInfo group, string itemName)
        {
            // get service id
            int serviceId = PackageController.GetPackageServiceId(packageId, group.GroupName);
            if (serviceId == 0)
                return;

            if (itemType == typeof(DnsZone))
            {
                // add DNS zone
                DnsZone zone = new DnsZone();
                zone.Name = itemName;
                zone.ServiceId = serviceId;
                zone.PackageId = packageId;
                int zoneId = PackageController.AddPackageItem(zone);

                // add/update domains/pointers
                RestoreDomainByZone(itemName, packageId, zoneId);
            }
        }

        private void RestoreDomainByZone(string itemName, int packageId, int zoneId)
        {
            DomainInfo domain = ServerController.GetDomain(itemName);
            if (domain == null)
            {
                domain = new DomainInfo();
                domain.DomainName = itemName;
                domain.PackageId = packageId;
                domain.ZoneItemId = zoneId;
                ServerController.AddDomainItem(domain);
            }
            else
            {
                domain.ZoneItemId = zoneId;
                ServerController.UpdateDomain(domain);
            }
        }

        #endregion

        #region IBackupController Members

        public int BackupItem(string tempFolder, XmlWriter writer, ServiceProviderItem item, ResourceGroupInfo group)
        {
			if (!(item is DnsZone))
				return 0;

            // DNS provider
			DNSServer dns = GetDNSServer(item.ServiceId);

            // zone records serialized
            XmlSerializer serializer = new XmlSerializer(typeof(DnsRecord));

            try
            {
                // get zone records
                DnsRecord[] records = dns.GetZoneRecords(item.Name);

                // serialize zone records
                foreach (DnsRecord record in records)
                    serializer.Serialize(writer, record);
            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex, "Could not read zone records");
            }

            return 0;
        }

        public int RestoreItem(string tempFolder, XmlNode itemNode, int itemId, Type itemType,
            string itemName, int packageId, int serviceId, ResourceGroupInfo group)
        {
			if (itemType != typeof(DnsZone))
				return 0;

			// DNS provider
			DNSServer dns = GetDNSServer(serviceId);

			// check service item
			if (!dns.ZoneExists(itemName))
			{
				// create primary and secondary zones
				AddZone(packageId, serviceId, itemName, false);

				// restore records
				XmlSerializer serializer = new XmlSerializer(typeof(DnsRecord));
				List<DnsRecord> records = new List<DnsRecord>();
				foreach (XmlNode childNode in itemNode.ChildNodes)
				{
					if (childNode.Name == "DnsRecord")
					{
						records.Add((DnsRecord)serializer.Deserialize(new XmlNodeReader(childNode)));
					}
				}

				dns.AddZoneRecords(itemName, records.ToArray());
			}

            // check if meta-item exists
            int zoneId = 0;
            DnsZone item = (DnsZone)PackageController.GetPackageItemByName(packageId, itemName, typeof(DnsZone));
            if (item == null)
            {
                // restore meta-item
                item = new DnsZone();
                item.Name = itemName;
                item.PackageId = packageId;
                item.ServiceId = serviceId;
                zoneId = PackageController.AddPackageItem(item);
            }
            else
            {
                zoneId = item.Id;
            }

            // restore domains
            RestoreDomainByZone(itemName, packageId, zoneId);

			return 0;
        }

        #endregion
    }
}
