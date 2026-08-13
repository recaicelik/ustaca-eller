using System.Collections.Generic;
using UstacaEller.Core.Localization;
using Xunit;

namespace UstacaEller.Core.Tests
{
    public class LocalizationCatalogTests
    {
        private static LocalizationCatalog Catalog(string activeLocale)
        {
            var turkish = new Dictionary<string, string>
            {
                ["scene.kitchen.title"] = "Mutfak",
                ["subscription.renews"] = "{date} tarihinde yenilenecek",
                ["settings.sound"] = "Ses",
            };

            var english = new Dictionary<string, string>
            {
                ["scene.kitchen.title"] = "Kitchen",
                ["subscription.renews"] = "Renews on {date}",
                // settings.sound deliberately absent: a locale mid-translation.
            };

            var byLocale = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["tr"] = turkish,
                ["en"] = english,
            };

            return new LocalizationCatalog(activeLocale, byLocale, new[] { "tr" });
        }

        [Fact]
        public void ReturnsTheStringForTheActiveLocale()
        {
            Assert.Equal("Kitchen", Catalog("en").Get("scene.kitchen.title"));
            Assert.Equal("Mutfak", Catalog("tr").Get("scene.kitchen.title"));
        }

        [Fact]
        public void FallsBackWhenATranslationIsMissing()
        {
            LocalizationCatalog catalog = Catalog("en");

            Assert.Equal("Ses", catalog.Get("settings.sound"));
        }

        [Fact]
        public void FallingBackIsRecordedSoQaCanSeeTheGap()
        {
            LocalizationCatalog catalog = Catalog("en");
            catalog.Get("settings.sound");

            Assert.Contains("settings.sound", catalog.MissingKeys);
        }

        [Fact]
        public void HasReportsTheActiveLocaleOnlyNotTheFallback()
        {
            LocalizationCatalog catalog = Catalog("en");

            Assert.True(catalog.Has("scene.kitchen.title"));
            Assert.False(catalog.Has("settings.sound"));
        }

        [Fact]
        public void AnUnknownKeyReturnsTheKeyItself()
        {
            LocalizationCatalog catalog = Catalog("tr");

            // Never a blank label: a missing key has to be obvious in a screenshot.
            Assert.Equal("paywall.nonexistent", catalog.Get("paywall.nonexistent"));
            Assert.Contains("paywall.nonexistent", catalog.MissingKeys);
        }

        [Fact]
        public void PlaceholdersAreSubstituted()
        {
            LocalizationCatalog catalog = Catalog("en");

            string text = catalog.Get("subscription.renews", new Dictionary<string, string> { ["date"] = "12 May" });

            Assert.Equal("Renews on 12 May", text);
        }

        [Fact]
        public void AnUnsuppliedPlaceholderStaysVisible()
        {
            LocalizationCatalog catalog = Catalog("en");

            string text = catalog.Get("subscription.renews", new Dictionary<string, string> { ["wrong"] = "x" });

            Assert.Equal("Renews on {date}", text);
        }

        [Fact]
        public void VoiceClipsResolveUnderTheActiveLocale()
        {
            // This is the one that matters most: the audience cannot read, so voice-over
            // is what actually carries the language.
            Assert.Equal("audio/en/vo_dough.ogg", Catalog("en").VoicePath("vo_dough.ogg"));
            Assert.Equal("audio/tr/vo_dough.ogg", Catalog("tr").VoicePath("vo_dough.ogg"));
        }
    }
}
