using BTCPayServerDockerConfigurator.Controllers;
using Microsoft.Extensions.Configuration;

namespace BTCPayServerDockerConfigurator.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
