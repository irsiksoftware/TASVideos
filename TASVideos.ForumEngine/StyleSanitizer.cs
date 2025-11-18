using System.Text;
using System.Text.RegularExpressions;

namespace TASVideos.ForumEngine;

/// <summary>
/// Provides comprehensive CSS style sanitization to prevent style injection attacks.
/// Uses a whitelist-based approach to only allow specific safe CSS properties and values.
/// </summary>
public static partial class StyleSanitizer
{
	/// <summary>
	/// Maximum number of CSS rules allowed per style attribute to prevent complexity attacks.
	/// </summary>
	private const int MaxRulesPerStyle = 5;

	/// <summary>
	/// Maximum length for a single CSS value to prevent buffer overflow attempts.
	/// </summary>
	private const int MaxValueLength = 100;

	/// <summary>
	/// Whitelist of safe CSS properties that can be used in forum posts.
	/// These properties have been carefully selected to allow styling without
	/// enabling position manipulation, overlays, or other attack vectors.
	/// </summary>
	private static readonly HashSet<string> SafeProperties =
	[
		// Text color properties
		"color",
		"background-color",

		// Font properties
		"font-size",
		"font-weight",
		"font-style",
		"font-family",
		"font-variant",

		// Text formatting
		"text-align",
		"text-decoration",
		"text-transform",
		"line-height",
		"letter-spacing",
		"word-spacing",

		// Spacing (with value limits enforced separately)
		"margin",
		"margin-top",
		"margin-right",
		"margin-bottom",
		"margin-left",
		"padding",
		"padding-top",
		"padding-right",
		"padding-bottom",
		"padding-left",

		// Border styling
		"border-color",
		"border-style",
		"border-width",

		// Display properties (limited set)
		"opacity",
		"visibility"
	];

	/// <summary>
	/// Patterns that indicate dangerous CSS constructs that must be blocked.
	/// These patterns are checked case-insensitively.
	/// </summary>
	private static readonly string[] DangerousPatterns =
	[
		// CSS expressions (IE-specific but dangerous)
		"expression",
		"expression(",

		// URL loading (can be used for data exfiltration or external resource loading)
		"url(",
		"url (",

		// Import statements
		"@import",
		"@charset",
		"@namespace",

		// JavaScript protocol
		"javascript:",
		"vbscript:",
		"data:",

		// Behavior (IE-specific)
		"behavior:",
		"-moz-binding",

		// Position manipulation (can be used for clickjacking)
		"position:",
		"z-index:",
		"position :",
		"z-index :",

		// Special characters that might break out of style context
		"<",
		">",
		"\"",
		"'",
		"\\",

		// CSS functions that could be dangerous
		"calc(",
		"var(",
		"attr("
	];

	// Compiled regex patterns for better performance
	[GeneratedRegex(@"^\s*(\d+(?:\.\d+)?)\s*(px|em|rem|%|pt|vh|vw)?\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex NumericValueRegex();

	[GeneratedRegex(@"^\s*#[0-9a-f]{3,6}\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex HexColorRegex();

	[GeneratedRegex(@"^\s*rgb\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex RgbColorRegex();

	[GeneratedRegex(@"^\s*rgba\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*[\d.]+\s*\)\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex RgbaColorRegex();

	[GeneratedRegex(@"^\s*[a-z]+\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex KeywordRegex();

	/// <summary>
	/// Valid CSS keywords for various properties.
	/// </summary>
	private static readonly HashSet<string> SafeKeywords =
	[
		// Color keywords (CSS named colors subset - common safe colors)
		"black", "white", "red", "green", "blue", "yellow", "orange", "purple",
		"gray", "grey", "silver", "navy", "aqua", "lime", "maroon", "olive",
		"teal", "fuchsia", "transparent",

		// Font weight
		"normal", "bold", "bolder", "lighter",

		// Font style
		"italic", "oblique",

		// Text decoration
		"none", "underline", "overline", "line-through",

		// Text transform
		"uppercase", "lowercase", "capitalize",

		// Text align
		"left", "right", "center", "justify",

		// Visibility
		"visible", "hidden",

		// Border style
		"solid", "dotted", "dashed", "double",

		// Display
		"inherit", "initial"
	];

	/// <summary>
	/// Sanitizes a complete CSS style string by validating and filtering all properties.
	/// </summary>
	/// <param name="style">The raw CSS style string to sanitize.</param>
	/// <returns>A sanitized CSS style string containing only safe properties and values, or empty string if all rules are invalid.</returns>
	public static string SanitizeStyle(string style)
	{
		if (string.IsNullOrWhiteSpace(style))
		{
			return string.Empty;
		}

		// Split on semicolon to get individual rules
		var rules = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		// Limit number of rules to prevent complexity attacks
		if (rules.Length > MaxRulesPerStyle)
		{
			rules = rules[..MaxRulesPerStyle];
		}

		var sanitizedRules = new List<string>();

		foreach (var rule in rules)
		{
			var sanitized = SanitizeRule(rule);
			if (!string.IsNullOrEmpty(sanitized))
			{
				sanitizedRules.Add(sanitized);
			}
		}

		return sanitizedRules.Count > 0 ? string.Join("; ", sanitizedRules) : string.Empty;
	}

	/// <summary>
	/// Sanitizes a single CSS property-value pair.
	/// </summary>
	/// <param name="property">The CSS property name (e.g., "color").</param>
	/// <param name="value">The CSS value (e.g., "red" or "#ff0000").</param>
	/// <returns>A sanitized CSS rule string, or empty string if invalid.</returns>
	public static string SanitizePropertyValue(string property, string value)
	{
		if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		property = property.Trim().ToLowerInvariant();
		value = value.Trim();

		// Check if property is in whitelist
		if (!SafeProperties.Contains(property))
		{
			return string.Empty;
		}

		// Sanitize the value
		var sanitizedValue = SanitizeValue(value, property);
		if (string.IsNullOrEmpty(sanitizedValue))
		{
			return string.Empty;
		}

		return $"{property}: {sanitizedValue}";
	}

	/// <summary>
	/// Sanitizes a single CSS rule (property: value).
	/// </summary>
	private static string SanitizeRule(string rule)
	{
		if (string.IsNullOrWhiteSpace(rule))
		{
			return string.Empty;
		}

		// Split on first colon to separate property and value
		var colonIndex = rule.IndexOf(':');
		if (colonIndex <= 0)
		{
			return string.Empty;
		}

		var property = rule[..colonIndex].Trim().ToLowerInvariant();
		var value = rule[(colonIndex + 1)..].Trim();

		return SanitizePropertyValue(property, value);
	}

	/// <summary>
	/// Sanitizes a CSS value, checking for dangerous patterns and validating format.
	/// </summary>
	private static string SanitizeValue(string value, string property)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		// Length check
		if (value.Length > MaxValueLength)
		{
			return string.Empty;
		}

		// Check for dangerous patterns (case-insensitive)
		var lowerValue = value.ToLowerInvariant();
		foreach (var pattern in DangerousPatterns)
		{
			if (lowerValue.Contains(pattern.ToLowerInvariant()))
			{
				return string.Empty;
			}
		}

		// Validate value format based on expected types
		if (IsValidValue(value, property))
		{
			return value;
		}

		return string.Empty;
	}

	/// <summary>
	/// Validates that a value matches expected formats for the given property.
	/// </summary>
	private static bool IsValidValue(string value, string property)
	{
		// Check if it's a safe keyword
		if (KeywordRegex().IsMatch(value) && SafeKeywords.Contains(value.ToLowerInvariant()))
		{
			return true;
		}

		// Check if it's a numeric value with optional unit
		if (NumericValueRegex().IsMatch(value))
		{
			// For spacing properties, enforce reasonable limits
			if (IsSpacingProperty(property))
			{
				return ValidateSpacingValue(value);
			}
			return true;
		}

		// Check color formats (for color-related properties)
		if (IsColorProperty(property))
		{
			return HexColorRegex().IsMatch(value)
				|| RgbColorRegex().IsMatch(value)
				|| RgbaColorRegex().IsMatch(value);
		}

		// For font-family, allow word characters, spaces, and commas
		if (property == "font-family")
		{
			// Simple validation: only allow letters, numbers, spaces, commas, and hyphens
			return value.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == ',' || c == '-' || c == '\'');
		}

		return false;
	}

	/// <summary>
	/// Checks if a property is a spacing-related property that needs value limits.
	/// </summary>
	private static bool IsSpacingProperty(string property)
	{
		return property.StartsWith("margin") || property.StartsWith("padding");
	}

	/// <summary>
	/// Checks if a property is color-related.
	/// </summary>
	private static bool IsColorProperty(string property)
	{
		return property.Contains("color");
	}

	/// <summary>
	/// Validates spacing values to prevent excessive margins/padding.
	/// </summary>
	private static bool ValidateSpacingValue(string value)
	{
		var match = NumericValueRegex().Match(value);
		if (!match.Success)
		{
			return false;
		}

		if (!double.TryParse(match.Groups[1].Value, out var numericValue))
		{
			return false;
		}

		var unit = match.Groups[2].Value.ToLowerInvariant();

		// Enforce reasonable limits based on unit
		return unit switch
		{
			"px" => numericValue <= 100,      // Max 100px
			"em" or "rem" => numericValue <= 10,  // Max 10em/rem
			"%" => numericValue <= 100,       // Max 100%
			"pt" => numericValue <= 75,       // Max 75pt
			"vh" or "vw" => numericValue <= 50,   // Max 50vh/vw
			"" => numericValue <= 100,        // If no unit, assume px and limit
			_ => false                         // Unknown unit, reject
		};
	}
}
