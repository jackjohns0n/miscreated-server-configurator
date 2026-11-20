using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MiscreatedServerConfigurator.Core.Tests;

/// <summary>
/// WPF throws while a window opens when TextBox.Text, CheckBox.IsChecked, or another default-TwoWay
/// property is bound to a view-model member with no public setter. This environment cannot construct
/// a WPF window, so the check reads the XAML and the view-model source instead.
/// </summary>
public sealed class XamlBindingSetterTests
{
    private static readonly HashSet<string> TwoWayProperties = new(StringComparer.Ordinal)
    {
        "TextBox.Text",
        "CheckBox.IsChecked",
        "RadioButton.IsChecked",
        "ComboBox.Text",
        "ComboBox.SelectedItem",
        "ComboBox.SelectedValue",
        "ListBox.SelectedItem",
        "ListBox.SelectedValue",
        "Slider.Value",
        "ProgressBar.Value",
    };

    [Fact]
    public void Default_twoway_bindings_target_a_public_setter()
    {
        var root = FindRepoRoot();
        var appDir = Path.Combine(root, "src", "MiscreatedServerConfigurator.App");
        var properties = ScanProperties(appDir);
        var failures = new List<string>();
        foreach (var xamlPath in Directory.EnumerateFiles(appDir, "*.xaml", SearchOption.AllDirectories))
        {
            var document = XDocument.Load(xamlPath);
            var rootType = ResolveRootContext(xamlPath, document, appDir, properties);
            foreach (var element in document.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    var key = element.Name.LocalName + "." + attribute.Name.LocalName;
                    if (!TwoWayProperties.Contains(key) || !TryReadBinding(attribute.Value, out var path, out var mode))
                    {
                        continue;
                    }

                    if (mode is "OneWay" or "OneTime")
                    {
                        continue;
                    }

                    var candidates = Candidates(element, rootType, path, properties);
                    if (candidates.Count == 0)
                    {
                        continue;
                    }

                    var writable = candidates.Where(candidate => candidate.HasPublicSetter).ToList();
                    if (writable.Count > 0 && IsInsideDataTemplate(element))
                    {
                        continue;
                    }

                    if (writable.Count == candidates.Count)
                    {
                        continue;
                    }

                    var names = string.Join(", ", candidates.Select(candidate => candidate.DeclaringType + "." + candidate.Name));
                    failures.Add(Path.GetFileName(xamlPath) + " " + key + " {" + path + "} has no public setter (" + names + ").");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static List<BoundProperty> Candidates(XElement element, string? rootType, string path, IReadOnlyDictionary<string, List<BoundProperty>> properties)
    {
        if (!IsInsideDataTemplate(element))
        {
            return rootType is null ? new List<BoundProperty>() : Resolve(rootType, path, properties);
        }

        var matches = new List<BoundProperty>();
        foreach (var type in properties.Keys)
        {
            var resolved = Resolve(type, path, properties);
            if (resolved.Count == 1)
            {
                matches.Add(resolved[0]);
            }
        }

        return matches
            .GroupBy(candidate => candidate.DeclaringType + "." + candidate.Name)
            .Select(group => group.First())
            .ToList();
    }

    private static List<BoundProperty> Resolve(string typeName, string path, IReadOnlyDictionary<string, List<BoundProperty>> properties)
    {
        var parts = path.Split('.');
        var matches = new List<BoundProperty>();
        if (!properties.TryGetValue(typeName, out var current))
        {
            return matches;
        }

        BoundProperty? leaf = null;
        foreach (var part in parts)
        {
            leaf = current.FirstOrDefault(property => property.Name == part);
            if (leaf is null)
            {
                return new List<BoundProperty>();
            }

            var nextType = ElementType(leaf.TypeName);
            current = properties.TryGetValue(nextType, out var next) ? next : new List<BoundProperty>();
        }

        if (leaf is not null)
        {
            matches.Add(leaf);
        }

        return matches;
    }

    private static string? ResolveRootContext(
        string xamlPath,
        XDocument document,
        string appDir,
        IReadOnlyDictionary<string, List<BoundProperty>> properties)
    {
        var codeBehind = xamlPath + ".cs";
        if (File.Exists(codeBehind))
        {
            var fromCode = DataContextFromCode(File.ReadAllText(codeBehind));
            if (fromCode is not null)
            {
                return fromCode;
            }
        }

        var className = document.Root?.Attribute(XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml"))?.Value;
        var shortName = className?.Split('.').Last();
        if (shortName is null)
        {
            return null;
        }

        foreach (var other in Directory.EnumerateFiles(appDir, "*.xaml", SearchOption.AllDirectories))
        {
            var host = XDocument.Load(other);
            foreach (var usage in host.Descendants().Where(element => element.Name.LocalName == shortName))
            {
                var template = usage.Ancestors().FirstOrDefault(element => element.Name.LocalName == "DataTemplate");
                if (template is null)
                {
                    continue;
                }

                var content = template.Parent?.Parent;
                if (content is not null && content.Name.LocalName == "ContentControl")
                {
                    var contentBinding = content.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Content")?.Value;
                    if (TryReadBinding(contentBinding ?? string.Empty, out var contentPath, out _))
                    {
                        var hostType = ResolveRootContext(other, host, appDir, properties);
                        var resolved = hostType is null ? new List<BoundProperty>() : Resolve(hostType, contentPath, properties);
                        if (resolved.Count == 1)
                        {
                            return ElementType(resolved[0].TypeName);
                        }
                    }
                }

                var items = template.Ancestors().FirstOrDefault(element => element.Name.LocalName == "ItemsControl");
                var source = items?.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "ItemsSource")?.Value;
                if (source is not null && TryReadBinding(source, out var sourcePath, out _))
                {
                    var hostType = ResolveRootContext(other, host, appDir, properties);
                    var resolved = hostType is null ? new List<BoundProperty>() : Resolve(hostType, sourcePath, properties);
                    if (resolved.Count == 1)
                    {
                        return ElementType(resolved[0].TypeName);
                    }
                }
            }
        }

        return null;
    }

    private static string? DataContextFromCode(string code)
    {
        var assignment = Regex.Match(code, @"DataContext\s*=\s*(?:new\s+(?<type>[\w.]+)|(?<field>_\w+))");
        if (!assignment.Success)
        {
            return null;
        }

        if (assignment.Groups["type"].Success && assignment.Groups["type"].Value.Length > 0)
        {
            return assignment.Groups["type"].Value.Split('.').Last();
        }

        var field = assignment.Groups["field"].Value;
        var declared = Regex.Match(code, @"([\w.]+)\??\s+" + Regex.Escape(field) + @"\b");
        return declared.Success ? declared.Groups[1].Value.Split('.').Last() : null;
    }

    private static bool TryReadBinding(string markup, out string path, out string? mode)
    {
        path = string.Empty;
        mode = null;
        var match = Regex.Match(markup, @"^\{Binding\s*(?<body>.*)\}$");
        if (!match.Success)
        {
            return false;
        }

        var body = match.Groups["body"].Value.Trim();
        if (body.Contains("RelativeSource", StringComparison.Ordinal) || body.Contains("ElementName", StringComparison.Ordinal))
        {
            return false;
        }

        var modeMatch = Regex.Match(body, @"\bMode\s*=\s*(?<mode>\w+)");
        if (modeMatch.Success)
        {
            mode = modeMatch.Groups["mode"].Value;
            body = body.Remove(modeMatch.Index, modeMatch.Length);
        }

        body = Regex.Replace(body, @"\b[\w.]+\s*=\s*[^,}]+", string.Empty).Trim().Trim(',');
        var pathMatch = Regex.Match(body, @"^(?:Path\s*=\s*)?(?<path>[\w.]+)$");
        if (!pathMatch.Success)
        {
            return false;
        }

        path = pathMatch.Groups["path"].Value;
        return true;
    }

    private static bool IsInsideDataTemplate(XElement element)
    {
        return element.Ancestors().Any(ancestor => ancestor.Name.LocalName == "DataTemplate");
    }

    private static string ElementType(string typeName)
    {
        var generic = Regex.Match(typeName, @"<([\w.?]+)>$");
        var name = generic.Success ? generic.Groups[1].Value : typeName;
        var shortName = name.Split('.').Last().TrimEnd('?');
        return shortName;
    }

    private static IReadOnlyDictionary<string, List<BoundProperty>> ScanProperties(string appDir)
    {
        var result = new Dictionary<string, List<BoundProperty>>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(appDir, "*.cs", SearchOption.AllDirectories))
        {
            ScanFile(File.ReadAllText(file), result);
        }

        return result;
    }

    private static void ScanFile(string text, Dictionary<string, List<BoundProperty>> result)
    {
        var classes = Regex.Matches(text, @"\b(?:class|record)\s+(\w+)");
        foreach (Match property in Regex.Matches(text, @"public\s+(?:static\s+|readonly\s+|required\s+)*([\w.?<>]+)\s+(\w+)\s*(\{|=>)"))
        {
            var className = classes.Cast<Match>().LastOrDefault(match => match.Index < property.Index)?.Groups[1].Value;
            if (className is null)
            {
                continue;
            }

            var hasSetter = false;
            if (property.Groups[3].Value == "{")
            {
                var body = ReadBrace(text, property.Groups[3].Index);
                foreach (Match accessor in Regex.Matches(body, @"(?:(?<mod>private|protected|internal|public)\s+)?(?<kind>get|set|init)\b"))
                {
                    if (accessor.Groups["kind"].Value != "set")
                    {
                        continue;
                    }

                    var modifier = accessor.Groups["mod"].Value;
                    hasSetter = modifier.Length == 0 || modifier == "public";
                }
            }

            if (!result.TryGetValue(className, out var list))
            {
                list = new List<BoundProperty>();
                result.Add(className, list);
            }

            list.Add(new BoundProperty(className, property.Groups[2].Value, property.Groups[1].Value, hasSetter));
        }
    }

    private static string ReadBrace(string text, int openIndex)
    {
        var depth = 0;
        var inString = false;
        for (var i = openIndex; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                inString = !inString;
            }

            if (inString)
            {
                continue;
            }

            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text[openIndex..(i + 1)];
                }
            }
        }

        return string.Empty;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MiscreatedServerConfigurator.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not find MiscreatedServerConfigurator.sln above the test output.");
        }

        return dir.FullName;
    }

    private sealed record BoundProperty(string DeclaringType, string Name, string TypeName, bool HasPublicSetter);
}
