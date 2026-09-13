# DiskUsage

A fast, interactive C# WPF desktop application built on **.NET 8** to scan folders/drives, visualize disk space consumption, and explore directory hierarchies with drill-through navigation.

---

## Features

- **Asynchronous Disk Scanner**:
  - Non-blocking background directory traversal with live progress reporting.
  - Cancellation support to stop in-progress scans at any time.
  - Resilient error handling for `UnauthorizedAccessException`, system files, long paths, and junction/symlink cycles.
- **Hierarchical TreeView & Details Grid**:
  - **Left Pane**: Expandable directory hierarchy with folder size badges.
  - **Right Pane**: Detailed list of files and subdirectories sorted by size (largest first).
  - Columns: Item Type, Name, Size, % of Parent (with visual progress bar), File Count, Folder Count, and Date Modified.
- **Interactive Drill-Through Navigation**:
  - Double-click any folder row in the grid to drill into that subfolder level.
  - Navigation Toolbar: **Back**, **Forward**, and **Up One Level** buttons.
  - Interactive **Breadcrumb Bar**: Click any ancestor folder in the path to jump directly to it.
- **Search & Filter**:
  - Real-time search/filter bar to filter current items by name or file extension.
- **Context Menus**:
  - *Open in File Explorer*: Launch File Explorer focused on the selected file/folder.
  - *Copy Full Path*: Copy path to clipboard.
- **Unit Tested**:
  - Comprehensive xUnit test suite covering recursive size aggregation, byte formatting, view models, and navigation stacks.

---

## Architecture

- **Framework**: .NET 8.0 Windows Desktop (`net8.0-windows`)
- **Pattern**: MVVM (Model-View-ViewModel) using `CommunityToolkit.Mvvm`
- **Solution Layout**:
  - `DiskUsage/`
    - `Models/`: `FileSystemItem`, `ScanProgressReport`
    - `Services/`: `IDiskScannerService`, `DiskScannerService`
    - `ViewModels/`: `MainViewModel`, `FileSystemItemViewModel`, `BreadcrumbItemViewModel`
    - `Views/`: `MainWindow.xaml`, `MainWindow.xaml.cs`
    - `Converters/`: `FileSizeConverter`, `PercentageToBrushConverter`, `ItemTypeToGlyphConverter`, etc.
    - `Helpers/`: `ByteSizeFormatter`
  - `DiskUsage.Tests/`: Unit tests for scanner, calculations, and navigation logic.

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

### Build & Run
```powershell
# Build solution
dotnet build

# Run unit tests
dotnet test

# Run application
dotnet run --project DiskUsage/DiskUsage.csproj
```
