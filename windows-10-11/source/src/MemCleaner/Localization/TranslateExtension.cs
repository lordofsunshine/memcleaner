using System.Windows.Data;
using System.Windows.Markup;

namespace MemCleaner.Localization;

public sealed class Tr : MarkupExtension
{
    private readonly string _key;

    public Tr(string key)
    {
        _key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding("[" + _key + "]")
        {
            Source = Loc.Current,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
