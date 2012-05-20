// Copyright 2012 Kindel Systems, LLC.
// 
// This file is part of Email2Calendar
//
// Email2Calendar is free software: you can redistribute it and/or modify it under the 
// terms of the MIT License (http://www.opensource.org/licenses/mit-license.php)
//
// Official source repository is at https://github.com/tig/Email2Calendar
//
using Email2Calendar.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests {
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class EmailToPoviderTests {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        [TestMethod, Description("EmailToProvider: Empty email address should fail.")]
        public void TestResolveFailure() {
            var provider = new Email2Provider("");
            Assert.IsNotNull(provider);

            bool success = provider.Resolve();
            Assert.IsFalse(success);

            Assert.AreEqual("Email address is null or empty", provider.FailureReason);
        }

        [TestMethod, Description("EmailToProvider: Office365 email address should be Exchange.")]
        public void TestResolveOffice365() {
            var provider = new Email2Provider("charlie@bizlogr.com");
            Assert.IsNotNull(provider);

            bool success = provider.Resolve();
            Assert.IsTrue(success);

            Assert.AreEqual(null, provider.FailureReason);

            Assert.AreEqual("Microsoft Exchange", provider.Provider);
        }

        [TestMethod, Description("EmailToProvider: Microsoft employee email address should be Exchange.")]
        public void TestResolveMsEmployee() {
            var provider = new Email2Provider("billg@microsoft.com");
            Assert.IsNotNull(provider);

            bool success = provider.Resolve();
            Assert.IsTrue(success);

            Assert.AreEqual(null, provider.FailureReason);

            Assert.AreEqual("Microsoft Exchange", provider.Provider);
        }

        [TestMethod, Description("EmailToProvider: SMTP server with Exchange SMTP extensions should be Exchange.")]
        public void TestResolveExchangeSmtp() {
            var provider = new Email2Provider("user@oddie.com.au");
            Assert.IsNotNull(provider);

            bool success = provider.Resolve();
            Assert.IsTrue(success);

            Assert.AreEqual(null, provider.FailureReason);

            Assert.AreEqual("Microsoft Exchange", provider.Provider);
        }
    }
}