// Copyright 2012 Kindel Systems, LLC.
//   
// This file is part of Email2Calendar
//  
// Email2Calendar is free software: you can redistribute it and/or modify it under the 
// terms of the MIT License (http://www.opensource.org/licenses/mit-license.php)
//  
// Official source repository is at https://github.com/tig/Email2Calendar
//  
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using ARSoft.Tools.Net.Dns;

namespace Email2Calendar.Services {
    public class Email2Provider {
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

        private static readonly Dictionary<string, string> SmtpCapabilities = new Dictionary<string, string> {
            {"XEXCH50",                     "Microsoft Exchange"}, // http://technet.microsoft.com/en-us/library/dd535395(v=EXCHG.80).aspx
            {"X-EXPS",                      "Microsoft Exchange"}  // http://technet.microsoft.com/en-us/library/dd535395(v=EXCHG.80).aspx
        };

        /// <summary>
        /// Contains the MX record details, including EHLO responses from the SMTP servers. 
        /// For diagnostics purposes.
        /// </summary>
        public List<MxDetails> Details = new List<MxDetails>();

        public Email2Provider(string emailAddress) {
            EmailAddress = emailAddress;
            DiagnosticsMode = false;
        }

        #region Nested type: MxDetails
        public class MxDetails
        {
            public List<String> EhloResponses = new List<string>();
            public MxRecord MxRecord;
        }
        #endregion

        // Public members
        /// <summary>
        /// Gets the email address set when the object was instanced.
        /// </summary>
        public String EmailAddress { get; private set; }

        /// <summary>
        /// Gets the name of the email system provider.
        /// </summary>
        public String Provider { get; private set; }

        /// <summary>
        /// Gets a description of how the email system provider was determined.
        /// </summary>
        public String Clue { get; private set; }

        /// <summary>
        /// Gets the reason resolution was not successful. Will be null if resolution was successful.
        /// </summary>
        public String FailureReason { get; private set; }

        public bool DiagnosticsMode { get; set; }

        private static bool TryGetProvider(string mx, out string provider) {
            provider = "";
            bool found = false;
            foreach (string key in DomainProviders.Keys.Where(mx.Contains)) {
                found = DomainProviders.TryGetValue(key.ToLower(), out provider);
            }
            return found;
        }

        private static bool TryGetProviderFromSmtpCapability(string ehloResponse, out string provider) {
            provider = SmtpCapabilities
                .Where(c => ehloResponse.Contains(c.Key))
                .Select(c => c.Value)
                .SingleOrDefault();

            return provider != null;
        }

        private void WriteLine(StreamWriter writer, string cmd) {
            Console.WriteLine("-> " + cmd);
            writer.WriteLine(cmd);
        }

        private List<string> GetEhlo(string server) {
            var ehlos = new List<string>();

            using (var client = new TcpClient()) {
                var tls = false;
                const int port = 25;
                // SSL can be either 465 (legacy) or 587 (standard)
                //const int sslPort = 587;
                Console.WriteLine(String.Format("Starting SMTP conversation with {0}:{1}", server, port));
                try {
                    client.Connect(server, port);
                    // As GMail requires SSL we should use SslStream
                    // If your SMTP server doesn't support SSL you can
                    // work directly with the underlying stream
                    using (NetworkStream stream = client.GetStream())
                        //using (var sslStream = new SslStream(stream))
                    {
                        //sslStream.AuthenticateAsClient(server);
                        using (var writer = new StreamWriter(stream))
                        using (var reader = new StreamReader(stream)) {
                            WriteLine(writer, "EHLO " + server);
                            writer.Flush();
                            while (true) {
                                var line = reader.ReadLine();
                                if (line == null) {
                                    //WriteLine(writer, "QUIT");
                                    //writer.Flush();
                                    break;
                                }
                                else {
                                    Console.WriteLine("<- " + line);
                                    ehlos.Add(line);

                                    if (line.ToUpper().StartsWith("250-STARTTLS")) {
                                        tls = true;
                                    }

                                    // If TLS or erro (5xx) or end (250 with a space) we're done
                                    if (tls || line.StartsWith("5") || line.ToUpper().StartsWith("250 ")) {
                                        try {
                                            WriteLine(writer, "QUIT");
                                            writer.Flush();
                                        }
                                        catch (SocketException) {
                                        }
                                        break;
                                    }
                                }
                            }

                            // If TLS then upgrade to TLS...
                        }
                    }
                }
                catch (SocketException se) {
                    Console.WriteLine(se);
                } // try/catch
            } // using
            return ehlos;
        }

        /// <summary>
        /// Resolves EmailAddress to a calendar provider. Upon return, the public members will be
        /// filled out with resolution information.  
        /// On success FailureReason will be null, otherwise it will be a string describing the failure.
        /// </summary>
        /// <returns>True the Calendar Provider could be determined. False if not.</returns>
        public bool Resolve() {
            FailureReason = null;
            if (String.IsNullOrEmpty(EmailAddress)) {
                FailureReason = "Email address is null or empty";
                return false;
            }

            string host;
            try {
                host = new MailAddress(EmailAddress).Host;
            }
            catch (FormatException) {
                FailureReason = "<code>" + EmailAddress + "</code> is an invalid email address";
                return false;
            }

            DnsMessage dnsMessage = DnsClient.Default.Resolve(host, RecordType.Mx);
            if ((dnsMessage == null) ||
                ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain))) {
                FailureReason = "The DNS for for <code>" + host + "</code> failed";
                return false;
            }

            if (dnsMessage.AnswerRecords.Count == 0) {
                FailureReason = "There are no DNS records for <code>" + host + "</code>.";
                return false;
            }

            var found = false;
            foreach (var mxRecord in dnsMessage.AnswerRecords.OfType<MxRecord>()) {
                string tempProvider;
                if (TryGetProvider(mxRecord.ExchangeDomainName, out tempProvider)) {
                    found = true;
                    Clue = "The domain name of the MX host is <code>" + mxRecord.ExchangeDomainName + "</code>.";
                    Provider = tempProvider;
                    if (!DiagnosticsMode)
                        break;
                }

                List<string> ehlos = GetEhlo(mxRecord.ExchangeDomainName);
                Details.Add(new MxDetails {MxRecord = mxRecord, EhloResponses = ehlos});

                // Go through the ehlos responses looking for a hit
                foreach (var ehlo in ehlos) {
                    string temp;
                    if (TryGetProvider(ehlo, out temp)) {
                        found = true;
                        Clue = "The domain name the SMTP server claims in its EHLO response is <code>" + ehlo +
                               "</code>.";
                        Provider = temp;
                        break;
                    }

                    if (TryGetProviderFromSmtpCapability(ehlo, out temp)) {
                        found = true;
                        Clue = "The SMTP server claims in its EHLO response to support <code>" + ehlo +
                               "</code>, which is a proprietary extension.";
                        Provider = temp;
                        break;
                    }
                }

                if (!DiagnosticsMode && found)
                    break;
            }

            if (!found) {
                FailureReason = "We could not determine what the email provider for <code>" + host + "</code>.";
            }

            return found;
        }

    }
}