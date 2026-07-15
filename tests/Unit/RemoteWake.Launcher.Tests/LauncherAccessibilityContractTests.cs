using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RemoteWake.Launcher.Tests;

[TestClass]
public sealed partial class LauncherAccessibilityContractTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace Xaml =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [TestMethod]
    public void VisibleTextUsesResourcesOrBindingsAndEveryResourceExists()
    {
        var (window, styles, strings) = LoadResources();
        var allowedSymbols = new HashSet<string>(StringComparer.Ordinal) { "1", "2", "3", "4", "✓", "!" };
        var textAttributeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Text",
            "Content",
            "Header",
            "Title",
        };

        var literal = window.Root!
            .DescendantsAndSelf()
            .Attributes()
            .FirstOrDefault(attribute =>
                textAttributeNames.Contains(attribute.Name.LocalName) &&
                !string.IsNullOrWhiteSpace(attribute.Value) &&
                !attribute.Value.StartsWith('{') &&
                !allowedSymbols.Contains(attribute.Value));
        Assert.IsNull(
            literal,
            $"Visible text must use a resource or binding: {literal?.Name}={literal?.Value}");

        var definedKeys = styles.Root!.Elements()
            .Concat(strings.Root!.Elements())
            .Select(element => element.Attribute(Xaml + "Key")?.Value)
            .Where(key => key is not null)
            .ToHashSet(StringComparer.Ordinal);
        var referencedKeys = window.Root!
            .DescendantsAndSelf()
            .Attributes()
            .Select(attribute => DynamicResourcePattern().Match(attribute.Value))
            .Where(match => match.Success)
            .Select(match => match.Groups["key"].Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var key in referencedKeys)
        {
            Assert.IsTrue(definedKeys.Contains(key), $"Dynamic resource '{key}' is not defined.");
        }
    }

    [TestMethod]
    public void ButtonsAndScreenHeadingsExposeKeyboardAndAutomationContracts()
    {
        var (window, styles, _) = LoadResources();
        AssertButtonContracts(window);
        AssertHeadingContracts(window);
        AssertLiveRegionContracts(window);

        var keyboardFocusTriggers = styles.Descendants(Presentation + "Trigger")
            .Count(trigger => trigger.Attribute("Property")?.Value == "IsKeyboardFocused");
        Assert.IsGreaterThanOrEqualTo(2, keyboardFocusTriggers);
    }

    private static void AssertButtonContracts(XDocument window)
    {
        var buttons = window.Descendants(Presentation + "Button").ToArray();
        Assert.IsNotEmpty(buttons);

        foreach (var button in buttons)
        {
            var content = button.Attribute("Content")?.Value;
            var accessibleName = button.Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName.EndsWith(".Name", StringComparison.Ordinal))?.Value;
            Assert.IsTrue(
                content?.StartsWith("{DynamicResource ", StringComparison.Ordinal) == true ||
                accessibleName?.StartsWith("{DynamicResource ", StringComparison.Ordinal) == true,
                "Every button must derive an accessible name from a localized resource.");
            Assert.IsNotNull(
                button.Attributes().FirstOrDefault(
                    attribute => attribute.Name.LocalName.EndsWith(".TabIndex", StringComparison.Ordinal)),
                $"Button '{content}' must have an explicit tab order.");
        }
    }

    private static void AssertHeadingContracts(XDocument window)
    {
        foreach (var headingName in new[]
                 {
                     "DashboardHeading",
                     "ProgressHeading",
                     "SuccessHeading",
                     "FailureHeading",
                 })
        {
            var heading = window.Descendants()
                .Single(element => element.Attribute(Xaml + "Name")?.Value == headingName);
            Assert.AreEqual("True", heading.Attribute("Focusable")?.Value);
            Assert.AreEqual(
                "False",
                heading.Attributes().Single(
                    attribute => attribute.Name.LocalName.EndsWith(".IsTabStop", StringComparison.Ordinal)).Value);
            Assert.AreEqual(
                "Level1",
                heading.Attributes().Single(
                    attribute => attribute.Name.LocalName.EndsWith(".HeadingLevel", StringComparison.Ordinal)).Value);
        }
    }

    private static void AssertLiveRegionContracts(XDocument window)
    {
        var liveRegions = window.Descendants()
            .Attributes()
            .Where(attribute => attribute.Name.LocalName.EndsWith(".LiveSetting", StringComparison.Ordinal))
            .Select(attribute => attribute.Value)
            .ToArray();
        CollectionAssert.Contains(liveRegions, "Polite");
        CollectionAssert.Contains(liveRegions, "Assertive");
    }

    [TestMethod]
    public void CoreTextColorPairsMeetFourPointFiveToOneContrast()
    {
        var (_, styles, _) = LoadResources();
        var brushes = styles.Root!.Elements(Presentation + "SolidColorBrush")
            .ToDictionary(
                element => element.Attribute(Xaml + "Key")!.Value,
                element => element.Attribute("Color")!.Value,
                StringComparer.Ordinal);
        var pairs = new (string Foreground, string Background, string Label)[]
        {
            (brushes["PrimaryTextBrush"], brushes["WindowBackgroundBrush"], "primary/window"),
            (brushes["PrimaryTextBrush"], brushes["PanelBrush"], "primary/panel"),
            (brushes["SecondaryTextBrush"], brushes["WindowBackgroundBrush"], "secondary/window"),
            (brushes["SecondaryTextBrush"], brushes["PanelBrush"], "secondary/panel"),
            (brushes["HeaderTextBrush"], brushes["HeaderBackgroundBrush"], "header/header-background"),
            ("#C7D2DF", brushes["HeaderBackgroundBrush"], "header-subtitle/header-background"),
            ("#FFFFFF", brushes["AccentBrush"], "button/accent"),
            ("#FFFFFF", brushes["AccentHoverBrush"], "button/accent-hover"),
            (brushes["PrimaryTextBrush"], "#E5EBF1", "secondary-button/background"),
            (brushes["DemoBadgeTextBrush"], brushes["DemoBadgeBrush"], "demo-badge/background"),
            (brushes["WarningTextBrush"], brushes["WindowBackgroundBrush"], "warning/window"),
            ("#FFFFFF", brushes["SuccessBrush"], "success/icon"),
            ("#FFFFFF", brushes["ErrorBrush"], "error/icon"),
            (brushes["PrimaryTextBrush"], brushes["ActionBackgroundBrush"], "action/background"),
        };

        foreach (var pair in pairs)
        {
            var ratio = ContrastRatio(pair.Foreground, pair.Background);
            Assert.IsTrue(
                ratio >= 4.5,
                $"Contrast for {pair.Label} is {ratio:F2}:1 instead of at least 4.5:1.");
        }
    }

    private static (XDocument Window, XDocument Styles, XDocument Strings) LoadResources()
    {
        var root = FindRepositoryRoot();
        var launcher = Path.Combine(root, "src", "RemoteWake.Launcher.Wpf");
        return (
            XDocument.Load(Path.Combine(launcher, "MainWindow.xaml")),
            XDocument.Load(Path.Combine(launcher, "Resources", "Styles.xaml")),
            XDocument.Load(Path.Combine(launcher, "Resources", "Strings.pt-BR.xaml")));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RemoteWake.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static double ContrastRatio(string foreground, string background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string color)
    {
        var components = new[]
        {
            int.Parse(color.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
            int.Parse(color.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
            int.Parse(color.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
        };
        return 0.2126 * Linearize(components[0]) +
               0.7152 * Linearize(components[1]) +
               0.0722 * Linearize(components[2]);
    }

    private static double Linearize(double component) =>
        component <= 0.04045
            ? component / 12.92
            : Math.Pow((component + 0.055) / 1.055, 2.4);

    [GeneratedRegex("^\\{DynamicResource (?<key>[^}]+)\\}$", RegexOptions.CultureInvariant)]
    private static partial Regex DynamicResourcePattern();
}
