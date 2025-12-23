using System.Globalization;

namespace Unchained.Domain;

public readonly record struct ChannelId(int Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
