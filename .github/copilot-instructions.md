<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# BMS Simulator Project Instructions

This is a Battery Management System (BMS) Simulator built with WPF and .NET 6. The application simulates battery State of Charge (SoC) and State of Health (SoH) estimation using advanced battery management techniques with real-time visualization and fault detection.

## Project Structure
- **MainWindow.xaml**: UI layout with input fields, charts, and status displays
- **MainWindow.xaml.cs**: Core logic for SoC/SoH calculation, charting, and fault detection
- **App.xaml**: Application configuration
- **BMSSimulator.csproj**: Project file with LiveCharts.Wpf dependency

## Key Features
- Real-time SoC calculation based on battery capacity, initial SoC, and dynamic discharge current
- Real-time SoH (State of Health) calculation based on cycle count (a new cycle is counted every 10 seconds of simulation time)
- Dynamic current loads using randomization (50-150% of base current)
- Real-time charts displaying SoC and voltage trends using LiveCharts.Wpf
- Comprehensive fault detection system with alerts for low SoC, voltage anomalies, and battery health issues
- Data logging of SoC, SoH, voltage, cycle count, timestamp, and time elapsed to a CSV file
- Input validation for battery parameters
- Timer-based simulation with 1-second intervals
- Automatic simulation stop when battery is depleted
- Clean WPF UI with proper state management and live charts

## Development Guidelines
- Follow WPF best practices for UI design and data binding
- Use LiveCharts.Wpf for real-time data visualization
- Implement proper fault detection with user-friendly alerts
- Use proper input validation for all user inputs
- Implement proper error handling and user feedback
- Maintain clean separation between UI and business logic
- Use descriptive variable names related to battery terminology (capacity, SoC, current, etc.)
- Handle dynamic data updates efficiently for smooth chart performance

## Battery Calculation Notes
- SoC calculation uses Coulomb counting: `SoC = Initial SoC - (Current × Time / 3600) / Capacity × 100`
- SoH calculation: `SoH = 100 - (Cycle Count × 0.1)` (0.1% degradation per cycle)
- Simulated voltage calculation: `Voltage = 3.7 - (0.3 * (100 - SoC) / 100)`
- Dynamic current simulation: `Current = Base Current × (0.5 + Random[0,1])`
- Time is tracked in seconds and converted to hours for calculation
- Current is in Amperes (A), Capacity in Ampere-hours (Ah)
- SoC is displayed as percentage (%)
- SoH is displayed as percentage (%)

## Fault Detection Thresholds
- Low SoC Alert: Below 10%
- Overvoltage Alert: Above 4.2V
- Undervoltage Alert: Below 3.0V
- Critical SoH Alert: Below 80%
