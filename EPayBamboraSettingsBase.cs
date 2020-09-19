using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.EPayBambora
{
    public class EPayBamboraSettingsBase
    {

        [PaymentProviderSetting(Name = "Accept URL",
            Description = "The URL to continue to after this provider has done processing. eg: /continue/",
            SortOrder = 100)]
        public string AcceptUrl { get; set; }

        [PaymentProviderSetting(Name = "Cancel URL",
            Description = "The URL to return to if the payment attempt is canceled. eg: /cancel/",
            SortOrder = 200)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error URL",
            Description = "The URL to return to if the payment attempt errors. eg: /error/",
            SortOrder = 300)]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(Name = "Test Merchant Number",
            Description = "Your Bambora Merchant Number for test transactions.",
            SortOrder = 400)]
        public string TestMerchantNumber { get; set; }

        [PaymentProviderSetting(Name = "Live Merchant Number",
            Description = "Your Bambora Merchant Number for live transactions.",
            SortOrder = 500)]
        public string LiveMerchantNumber { get; set; }

        [PaymentProviderSetting(Name = "MD5 Key",
            Description = "The MD5 hashing key obtained from the Bambora portal.",
            SortOrder = 1000)]
        public string Md5Key { get; set; }

        [PaymentProviderSetting(Name = "Webservice Password",
            Description = "The webservice password obtained from the Bambora portal.",
            SortOrder = 1100)]
        public string WebservicePassword { get; set; }


        [PaymentProviderSetting(Name = "Instant Capture",
            Description = "Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture.",
            SortOrder = 1300)]
        public bool Capture { get; set; }

        [PaymentProviderSetting(Name = "Test Mode",
            Description = "Set whether to process payments in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }

    }
}
