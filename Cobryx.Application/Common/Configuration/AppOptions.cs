using System.ComponentModel.DataAnnotations;

namespace Cobryx.Application.Common.Configuration;

public class AppOptions
{
    [Required, Url]
    public string BaseUrl { get; set; } = "https://cobryx.com.mx";

    [Required, Url]
    public string AppUrl { get; set; } = "https://app.cobryx.com.mx";

    [Required, Url]
    public string DocsUrl { get; set; } = "https://docs.api.cobryx.com.mx";
}
