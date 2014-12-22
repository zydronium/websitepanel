﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebsitePanel.Providers.DNS;

namespace WebsitePanel.Providers.DomainLookup
{
    public class DnsRecordInfoChange
    {
        public string DnsServer { get; set; }
        public DnsRecordInfo OldRecord { get; set; }
        public DnsRecordInfo NewRecord { get; set; }
        public DomainDnsRecordStatuses Status { get; set; }
        public DnsRecordType Type { get; set; }
    }
}
