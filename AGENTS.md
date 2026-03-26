# AGENTS.md

## Project
Build a cross-platform desktop calendar application for **Windows** and **macOS** using **C#**, **.NET**, and **Avalonia UI**.

## Product requirements
- Show a **13-month calendar**.
  - 13 months of 28 days each
  - Extra month **Sol** between June and July
  - **Year Day** outside the months at the end of the year
  - **Leap Day** outside the months in leap years in between Juen and Sol
- Support **month view**, **week view**, and **day view**.
- Allow AI assistants to create **all-day events** through CLI.
- Persist events to **disk locally** so data survives app restarts.
- Support **categories** for events.
- The user should not be able to make modifications to the events or categories directly, it should always be done through the AI assistants using the CLI
- Each category must have a **color**.
- Allow filtering visible events by **category**.
- Has year, day, and month progress bar with percentage

## Implementation guidance
- Use **Avalonia** for the UI.
- Use **C#** for all app logic.
- Prefer a simple, maintainable architecture such as **MVVM**.
- Start with local-only storage using a straightforward **JSON** format.
- Keep dependencies minimal unless they clearly reduce complexity.

## AI agent / CLI compatibility
- The app should be designed to work well with persistent AI agent assistants ("claws") and other automation tools.
- Provide a simple **command-line interface** for managing data without opening the GUI.
- It must be easy to **add, list, update, and remove events** from the command line.
- It must be easy to **add, list, update, and remove categories** from the command line.
- CLI commands should support scripting and agent use:
  - predictable command names
  - stable arguments and exit codes
  - machine-readable output such as **JSON**
  - clear error messages
- Store data in a local format that is easy for both the app and external tools to read and write safely.
- Keep GUI and CLI operations backed by the same core application logic to avoid behavior differences.
- The GUI should update automatically after AI applies changes

## Visual design / look and feel
- The app should have a clean, modern, minimal interface that is easy to read at a glance.
- The app should always be in **dark mode**, the visual style should feel **neon futuristic**:
  - deep dark backgrounds
  - soft glowing accents
  - vivid highlight colors used sparingly
  - subtle borders and layered panels
  - crisp typography with strong contrast
- Dark mode should take inspiration from futuristic dashboards and neon UI styles, similar to cyberpunk-inspired interfaces, while still remaining practical and readable for daily use.
- Avoid visual clutter, excessive animation, or decorative effects that reduce usability.
- Colors should be used consistently for:
  - categories
  - selection states
  - active view indicators
  - progress bars
- The interface should feel polished but still fast and functional, not gimmicky.
- There is rough layout guideline in RoughLayout.png
- The layout should have have scroll wheels if necessary in the categories board and selected day board as there may be too many categories and events to display in the boards. The scroll bar should be entirely contained within the board it is for and should not move the whole window
- The days should all be the same size
- The selected day should be indicated by a different coloring
- The event listing in each day should follow the guidelines in RoughLayout.png
- Do not include a title of the calendar
- There is no need to include a gregorian reference
- Each day does not need to have the day of the week, that should be obvious by the grid
- Ensure that the progres percentages line up with their bars

## Expected output from Codex
- A runnable Avalonia solution.
- Clear project structure.
- Basic README with build and run steps.
- Sensible default sample categories/colors if needed.
- The project must build into a runnable **Windows executable** from a Windows development environment.
- The codebase should be written and structured to remain compatible with **Windows and macOS**, even though validation will only be performed on Windows.

