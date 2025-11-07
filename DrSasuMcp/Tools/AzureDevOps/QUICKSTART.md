# Azure DevOps PR Review Tool - Quick Start Guide

Get up and running with Azure DevOps PR reviews in 5 minutes!

## Step 1: Setup Personal Access Token (PAT)

### Create PAT in Azure DevOps
1. Go to https://dev.azure.com
2. Click your profile icon → **Personal Access Tokens**
3. Click **New Token**
4. Configure:
   - **Name**: DrSasuMcp PR Review
   - **Organization**: Your organization
   - **Expiration**: 90 days (or your preference)
   - **Scopes**:
     - ✅ Code (Read)
     - ✅ Pull Request Threads (Read)
5. Click **Create**
6. **Copy the token** (you won't see it again!)

### Set Environment Variable

**Windows (PowerShell):**
```powershell
$env:AZURE_DEVOPS_PAT="your_token_here"

# To make it permanent:
[System.Environment]::SetEnvironmentVariable('AZURE_DEVOPS_PAT', 'your_token_here', 'User')
```

**Windows (Command Prompt):**
```cmd
set AZURE_DEVOPS_PAT=your_token_here
```

**Linux/Mac:**
```bash
export AZURE_DEVOPS_PAT="your_token_here"

# To make it permanent, add to ~/.bashrc or ~/.zshrc:
echo 'export AZURE_DEVOPS_PAT="your_token_here"' >> ~/.bashrc
```

## Step 2: Test Connection

Test that your PAT is working:

```
AI: "Test my Azure DevOps connection"
```

Expected result:
```json
{
  "success": true,
  "data": {
    "connected": true,
    "message": "Successfully connected to Azure DevOps"
  }
}
```

## Step 3: Your First PR Review

### Get a PR URL
1. Open any Pull Request in Azure DevOps
2. Copy the URL (it should look like):
   ```
   https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
   ```

### Review the PR

**Simple Review:**
```
AI: "Review this PR: https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123"
```

**Security-Focused Review:**
```
AI: "Check this PR for security issues: <PR_URL>"
```

**Show Critical Issues Only:**
```
AI: "Review PR <ID> and show only critical issues"
```

## Common Use Cases

### 1. Complete Code Review
```
User: "Do a full review of PR 456"

AI calls: ReviewPullRequest(
  prUrl: "...",
  includeAnalyzers: "security,quality,bestpractices",
  minIssueLevel: "info"
)

Result: Comprehensive review with all findings
```

### 2. Security Audit
```
User: "Security check on PR 789"

AI calls: ReviewPullRequest(
  prUrl: "...",
  includeAnalyzers: "security",
  minIssueLevel: "warning"
)

Result: Security issues with warning+ severity
```

### 3. View File Changes
```
User: "What changed in Program.cs?"

AI calls: GetPullRequestDiff(
  prUrl: "...",
  filePath: "src/Program.cs",
  diffFormat: "unified"
)

Result: Detailed diff of the file
```

### 4. PR Summary
```
User: "Tell me about PR 123"

AI calls: GetPullRequestInfo(
  prUrl: "..."
)

Result: PR title, author, status, file count
```

## Understanding Review Results

### Issue Levels

| Level | Icon | Meaning |
|-------|------|---------|
| **Critical** | 🔴 | Must fix before merge |
| **Warning** | 🟡 | Should address |
| **Info** | 🔵 | Suggestion for improvement |

### Overall Assessment

- **"Looks good to merge"** - No issues found
- **"Minor issues found"** - A few warnings
- **"Review recommended"** - Multiple warnings (5+)
- **"Requires changes"** - Critical issues present

### Sample Output

```
PR #123: Add user authentication
Status: Active | Author: Jane Doe | Files: 5

📊 Summary:
  + 234 additions | - 67 deletions
  🔴 2 Critical | 🟡 5 Warnings | 🔵 8 Suggestions

📄 src/Services/AuthService.cs (Modified: +89/-12)
  🔴 Line 45: [SEC001] Potential hardcoded password
     Suggestion: Use environment variables or Key Vault
     
  🟡 Line 78: [QUAL005] Method too long (65 lines)
     Suggestion: Break into smaller methods

Overall: ⚠️ Requires changes before merge
```

## Tips & Best Practices

### 1. Start with Security
Always run security analysis first for PRs touching auth, data access, or external APIs:
```
AI: "Security review of PR <ID>"
```

### 2. Filter by Severity
For large PRs, focus on critical issues first:
```
AI: "Review PR <ID> - show only critical issues"
```

### 3. Check Specific Files
Review complex files individually:
```
AI: "Show me the diff and review for AuthService.cs in PR <ID>"
```

### 4. Regular Reviews
Make it a habit to review PRs before approval:
- Security check before code review
- Quality check before merging
- Best practices for junior developer PRs

## Troubleshooting

### "Authentication failed"
**Problem:** PAT not set or expired  
**Solution:** 
```powershell
# Check if PAT is set
echo $env:AZURE_DEVOPS_PAT

# Set it if missing
$env:AZURE_DEVOPS_PAT="your_token_here"
```

### "Invalid PR URL"
**Problem:** Incorrect URL format  
**Solution:** URL must be:
```
https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}
```

### "Failed to connect"
**Problem:** Network or firewall issue  
**Solution:**
- Check internet connection
- Verify Azure DevOps is accessible
- Check corporate proxy/firewall settings

### "No issues found but I see problems"
**Problem:** File type not supported  
**Solution:** Check supported file types (.cs, .js, .ts, .java, .py, .go, etc.)

## Configuration Options

### Set Default Organization
```bash
export AZURE_DEVOPS_ORG="myorganization"
```

### Increase File Limit
```bash
export AZURE_DEVOPS_MAX_FILES=200
```

### Adjust Timeout
```bash
export AZURE_DEVOPS_TIMEOUT=120
```

## Example Conversations

### Example 1: New PR
```
User: "I just created PR 456 for the login feature. Can you review it?"

AI: Calls ReviewPullRequest(...)

AI Response: "I reviewed PR 456 'Add login feature'. Found:
- 2 critical security issues (hardcoded credentials)
- 3 warnings (method complexity, missing error handling)
- 5 suggestions (naming conventions, code style)

The hardcoded credentials in LoginService.cs line 23 must be fixed before merging.
I recommend using environment variables or Azure Key Vault."
```

### Example 2: Quick Check
```
User: "Quick check - any security issues in PR 789?"

AI: Calls ReviewPullRequest(includeAnalyzers="security", minIssueLevel="critical")

AI Response: "Security scan complete. PR 789 looks secure! No critical security 
issues found. Ready for code review."
```

### Example 3: Before Merge
```
User: "Before I merge PR 234, do a final review"

AI: Calls ReviewPullRequest(minIssueLevel="warning")

AI Response: "PR 234 'Fix user profile bug':
✅ No critical issues
⚠️ 2 warnings:
   - Missing null checks in ProfileService.cs
   - Empty catch block in UpdateProfile method

Recommend addressing the empty catch block before merging to ensure 
proper error handling."
```

## Next Steps

- 📖 Read the full [README.md](README.md) for detailed documentation
- 🔧 Customize analyzers for your team's standards
- 🤖 Integrate into your PR approval workflow
- 📊 Track common issues across multiple PRs

## Need Help?

- Check the [README.md](README.md) for detailed information
- Review the [Issue Codes Reference](README.md#issue-codes-reference)
- Refer to [Common Errors](README.md#error-handling)

Happy reviewing! 🚀

