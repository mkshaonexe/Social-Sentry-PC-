# Social Sentry Codebase Details

## Project Structure Overview

The Visual Studio Solution (`Social Sentry.sln`) consists of the following connected components:

### 1. Main Desktop Application (`Social Sentry/`)
- **Type**: WPF Application (.NET)
- **Purpose**: The core "Social Sentry" desktop interface.
- **Key Directories**:
  - `Views/`: XAML windows and user controls.
  - `ViewModels/`: Logic for views (MVVM pattern).
  - `Services/`: Background logic, screen time tracking, and data handling.
  - `Models/`: Data structures.
  - `extension/`: Browser extension files for enhanced web activity tracking.

### 2. Watchdog Service (`SocialSentry.Watchdog/`)
- **Type**: Console/Details Runner
- **Purpose**: Helper process to ensure the main application or specific monitoring tasks persist or recover.

### 3. Installer (`Installer/`)
- **Type**: WiX Toolset Project (implied by file structure)
- **Purpose**: Generates the MSI installer for deployment.
- **Script**: `build_installer.ps1` in the root automates this build.

### 4. Database / Backend (`mastersqlalinhere/`)
- **File**: `mastersqlalinhere.txt`
- **Purpose**: Contains SQL scripts and policy definitions for the Supabase backend. All backend changes are recorded here.

## Cleanup & Organization

The following files and folders are identified as temporary, build artifacts, or redundant and will be removed to maintain an ideal structure:

- **Logs & Temporary Output**:
  - `build.log`, `build_final.log`, `build_new.log`, `build_log.txt`
  - `build_output.txt`, `errors.txt`
  - `Social Sentry/build.log`
- **Temporary Directories**:
  - `Social Sentry/temp_build`
  - `Social Sentry/temp_verification_build`

## Status of Components

- **Social Sentry App**: Active, Main UI.
- **Browser Extension**: Helper component for web tracking.
- **Watchdog**: Active, process reliability.
- **Installer**: Active, used for releasing.
