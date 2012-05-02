using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using ARSoft.Tools.Net.Dns;

namespace Email2Calendar.Services {
    public class Email2Provider {
        private static readonly Dictionary<string, string> DomainProviders = new Dictionary<string, string> {
            {"outlook.com",                 "Microsoft Exchange"},
            {"exchangelabs.com",            "Microsoft Exchange"},
            {"microsoft.com",               "Microsoft Exchange"},
            {"hotmail.com",                 "Windows Live/Hotmail"},
            {"google.com",                  "Google"},
            {"googlemail.com",              "Google"},
            {"aol.com",                     "AOL"},
            {"yahoodns.net",                "Yahoo! Calendar"}
        };

        public List<MxRecord> MxRecords = new List<MxRecord>();


        public Email2Provider(string emailAddress) {
            EmailAddress = emailAddress;
        }

        public String EmailAddress { get; private set; }
        public String Provider { get; private set; }
        public String FailureReason { get; private set; }

        private static bool TryGetProvider(string mx, out string provider) {
            provider = "";
            var found = false;
            foreach (var key in DomainProviders.Keys.Where(mx.Contains)) {
                found = DomainProviders.TryGetValue(key, out provider);
            }
            return found;
        }

        public bool Resolve() {
            if (String.IsNullOrEmpty(EmailAddress)) {
                return false;
            }

            var found = false;
            string host;
            try {
                host = new MailAddress(EmailAddress).Host;
            }
            catch (FormatException) {
                FailureReason = "Invalid email address";
                return false;
            }

            var dnsMessage = DnsClient.Default.Resolve(host, RecordType.Mx);
            if ((dnsMessage == null) ||
                ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain))) {
                FailureReason = "The DNS request failed";
                return false;
            }

            if (dnsMessage.AnswerRecords.Count == 0) {
                FailureReason = "There are no DNS records for that email domain.";
                return false;
            }

            foreach (var mxRecord in dnsMessage.AnswerRecords.OfType<MxRecord>())
            {
                MxRecords.Add(mxRecord);
                string tempProvider;
                if (!TryGetProvider(mxRecord.ExchangeDomainName, out tempProvider)) continue;
                found = true;
                Provider = tempProvider;
            }

            if (!found) {
                if (MxRecords.Count > 0) {
                    FailureReason = "The mail server was not recognized.";

                    // We have at least one MX record. Do EHLO on each...
                }
                else {
                    FailureReason = "Not quite sure why the provider could not be determined.";    
                }
                
            }
            return found;
        }
    }
}