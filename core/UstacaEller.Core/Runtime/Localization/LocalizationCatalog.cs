using System;
using System.Collections.Generic;
using System.Text;

namespace UstacaEller.Core.Localization
{
    /// <summary>
    /// Resolves localization keys and locale-specific asset paths.
    ///
    /// Parsing is deliberately not this class's job — it takes plain dictionaries so
    /// the core assembly stays free of JSON dependencies and this logic can be tested
    /// without touching a file.
    ///
    /// Note that in this product the catalog is the smaller half of localization. The
    /// audience cannot read, so the asset that actually carries the language is
    /// voice-over; see <see cref="VoicePath"/>.
    /// </summary>
    public sealed class LocalizationCatalog
    {
        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _byLocale;
        private readonly List<string> _lookupOrder;
        private readonly HashSet<string> _missingKeys = new HashSet<string>();

        public LocalizationCatalog(
            string activeLocale,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> catalogsByLocale,
            IReadOnlyList<string> fallbackChain)
        {
            if (string.IsNullOrEmpty(activeLocale)) throw new ArgumentException("Active locale is required.", nameof(activeLocale));
            if (catalogsByLocale == null) throw new ArgumentNullException(nameof(catalogsByLocale));

            ActiveLocale = activeLocale;
            _byLocale = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> entry in catalogsByLocale)
            {
                _byLocale[entry.Key] = entry.Value;
            }

            _lookupOrder = new List<string> { activeLocale };
            foreach (string locale in fallbackChain ?? Array.Empty<string>())
            {
                if (!_lookupOrder.Contains(locale)) _lookupOrder.Add(locale);
            }
        }

        public string ActiveLocale { get; }

        /// <summary>
        /// Keys that had to fall back or were missing entirely. Surfaced so a QA build
        /// can report gaps instead of them being noticed by a user.
        /// </summary>
        public IReadOnlyCollection<string> MissingKeys => _missingKeys;

        public bool Has(string key) =>
            _byLocale.TryGetValue(ActiveLocale, out IReadOnlyDictionary<string, string> catalog)
            && catalog.ContainsKey(key);

        /// <summary>
        /// Returns the localized string, walking the fallback chain when needed.
        /// A completely unknown key returns the key itself: visible in a build, never
        /// a blank label, and obvious in a screenshot.
        /// </summary>
        public string Get(string key, IReadOnlyDictionary<string, string> arguments = null)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key is required.", nameof(key));

            foreach (string locale in _lookupOrder)
            {
                if (!_byLocale.TryGetValue(locale, out IReadOnlyDictionary<string, string> catalog)) continue;
                if (!catalog.TryGetValue(key, out string value)) continue;

                if (!string.Equals(locale, ActiveLocale, StringComparison.Ordinal)) _missingKeys.Add(key);
                return Format(value, arguments);
            }

            _missingKeys.Add(key);
            return key;
        }

        /// <summary>
        /// Where a voice clip lives for the active locale. Audio declared as
        /// <c>type: "voice"</c> in a scene manifest is resolved through here; sfx and
        /// ambience are shared across locales and must not go through it.
        /// </summary>
        public string VoicePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));

            return $"audio/{ActiveLocale}/{fileName}";
        }

        /// <summary>
        /// Substitutes <c>{name}</c> placeholders. An unknown placeholder is left in
        /// place rather than blanked, so a translation bug looks like a bug.
        /// </summary>
        private static string Format(string template, IReadOnlyDictionary<string, string> arguments)
        {
            if (arguments == null || arguments.Count == 0 || template.IndexOf('{') < 0) return template;

            var builder = new StringBuilder(template.Length);
            int index = 0;

            while (index < template.Length)
            {
                int open = template.IndexOf('{', index);
                if (open < 0) break;

                int close = template.IndexOf('}', open + 1);
                if (close < 0) break;

                string name = template.Substring(open + 1, close - open - 1);
                builder.Append(template, index, open - index);
                builder.Append(arguments.TryGetValue(name, out string replacement) ? replacement : $"{{{name}}}");
                index = close + 1;
            }

            builder.Append(template, index, template.Length - index);
            return builder.ToString();
        }
    }
}
