using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using ARSoft.Tools.Net.Dns;

namespace Email2Calendar.Services
{
    public class Email2Provider
    {
        private static readonly Dictionary<string, string> DomainProviders = new Dictionary<string, string> {
            {"outlook.com",                 "Microsoft Exchange"},
            {"exchangelabs.com",            "Microsoft Exchange"},
            {"microsoft.com",               "Microsoft Exchange"},
            {"exchange.ms",                 "Microsoft Exchange"},
            {"hotmail.com",                 "Windows Live/Hotmail"},
            {"google.com",                  "Google"},
            {"googlemail.com",              "Google"},
            {"aol.com",                     "AOL"},
            {"yahoodns.net",                "Yahoo! Calendar"},
            {"mac.com",                     "Apple MobileMe/iCloud"},
            {"mobile.me",                   "Apple MobileMe/iCloud"}
        };

        public class MxDetails
        {
            public MxRecord MxRecord;
            public List<String> EhloResponses = new List<string>();
        }
        public List<MxDetails> Details = new List<MxDetails>();

        public Email2Provider(string emailAddress)
        {
            EmailAddress = emailAddress;
        }

        public String EmailAddress { get; private set; }
        public String Provider { get; private set; }
        public String Clue { get; private set; }
        public String FailureReason { get; private set; }

        private static bool TryGetProvider(string mx, out string provider)
        {
            provider = "";
            var found = false;
            foreach (var key in DomainProviders.Keys.Where(mx.Contains))
            {
                found = DomainProviders.TryGetValue(key.ToLower(), out provider);
            }
            return found;
        }

        private void WriteLine(StreamWriter writer, string cmd)
        {
            Console.WriteLine("-> " + cmd);
            writer.WriteLine(cmd);
        }

        private List<string> GetEhlo(string server)
        {
            var ehlos = new List<string>();

            using (var client = new TcpClient())
            {
                bool tls = false;
                const int port = 25;
                // SSL can be either 465 (legacy) or 587 (standard)
                //const int sslPort = 587;
                Console.WriteLine(String.Format("Starting SMTP conversation with {0}:{1}", server, port));
                try
                {
                    client.Connect(server, port);
                    // As GMail requires SSL we should use SslStream
                    // If your SMTP server doesn't support SSL you can
                    // work directly with the underlying stream
                    using (var stream = client.GetStream())
                    //using (var sslStream = new SslStream(stream))
                    {
                        //sslStream.AuthenticateAsClient(server);
                        using (var writer = new StreamWriter(stream))
                        using (var reader = new StreamReader(stream))
                        {
                            WriteLine(writer, "EHLO " + server);
                            writer.Flush();
                            while (true)
                            {
                                var line = reader.ReadLine();
                                if (line != null)
                                {
                                    Console.WriteLine("<- " + line);
                                    ehlos.Add(line);

                                    if (line.ToUpper().StartsWith("250-STARTTLS"))
                                    {
                                        tls = true;
                                    }

                                    if (tls || line.ToUpper().StartsWith("250 "))
                                    {
                                        WriteLine(writer, "QUIT");
                                        writer.Flush();
                                        break;
                                    }
                                }
                                else
                                {
                                    //WriteLine(writer, "QUIT");
                                    //writer.Flush();
                                    break;
                                }
                            }

                            // If TLS then upgrade to TLS...
                        }
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se);
                } // try/catch
            } // using
            return ehlos;
        }

        public bool Resolve()
        {
            if (String.IsNullOrEmpty(EmailAddress))
            {
                return false;
            }

            var found = false;
            string host;
            try
            {
                host = new MailAddress(EmailAddress).Host;
            }
            catch (FormatException)
            {
                FailureReason = "<code>" + EmailAddress + "</code> is an invalid email address";
                return false;
            }

            var dnsMessage = DnsClient.Default.Resolve(host, RecordType.Mx);
            if ((dnsMessage == null) ||
                ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
            {
                FailureReason = "The DNS for for <code>" + host + "</code> failed";
                return false;
            }

            if (dnsMessage.AnswerRecords.Count == 0)
            {
                FailureReason = "There are no DNS records for <code>" + host + "</code>.";
                return false;
            }

            foreach (var mxRecord in dnsMessage.AnswerRecords.OfType<MxRecord>())
            {
                string tempProvider;
                if (TryGetProvider(mxRecord.ExchangeDomainName, out tempProvider))
                {
                    found = true;
                    Clue = "We figured this out by looking at the domain name of your MX host, which is <code>" + mxRecord.ExchangeDomainName + "</code>.";
                    Provider = tempProvider;
                    //break;
                }

                var ehlos = GetEhlo(mxRecord.ExchangeDomainName);
                Details.Add(new MxDetails { MxRecord = mxRecord, EhloResponses = ehlos });

                // Go through the ehlos responses looking for a hit
                foreach (var ehlo in ehlos) {
                    string temp;
                    if (TryGetProvider(ehlo, out temp)) {
                        found = true;
                        Clue = "We figured this out by looking at the domain name your SMTP server claims in its EHLO response, which was <code>" + ehlo + "</code>.";
                        Provider = temp;
                        break;
                    }
                }

                //if (found) break;

            }

            if (!found)
            {
                FailureReason = "We could not determine what the email provider for <code>" + host + "</code>.";
            }

            return found;
        }
    }
}