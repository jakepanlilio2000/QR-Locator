using Newtonsoft.Json;

namespace LocatorApp.Models
{
    public class LocatorQrPayload
    {
        [JsonProperty("locator_info")]
        public LocatorInfo Info { get; set; }
    }

    public class LocatorInfo
    {
        [JsonProperty("SlotNo")]
        public string SlotNo { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("aisle")]
        public string Aisle { get; set; }

        [JsonProperty("bay")]
        public string Bay { get; set; }

        [JsonProperty("bayname")]
        public string BayName { get; set; }

        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("RecNo")]
        public string RecNo { get; set; }

        [JsonProperty("loc_status")]
        public string LocStatus { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }
}