using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using SoftwareLearningGuide.Api.FeatureToggles;

namespace SoftwareLearningGuide.Api.Controllers.DiagnosticsControllerExample;

/// <summary>
/// Controller de diagnóstico para inspeccionar feature toggles y configuración en runtime.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[FeatureGate(FeatureToggleNames.FT_ENABLE_DIAGNOSIS_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class DiagnosticsController : ControllerBase {
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IFeatureManagerSnapshot _featureManager;

    public DiagnosticsController(IConfiguration configuration, IFeatureManagerSnapshot featureManager) {
        _configurationRoot = (IConfigurationRoot)configuration;
        _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
    }

    /// <summary>
    /// GET api/v1/diagnostics/feature-toggles?name=customer
    /// Devuelve las feature toggles registradas, su valor actual y de qué provider proviene.
    /// El parámetro opcional "name" filtra por contiene.
    /// </summary>
    [HttpGet("feature-toggles")]
    [ProducesResponseType(typeof(IEnumerable<FeatureToggleInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureToggles([FromQuery] string? name = null) {
        var toggles = new List<FeatureToggleInfo>();

        foreach (var featureName in GetFeatureToggleNames()) {
            if (name is not null && !featureName.Contains(name, StringComparison.OrdinalIgnoreCase))
                continue;

            var enabled = await _featureManager.IsEnabledAsync(featureName);
            toggles.Add(new FeatureToggleInfo(featureName, enabled, GetProviderName($"FeatureManagement:{featureName}")));
        }

        return Ok(toggles.OrderBy(toggle => toggle.Name));
    }

    /// <summary>
    /// GET api/v1/diagnostics/configuration?key=FeatureManagement
    /// Devuelve la configuración con valor y de qué provider proviene.
    /// El parámetro opcional "key" filtra por contiene.
    /// </summary>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationEntryInfo>), StatusCodes.Status200OK)]
    public IActionResult GetConfiguration([FromQuery] string? key = null) {
        var entries = GetConfigurationEntries();

        if (key is not null)
            entries = entries.Where(entry => entry.Key.Contains(key, StringComparison.OrdinalIgnoreCase));

        return Ok(entries);
    }

    private static IEnumerable<string> GetFeatureToggleNames() {
        return typeof(FeatureToggleNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null)!);
    }

    private string? GetProviderName(string key) {
        string? providerName = null;

        foreach (var provider in _configurationRoot.Providers) {
            if (provider.TryGet(key, out _))
                providerName = provider.GetType().Name;
        }

        return providerName;
    }

    private IEnumerable<ConfigurationEntryInfo> GetConfigurationEntries() {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in _configurationRoot.Providers) {
            CollectKeys(provider, null, keys);
        }

        var entries = new List<ConfigurationEntryInfo>();
        foreach (var key in keys) {
            string? value = null;
            string? providerName = null;

            foreach (var provider in _configurationRoot.Providers) {
                if (provider.TryGet(key, out var providerValue)) {
                    value = providerValue;
                    providerName = provider.GetType().Name;
                }
            }

            entries.Add(new ConfigurationEntryInfo(key, value, providerName));
        }

        return entries.OrderBy(entry => entry.Key);
    }

    private static void CollectKeys(IConfigurationProvider provider, string? parentPath, HashSet<string> keys) {
        foreach (var key in provider.GetChildKeys(Enumerable.Empty<string>(), parentPath)) {
            var fullPath = parentPath is null ? key : $"{parentPath}:{key}";

            if (provider.GetChildKeys(Enumerable.Empty<string>(), fullPath).Any()) {
                CollectKeys(provider, fullPath, keys);
            }
            else {
                keys.Add(fullPath);
            }
        }
    }
}

public record FeatureToggleInfo(string Name, bool Enabled, string? Provider);

public record ConfigurationEntryInfo(string Key, string? Value, string? Provider);
