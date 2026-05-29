namespace Imato.ConfigurationProvider.SqlServer;

internal static class DictionaryExtensions
{
    public static void AddOrUdate<K, V>(this IDictionary<K?, V?> dictionary, K? key, V? value)
    {
        if (key == null || value == null) return;
        if (dictionary.TryAdd(key, value)) return;
        dictionary[key] = value;
    }
}