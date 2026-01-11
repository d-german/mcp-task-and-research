# NuGet.org Setup Instructions

## Create NuGet.org Account and API Key

### Step 1: Create/Login to NuGet.org Account
1. Go to https://www.nuget.org/
2. Click "Sign in" in the top right
3. Sign in with your Microsoft account (or create one if needed)

### Step 2: Generate API Key
1. Once logged in, click your username in the top right
2. Select "API Keys" from the dropdown menu
3. Click "Create" button
4. Fill in the form:
   - **Key Name:** `Mcp.TaskAndResearch Publish`
   - **Glob Pattern:** `Mcp.TaskAndResearch`
   - **Select Scopes:** Check "Push new packages and package versions"
   - **Expiration:** Choose desired duration (365 days recommended)
5. Click "Create"
6. **IMPORTANT:** Copy the API key immediately - it will only be shown once!

### Step 3: Store API Key Securely
Store your API key in a secure location. You'll need it to run:
```bash
dotnet nuget push ./test-pack/Mcp.TaskAndResearch.1.0.0-beta1.nupkg --api-key YOUR_KEY_HERE --source https://api.nuget.org/v3/index.json
```

### Optional: Configure GitHub Actions Secret
If you want automated publishing:
1. Go to your GitHub repository settings
2. Navigate to Secrets and variables > Actions
3. Click "New repository secret"
4. Name: `NUGET_API_KEY`
5. Value: Paste your API key
6. Click "Add secret"

---

## What's Next?
After obtaining your API key, you're ready to:
- Publish pre-release version (Task 8)
- Or wait and publish stable 1.0.0 directly
