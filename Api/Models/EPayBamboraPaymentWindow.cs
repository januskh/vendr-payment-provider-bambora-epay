using Newtonsoft.Json;

namespace Vendr.PaymentProviders.EPayBambora.Api.Models
{
    public class EPayBamboraPaymentWindow
    {
        public static class Configurations
        {
            public const int Overlay = 1;
            public const int IFrame = 2;
            public const int FullScreen = 3;
            public const int Integrated = 4;

        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("paymentmethods")]
        public EPayBamboraPaymentFilter[] PaymentMethods { get; set; }

        [JsonProperty("paymentgroups")]
        public EPayBamboraPaymentFilter[] PaymentGroups { get; set; }

        [JsonProperty("paymenttypes")]
        public EPayBamboraPaymentFilter[] PaymentTypes { get; set; }

        public EPayBamboraPaymentWindow()
        {
            Id = Configurations.FullScreen;
        }
    }
}
