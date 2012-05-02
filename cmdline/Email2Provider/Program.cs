using System;
using System.Net.Mail;
using System.Collections.Generic;
using ARSoft.Tools.Net.Dns;
using Email2Calendar.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace E2C {
    internal class Program {
        
        private static void Main(string[] args) {
            var email = "";
            if (args.Length < 1 || String.IsNullOrEmpty(args[0])) {
                Console.Write("Enter eamkil address to test: ");
                email = Console.ReadLine();
            }
            else {
                email = args[0];
            }

            var e2c = new Email2Provider(email);
            e2c.Resolve();

            //Console.WriteLine(JsonConvert.SerializeObject(e2c, Formatting.Indented));

            //Console.Write("Press any key to continue...");
            //Console.ReadKey();
        }
    }
}