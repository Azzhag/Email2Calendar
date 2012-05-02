using System;
using System.Collections.Generic;
using System.Net.Mail;
using ARSoft.Tools.Net.Dns;

namespace Email2Calendar.Services {
    public class Email2Provider {
        private static readonly Dictionary<string, string> _dictProviders = new Dictionary<string, string> {
            {
                "outlook.com"
                ,
                "Microsoft Exchange"
                },
            {
                "exchangelabs.com"
                ,
                "Microsoft Exchange"
                },
            {
                "google.com"
                ,
                "Google"
                },
            {
                "aol.com"
                ,
                "AOL"
                },
            {
                "yahoo.com"
                ,
                "Yahoo! Calendar"
                }
        };

        private readonly string _emailAddress;

        public Email2Provider(string emailAddress) {
            _emailAddress = emailAddress;
        }

        public String Provider { get; private set; }
        public String FailureReason { get; private set; }

        private bool TryGetProvider(string mx, out string provider) {
            provider = "";
            bool found = false;
            foreach (string key in _dictProviders.Keys) {
                if (mx.Contains(key)) {
                    found = _dictProviders.TryGetValue(key, out provider);
                }
            }
            return found;
        }

        public bool Resolve() {
            string address = "";
            if (String.IsNullOrEmpty(_emailAddress)) {
                return false;
            }

            var mailAddress = new MailAddress(_emailAddress);

            string provider = "";
            bool found = false;
            DnsMessage dnsMessage = DnsClient.Default.Resolve(mailAddress.Host, RecordType.Mx);
            if ((dnsMessage == null) ||
                ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain))) {
                FailureReason = "DNS request failed";
                return false;
            }
            else {
                foreach (DnsRecordBase dnsRecord in dnsMessage.AnswerRecords) {
                    var mxRecord = dnsRecord as MxRecord;
                    if (mxRecord != null) {
                        found = TryGetProvider(mxRecord.ExchangeDomainName, out provider);
                    }
                }
            }
            if (found) {
                Provider = provider;
            }
            else {
                FailureReason = "Provider could not be determined.";
            }
            return found;
        }
    }
}