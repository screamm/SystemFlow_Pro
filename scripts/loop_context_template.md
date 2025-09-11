# Claude Code Autonomous Loop Context Template

## Primary Objective
[DEFINE YOUR CLEAR, MEASURABLE GOAL HERE]

## Execution Strategy
- **Approach**: Systematic/Agile/Exploratory
- **Priority**: Speed/Quality/Completeness
- **Risk Tolerance**: Conservative/Moderate/Aggressive

## Technical Context
### Project Information
- **Name**: SystemFlow Pro
- **Type**: System Monitoring Application
- **Technologies**: C#, .NET, WPF
- **Architecture**: MVVM pattern

### Codebase Structure
```
/src
  /Core       - Business logic
  /UI         - User interface
  /Services   - System services
  /Models     - Data models
/tests        - Test suites
/docs         - Documentation
```

## Task Breakdown Strategy
Use this systematic approach:
1. **Analysis Phase** (First 20% of time)
   - /sc:analyze to understand current state
   - Identify improvement opportunities
   - Create task hierarchy

2. **Implementation Phase** (60% of time)
   - Execute tasks systematically
   - Use /sc:implement for features
   - Apply /sc:improve for optimizations

3. **Validation Phase** (Final 20% of time)
   - /sc:test to verify changes
   - /sc:reflect to assess completion
   - Document achievements

## Constraints & Guidelines
### Must Follow
- ✅ Maintain all existing functionality
- ✅ Run tests after significant changes
- ✅ Use TodoWrite for task tracking
- ✅ Save progress every 30 minutes with /sc:save
- ✅ Create backups before major refactoring

### Avoid
- ❌ Breaking changes to public APIs
- ❌ Modifying core business logic without tests
- ❌ Committing without proper testing
- ❌ Skipping validation steps

## Decision Framework
When encountering obstacles:
1. Try alternative approach (max 3 attempts)
2. Document blocker in progress log
3. Move to next priority task
4. Return to blocked task later with fresh perspective

## Progress Checkpoints
Execute these commands periodically:
- Every 30 min: `/sc:reflect --type session`
- Every hour: `/sc:save --checkpoint`
- After major changes: `/sc:test --quick`
- When stuck: `/sc:brainstorm` for alternatives

## Success Criteria
Goal is considered complete when:
- [ ] All defined tasks are completed
- [ ] Tests pass with >80% coverage
- [ ] No critical issues remain
- [ ] Documentation is updated
- [ ] Performance metrics meet targets

## Recovery Instructions
If loop gets stuck or confused:
1. Execute `/sc:reflect --type completion`
2. Review current TodoWrite list
3. Check progress.json in session directory
4. Reset with `/sc:load` if necessary
5. Continue from last checkpoint

## Example Goals

### Performance Optimization Goal
```
Goal: "Optimize application startup time to under 3 seconds"
Strategy: Measure → Profile → Optimize → Validate
Key Commands: /sc:analyze --focus performance, /sc:improve --type performance
```

### Test Coverage Goal
```
Goal: "Achieve 90% test coverage across all modules"
Strategy: Analyze gaps → Generate tests → Validate coverage
Key Commands: /sc:test --coverage, /sc:implement tests, /sc:analyze --focus quality
```

### Refactoring Goal
```
Goal: "Refactor codebase to follow SOLID principles"
Strategy: Identify violations → Refactor systematically → Validate
Key Commands: /sc:analyze --focus architecture, /sc:improve --type maintainability
```

## Advanced Loop Patterns

### Iterative Improvement Loop
```
while not goal_achieved:
    /sc:analyze current_state
    /sc:improve highest_priority_issue
    /sc:test --validate
    /sc:reflect --type task
    if blocked:
        /sc:brainstorm alternatives
```

### Parallel Task Execution
```
/sc:task create subtasks --parallel
Execute independent tasks concurrently
Synchronize at checkpoints
Merge results and validate
```

### Progressive Enhancement
```
Start with MVP implementation
Iterate with improvements
Add features incrementally
Validate at each stage
```

## Emergency Commands
If autonomous loop needs intervention:
- `Ctrl+C` - Graceful shutdown with report
- Check `claude_sessions/[timestamp]/` for logs
- Resume from `progress.json` checkpoint
- Review `execution.log` for debugging