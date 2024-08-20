using CamlBuilder;
using Newtonsoft.Json;

namespace FileSorter.Models
{
    public class SharePointList
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string odataContext { get; set; }
        public List<value> value { get; set; }
    }
}
