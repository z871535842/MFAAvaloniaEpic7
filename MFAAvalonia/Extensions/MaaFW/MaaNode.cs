using MFAAvalonia.Utilities.Attributes;
using MFAAvalonia.ViewModels;
using Newtonsoft.Json;
using System.ComponentModel;

namespace MFAAvalonia.Extensions.MaaFW;

[MaaProperty]
public partial class MaaNode : ViewModelBase
{
    [Browsable(false)] [JsonIgnore] [JsonProperty("name")]
    private string? _name;
}
