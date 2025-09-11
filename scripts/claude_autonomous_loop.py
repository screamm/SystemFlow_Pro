#!/usr/bin/env python3
"""
Claude Code Autonomous Loop System
Enables continuous task execution with context persistence and goal tracking
"""

import subprocess
import time
import json
import os
from datetime import datetime, timedelta
from pathlib import Path
import sys
import threading
import queue

class ClaudeAutonomousLoop:
    def __init__(self, goal, context_file, duration_hours=5):
        """
        Initialize autonomous loop system
        
        Args:
            goal: Clear objective for Claude to achieve
            context_file: Path to context/instructions file
            duration_hours: How long to run (default 5 hours)
        """
        self.goal = goal
        self.context_file = Path(context_file)
        self.duration = timedelta(hours=duration_hours)
        self.start_time = datetime.now()
        self.end_time = self.start_time + self.duration
        
        # Session management
        self.session_dir = Path("claude_sessions") / datetime.now().strftime("%Y%m%d_%H%M%S")
        self.session_dir.mkdir(parents=True, exist_ok=True)
        
        # Progress tracking
        self.progress_file = self.session_dir / "progress.json"
        self.log_file = self.session_dir / "execution.log"
        
        # Task queue for systematic execution
        self.task_queue = queue.Queue()
        self.completed_tasks = []
        
    def create_task_plan(self):
        """Generate task breakdown from goal"""
        initial_prompt = f"""
/sc:task analyze "{self.goal}" --strategy systematic --breakdown

Generate a detailed task plan that can be executed autonomously.
Format as JSON with the following structure:
{{
    "phases": [
        {{
            "name": "Phase name",
            "tasks": [
                {{
                    "id": "task_id",
                    "description": "Task description",
                    "command": "Claude command to execute",
                    "validation": "How to verify completion",
                    "dependencies": []
                }}
            ]
        }}
    ]
}}
"""
        return self.execute_claude_command(initial_prompt)
    
    def execute_claude_command(self, command, timeout=300):
        """Execute a Claude Code command and capture output"""
        try:
            # Save command to temporary file
            cmd_file = self.session_dir / "current_command.txt"
            cmd_file.write_text(command)
            
            # Execute via Claude CLI
            result = subprocess.run(
                ["claude", "code", "--file", str(cmd_file)],
                capture_output=True,
                text=True,
                timeout=timeout
            )
            
            # Log execution
            self.log_execution(command, result.stdout, result.stderr)
            
            return result.stdout
            
        except subprocess.TimeoutExpired:
            self.log_execution(command, "", "TIMEOUT")
            return None
        except Exception as e:
            self.log_execution(command, "", str(e))
            return None
    
    def log_execution(self, command, output, error=""):
        """Log command execution details"""
        with open(self.log_file, 'a', encoding='utf-8') as f:
            f.write(f"\n{'='*80}\n")
            f.write(f"Time: {datetime.now().isoformat()}\n")
            f.write(f"Command: {command[:200]}...\n")
            f.write(f"Output: {output[:500]}...\n")
            if error:
                f.write(f"Error: {error}\n")
    
    def save_progress(self):
        """Save current progress to file"""
        progress = {
            "goal": self.goal,
            "start_time": self.start_time.isoformat(),
            "current_time": datetime.now().isoformat(),
            "completed_tasks": self.completed_tasks,
            "remaining_tasks": list(self.task_queue.queue),
            "completion_percentage": len(self.completed_tasks) / (len(self.completed_tasks) + self.task_queue.qsize()) * 100 if self.task_queue.qsize() > 0 else 100
        }
        
        with open(self.progress_file, 'w') as f:
            json.dump(progress, f, indent=2)
    
    def load_context(self):
        """Load context and previous session state if available"""
        context = ""
        
        # Load base context
        if self.context_file.exists():
            context = self.context_file.read_text()
        
        # Load previous progress if resuming
        if self.progress_file.exists():
            with open(self.progress_file, 'r') as f:
                previous = json.load(f)
                context += f"\n\nPrevious Progress:\n{json.dumps(previous, indent=2)}"
        
        return context
    
    def autonomous_loop(self):
        """Main autonomous execution loop"""
        print(f"🚀 Starting autonomous loop for: {self.goal}")
        print(f"⏰ Will run until: {self.end_time.strftime('%Y-%m-%d %H:%M:%S')}")
        
        # Load context and create initial plan
        context = self.load_context()
        
        # Initialize with goal and context
        init_command = f"""
/sc:load
{context}

PRIMARY GOAL: {self.goal}

You are running in autonomous mode for {self.duration.total_seconds()/3600:.1f} hours.
Work systematically toward the goal. Use TodoWrite to track progress.
After each task, evaluate if you're closer to the goal and adjust approach if needed.
"""
        self.execute_claude_command(init_command)
        
        # Main execution loop
        iteration = 0
        while datetime.now() < self.end_time:
            iteration += 1
            
            # Generate next action based on current state
            next_action = f"""
/sc:reflect --type task --analyze

Based on the goal "{self.goal}" and current progress, determine the next action.
Execute it and report results concisely.
If blocked, try alternative approaches.
"""
            
            result = self.execute_claude_command(next_action, timeout=600)
            
            if result:
                self.completed_tasks.append({
                    "iteration": iteration,
                    "time": datetime.now().isoformat(),
                    "result_summary": result[:200]
                })
            
            # Save progress every iteration
            self.save_progress()
            
            # Check for completion
            if self.check_goal_completion():
                print("✅ Goal appears to be completed!")
                break
            
            # Brief pause between iterations
            time.sleep(10)
            
            # Status update every 10 iterations
            if iteration % 10 == 0:
                elapsed = datetime.now() - self.start_time
                remaining = self.end_time - datetime.now()
                print(f"📊 Iteration {iteration} | Elapsed: {elapsed} | Remaining: {remaining}")
        
        print(f"🏁 Loop completed after {iteration} iterations")
        self.generate_final_report()
    
    def check_goal_completion(self):
        """Check if the goal has been achieved"""
        check_command = f"""
/sc:reflect --type completion

Evaluate if the goal "{self.goal}" has been achieved.
Respond with JSON: {{"completed": true/false, "reason": "explanation"}}
"""
        result = self.execute_claude_command(check_command, timeout=60)
        
        try:
            if result:
                data = json.loads(result)
                return data.get("completed", False)
        except:
            pass
        
        return False
    
    def generate_final_report(self):
        """Generate comprehensive final report"""
        report_command = f"""
/sc:save --summarize

Generate a final report for the autonomous session:
- Goal: {self.goal}
- Duration: {(datetime.now() - self.start_time).total_seconds()/3600:.1f} hours
- Tasks completed: {len(self.completed_tasks)}

Summarize what was accomplished and any remaining work.
"""
        
        report = self.execute_claude_command(report_command)
        
        report_file = self.session_dir / "final_report.md"
        report_file.write_text(f"""
# Autonomous Session Report

**Goal:** {self.goal}
**Duration:** {(datetime.now() - self.start_time).total_seconds()/3600:.1f} hours
**Tasks Completed:** {len(self.completed_tasks)}

## Summary
{report if report else "No summary available"}

## Session Details
- Start: {self.start_time}
- End: {datetime.now()}
- Iterations: {len(self.completed_tasks)}
- Session Directory: {self.session_dir}
""")
        
        print(f"📄 Final report saved to: {report_file}")


def main():
    """Main entry point for autonomous loop"""
    if len(sys.argv) < 2:
        print("""
Usage: python claude_autonomous_loop.py "YOUR GOAL" [hours] [context_file]

Examples:
  python claude_autonomous_loop.py "Refactor entire codebase for better performance" 5
  python claude_autonomous_loop.py "Build complete test suite with 90% coverage" 3 context.md
""")
        sys.exit(1)
    
    goal = sys.argv[1]
    duration = float(sys.argv[2]) if len(sys.argv) > 2 else 5
    context_file = sys.argv[3] if len(sys.argv) > 3 else "loop_context.md"
    
    # Create context file if it doesn't exist
    if not Path(context_file).exists():
        Path(context_file).write_text(f"""
# Autonomous Loop Context

## Project Information
- Project: SystemFlow Pro
- Type: System monitoring application
- Language: C#/.NET

## Guidelines
- Focus on systematic improvements
- Maintain code quality standards
- Run tests after changes
- Document significant decisions

## Constraints
- Do not modify core business logic without validation
- Preserve all existing functionality
- Maintain backward compatibility
""")
    
    # Initialize and run autonomous loop
    loop = ClaudeAutonomousLoop(goal, context_file, duration)
    
    try:
        loop.autonomous_loop()
    except KeyboardInterrupt:
        print("\n⚠️ Loop interrupted by user")
        loop.generate_final_report()
    except Exception as e:
        print(f"❌ Error in autonomous loop: {e}")
        loop.generate_final_report()


if __name__ == "__main__":
    main()