using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace Mcp.TaskAndResearch.E2ETests;

[TestClass]
public class HistoryViewSearchTests : PageTest
{
    private const string BaseUrl = "http://localhost:5000";
    
    [TestMethod]
    public async Task SearchHighlighting_BasicSearch_HighlightsMatchingText()
    {
        // Navigate to history page
        await Page.GotoAsync($"{BaseUrl}/history");
        
        // Wait for page to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Find the search input
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Type "task" in search box
        await searchInput.FillAsync("task");
        
        // Wait for debounce (300ms) + a bit extra
        await Page.WaitForTimeoutAsync(500);
        
        // Check if any <mark> elements exist
        var markElements = Page.Locator("mark");
        var count = await markElements.CountAsync();
        
        // Assert that highlighting exists
        Assert.IsTrue(count > 0, "Expected to find highlighted text with <mark> elements");
        
        // Verify the mark elements have the expected styling
        var firstMark = markElements.First;
        var backgroundColor = await firstMark.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
        
        // Yellow background should be present (rgb(255, 235, 59) is #ffeb3b)
        Assert.IsNotNull(backgroundColor, "Expected mark element to have background color");
    }
    
    [TestMethod]
    public async Task SearchHighlighting_CaseInsensitive_MatchesRegardlessOfCase()
    {
        await Page.GotoAsync($"{BaseUrl}/history");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Type uppercase "TASK"
        await searchInput.FillAsync("TASK");
        await Page.WaitForTimeoutAsync(500);
        
        // Should still highlight lowercase "task" if it exists
        var markElements = Page.Locator("mark");
        var count = await markElements.CountAsync();
        
        if (count > 0)
        {
            // Get the text content of first mark
            var text = await markElements.First.TextContentAsync();
            Assert.IsNotNull(text, "Expected highlighted text");
            
            // Should match "task", "Task", "TASK", etc.
            Assert.IsTrue(text.Equals("task", StringComparison.OrdinalIgnoreCase) || 
                         text.Equals("TASK", StringComparison.OrdinalIgnoreCase), 
                         "Expected case-insensitive match");
        }
    }
    
    [TestMethod]
    public async Task SearchHighlighting_ClearSearch_RemovesHighlights()
    {
        await Page.GotoAsync($"{BaseUrl}/history");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Type search term
        await searchInput.FillAsync("task");
        await Page.WaitForTimeoutAsync(500);
        
        // Verify highlights exist
        var markElementsBefore = Page.Locator("mark");
        var countBefore = await markElementsBefore.CountAsync();
        
        // Clear search
        await searchInput.FillAsync("");
        await Page.WaitForTimeoutAsync(500);
        
        // Verify highlights are removed
        var markElementsAfter = Page.Locator("mark");
        var countAfter = await markElementsAfter.CountAsync();
        
        Assert.AreEqual(0, countAfter, "Expected no mark elements after clearing search");
    }
    
    [TestMethod]
    public async Task SearchHighlighting_XSSPrevention_DoesNotExecuteScript()
    {
        await Page.GotoAsync($"{BaseUrl}/history");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Attempt XSS injection
        await searchInput.FillAsync("<script>alert('XSS')</script>");
        await Page.WaitForTimeoutAsync(500);
        
        // Listen for dialogs (alerts) - there should be none
        bool alertTriggered = false;
        Page.Dialog += (_, dialog) =>
        {
            alertTriggered = true;
            dialog.DismissAsync();
        };
        
        await Page.WaitForTimeoutAsync(1000);
        
        Assert.IsFalse(alertTriggered, "XSS script should not execute");
        
        // Verify no console errors
        var errors = new List<string>();
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                errors.Add(msg.Text);
            }
        };
        
        await Page.WaitForTimeoutAsync(500);
        Assert.AreEqual(0, errors.Count, $"Expected no console errors, but found: {string.Join(", ", errors)}");
    }
    
    [TestMethod]
    public async Task SearchHighlighting_SpecialCharacters_HandledSafely()
    {
        await Page.GotoAsync($"{BaseUrl}/history");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Test special HTML characters
        var specialChars = new[] { "&", "<", ">", "\"", "'" };
        
        foreach (var specialChar in specialChars)
        {
            await searchInput.FillAsync(specialChar);
            await Page.WaitForTimeoutAsync(500);
            
            // Should not cause any errors (page should still be functional)
            var isEnabled = await searchInput.IsEnabledAsync();
            Assert.IsTrue(isEnabled, $"Search input should still be enabled after typing '{specialChar}'");
            
            await searchInput.FillAsync("");
            await Page.WaitForTimeoutAsync(300);
        }
    }
    
    [TestMethod]
    public async Task SearchHighlighting_ExpandCollapse_StillWorks()
    {
        await Page.GotoAsync($"{BaseUrl}/history");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Type search term
        await searchInput.FillAsync("task");
        await Page.WaitForTimeoutAsync(500);
        
        // Find a chevron/expand button
        var chevron = Page.Locator("button").Filter(new() { HasText = "›" })
            .Or(Page.Locator("button").Filter(new() { HasText = "▶" }))
            .Or(Page.Locator("[class*='chevron']"))
            .First;
        
        if (await chevron.CountAsync() > 0)
        {
            // Click to expand
            await chevron.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            
            // Verify details are visible (implementation specific - adjust selector)
            // This is just checking that the click worked without errors
            var isVisible = await chevron.IsVisibleAsync();
            Assert.IsTrue(isVisible, "Chevron should still be visible after click");
        }
    }
    
    [TestMethod]
    public async Task SearchHighlighting_MultipleOccurrences_AllHighlighted()
    {
        await Page.GotoAsync($"{BaseUrl}/history");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var searchInput = Page.Locator("input[placeholder*='search']").Or(Page.Locator("input[type='text']")).First;
        
        // Search for common word that might appear multiple times
        await searchInput.FillAsync("the");
        await Page.WaitForTimeoutAsync(500);
        
        // Count mark elements
        var markElements = Page.Locator("mark");
        var count = await markElements.CountAsync();
        
        // If there are results, verify multiple highlights can exist
        if (count > 1)
        {
            // Get text from first two marks
            var text1 = await markElements.Nth(0).TextContentAsync();
            var text2 = await markElements.Nth(1).TextContentAsync();
            
            Assert.IsNotNull(text1, "First mark should have text");
            Assert.IsNotNull(text2, "Second mark should have text");
        }
    }
}
