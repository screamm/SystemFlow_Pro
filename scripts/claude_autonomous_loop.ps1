# Claude Code Autonomous Loop System - PowerShell Version
# Enables continuous task execution with context persistence

param(
    [Parameter(Mandatory=$true)]
    [string]$Goal,
    
    [Parameter()]
    [double]$Hours = 5,
    
    [Parameter()]
    [string]$ContextFile = "loop_context.md"
)

# Configuration
$StartTime = Get-Date
$EndTime = $StartTime.AddHours($Hours)
$SessionDir = "claude_sessions\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $SessionDir -Force | Out-Null

$ProgressFile = "$SessionDir\progress.json"
$LogFile = "$SessionDir\execution.log"
$CompletedTasks = @()

# Create context file if missing
if (-not (Test-Path $ContextFile)) {
    @"
# Autonomous Loop Context

## Project Information
- Working Directory: $(Get-Location)
- Goal: $Goal
- Duration: $Hours hours

## Guidelines
- Work systematically toward the goal
- Use TodoWrite to track progress
- Save progress periodically
- Validate changes before proceeding
"@ | Out-File -FilePath $ContextFile -Encoding UTF8
}

function Execute-ClaudeCommand {
    param(
        [string]$Command,
        [int]$TimeoutSeconds = 300
    )
    
    $TempFile = "$SessionDir\current_command.txt"
    $Command | Out-File -FilePath $TempFile -Encoding UTF8
    
    try {
        # Execute Claude command
        $Process = Start-Process -FilePath "claude" `
            -ArgumentList "code", "--file", $TempFile `
            -NoNewWindow -PassThru -RedirectStandardOutput "$SessionDir\output.txt" `
            -RedirectStandardError "$SessionDir\error.txt"
        
        # Wait with timeout
        $Process | Wait-Process -Timeout $TimeoutSeconds -ErrorAction Stop
        
        $Output = Get-Content "$SessionDir\output.txt" -Raw
        Log-Execution $Command $Output
        return $Output
    }
    catch {
        Log-Execution $Command "" "Timeout or error: $_"
        return $null
    }
}

function Log-Execution {
    param(
        [string]$Command,
        [string]$Output,
        [string]$Error = ""
    )
    
    $LogEntry = @"
$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Command: $($Command.Substring(0, [Math]::Min(200, $Command.Length)))
Output: $($Output.Substring(0, [Math]::Min(500, $Output.Length)))
$(if ($Error) { "Error: $Error" })
$('='*80)
"@
    
    Add-Content -Path $LogFile -Value $LogEntry
}

function Save-Progress {
    $Progress = @{
        goal = $Goal
        startTime = $StartTime.ToString("o")
        currentTime = (Get-Date).ToString("o")
        completedTasks = $CompletedTasks
        completionPercentage = if ($CompletedTasks.Count -gt 0) { 
            ($CompletedTasks.Count / ($CompletedTasks.Count + 10)) * 100 
        } else { 0 }
    }
    
    $Progress | ConvertTo-Json -Depth 10 | Out-File -FilePath $ProgressFile -Encoding UTF8
}

function Check-GoalCompletion {
    $CheckCommand = @"
/sc:reflect --type completion

Evaluate if the goal "$Goal" has been achieved.
Respond with YES if completed, NO if not.
"@
    
    $Result = Execute-ClaudeCommand -Command $CheckCommand -TimeoutSeconds 60
    return $Result -match "YES"
}

# Main Loop
Write-Host "🚀 Starting autonomous loop for: $Goal" -ForegroundColor Green
Write-Host "⏰ Will run until: $($EndTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Yellow

# Initialize Claude with context
$Context = Get-Content $ContextFile -Raw
$InitCommand = @"
/sc:load

$Context

PRIMARY GOAL: $Goal

You are running in autonomous mode for $Hours hours.
Work systematically toward the goal using these strategies:
1. Break down the goal into concrete tasks
2. Execute tasks one by one
3. Validate each step before proceeding
4. Adjust approach if blocked
5. Use /sc:reflect periodically to assess progress
"@

Execute-ClaudeCommand -Command $InitCommand

# Main execution loop
$Iteration = 0
while ((Get-Date) -lt $EndTime) {
    $Iteration++
    
    # Generate next action
    $NextAction = @"
Based on the goal "$Goal", determine and execute the next action.
Use appropriate /sc: commands to make progress.
If you encounter errors, try alternative approaches.
Report what you accomplished concisely.
"@
    
    $Result = Execute-ClaudeCommand -Command $NextAction -TimeoutSeconds 600
    
    if ($Result) {
        $CompletedTasks += @{
            iteration = $Iteration
            time = (Get-Date).ToString("o")
            summary = $Result.Substring(0, [Math]::Min(200, $Result.Length))
        }
    }
    
    # Save progress
    Save-Progress
    
    # Check completion
    if (Check-GoalCompletion) {
        Write-Host "✅ Goal completed!" -ForegroundColor Green
        break
    }
    
    # Status update
    if ($Iteration % 10 -eq 0) {
        $Elapsed = (Get-Date) - $StartTime
        $Remaining = $EndTime - (Get-Date)
        Write-Host "📊 Iteration $Iteration | Elapsed: $Elapsed | Remaining: $Remaining" -ForegroundColor Cyan
    }
    
    # Brief pause
    Start-Sleep -Seconds 10
}

# Generate final report
$FinalReport = @"
# Autonomous Session Report

**Goal:** $Goal
**Duration:** $((Get-Date - $StartTime).TotalHours.ToString("F1")) hours
**Iterations:** $Iteration
**Tasks Completed:** $($CompletedTasks.Count)

## Session Details
- Start: $StartTime
- End: $(Get-Date)
- Session Directory: $SessionDir
"@

$FinalReport | Out-File -FilePath "$SessionDir\final_report.md" -Encoding UTF8
Write-Host "📄 Session complete! Report saved to: $SessionDir\final_report.md" -ForegroundColor Green