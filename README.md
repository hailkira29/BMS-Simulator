# BMS Simulator

A comprehensive Battery Management System (BMS) Simulator built with WPF and .NET 6. This application provides advanced battery monitoring, state estimation, and fault detection capabilities with real-time visualization and EIS (Electrochemical Impedance Spectroscopy) analysis.

![BMS Simulator](https://img.shields.io/badge/Framework-.NET%206-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## 🚀 Features

### Advanced Battery Modeling
- **Kalman Filter-based SoC Estimation**: Accurate State of Charge calculation using advanced filtering techniques
- **Real-time SoH Monitoring**: State of Health tracking with capacity fade analysis
- **Dynamic Load Profiles**: Multiple realistic usage scenarios (EV, Smartphone, Power Tools, etc.)
- **Temperature Compensation**: Thermal effects on battery performance
- **Internal Resistance Monitoring**: Real-time resistance tracking for health assessment

### EIS (Electrochemical Impedance Spectroscopy) Analysis
- **Impedance Spectrum Calculation**: Frequency-domain analysis of battery characteristics
- **Charge Transfer Resistance**: Monitoring of electrochemical reaction kinetics
- **Double Layer Capacitance**: Analysis of electrode-electrolyte interface
- **Warburg Impedance**: Diffusion-limited processes modeling

### Real-time Visualization
- **LiveCharts Integration**: Dynamic charts for SoC, voltage, temperature, and power
- **Multi-parameter Display**: Comprehensive dashboard with key battery metrics
- **Color-coded Status**: Visual indicators for battery health and operating conditions
- **Historical Data Tracking**: Time-series data with configurable time windows

### Fault Detection & Safety
- **Predictive Fault Detection**: Early warning system for battery issues
- **Overvoltage/Undervoltage Protection**: Voltage monitoring with configurable thresholds
- **Temperature Monitoring**: Hot/cold temperature alerts
- **SoH Degradation Alerts**: Health-based warnings and recommendations
- **EIS-based Diagnostics**: Advanced fault detection using impedance analysis

### Data Logging & Export
- **CSV Data Export**: Comprehensive data logging with timestamps
- **Real-world Data Integration**: Realistic temperature and current profiles
- **Performance Metrics**: Detailed battery performance tracking
- **Configurable Logging**: Customizable data export formats

## 🛠️ Technical Specifications

- **Framework**: .NET 6 with Windows targeting
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Charting Library**: LiveCharts.Wpf v0.9.7
- **Dependencies**: System.Windows.Forms, Microsoft.WindowsDesktop.App
- **OS Requirements**: Windows 10 or 11
- **Hardware**: Minimum 4GB RAM, Graphics support for WPF

## 📦 Installation

### Prerequisites
1. Windows 10 or 11
2. .NET 6 Runtime or SDK
3. Visual Studio 2022 (for development)

### Running the Application

#### From Release (Recommended)
1. Download the latest release from the [Releases](../../releases) page
2. Extract the ZIP file
3. Run `BMSSimulator.exe`

#### From Source
```bash
# Clone the repository
git clone https://github.com/yourusername/BMS_Simulator.git
cd BMS_Simulator

# Build and run
dotnet build
dotnet run
```

## 🎯 Quick Start

1. **Launch Application**: Start BMSSimulator.exe
2. **Configure Battery**:
   - Set battery capacity (Ah)
   - Set initial State of Charge (%)
   - Select load profile
3. **Start Simulation**: Click "Start Simulation"
4. **Monitor**: Watch real-time charts and status indicators
5. **Export Data**: Use "Export" to save simulation data

## 📊 Key Calculations

### State of Charge (Kalman Filter)
Uses advanced Kalman filtering for accurate SoC estimation combining:
- Coulomb counting
- Voltage measurements with noise compensation
- Temperature effects
- Battery aging factors

### State of Health
```
SoH = 100 - (Cycle Count × Degradation Rate)
```

### EIS Impedance
```
Z = Rs + Rct/(1 + jωRctCdl) + Zw
```
Where:
- `Rs`: Series resistance
- `Rct`: Charge transfer resistance  
- `Cdl`: Double layer capacitance
- `Zw`: Warburg impedance

## 📁 Project Structure

```
BMS_Simulator/
├── App.xaml                    # Application configuration
├── App.xaml.cs                 # Application entry point
├── MainWindow.xaml             # Main UI layout
├── MainWindow.xaml.cs          # Main application logic
├── BatteryModel.cs             # Advanced battery modeling with Kalman filter
├── RealWorldDataProvider.cs    # Real-world data simulation
├── BMSSimulator.csproj         # Project configuration
└── README.md                   # This file
```

## 🔧 Configuration

### Load Profiles
The simulator includes several predefined load profiles:
- **EV City Driving**: Electric vehicle urban driving pattern
- **EV Highway**: Highway driving with higher sustained loads
- **Smartphone/Tablet**: Mobile device usage patterns
- **Power Tool**: High-current intermittent loads
- **Drone Flight**: Aviation battery discharge patterns
- **Grid Storage**: Large-scale energy storage scenarios

### Fault Detection Thresholds
- **Low SoC**: Below 10%
- **Critical SoC**: Below 5%
- **Overvoltage**: Above 4.25V
- **Undervoltage**: Below 2.8V
- **High Temperature**: Above 50°C
- **Low Temperature**: Below -5°C
- **SoH Critical**: Below 80%
- **High Impedance**: Above 150mΩ

## 📈 Data Output

### CSV Export Format
```csv
Timestamp,Time_Elapsed(s),SoC(%),Voltage(V),Temperature(°C),Power(W),Current(A),SoH(%),Cycle_Count,EIS_Impedance(mΩ),Internal_Resistance(mΩ),Load_Profile
```

### Real-time Metrics
- State of Charge with estimation confidence
- Terminal voltage with noise modeling
- Temperature with thermal dynamics
- Power consumption and efficiency
- Internal resistance and aging indicators
- EIS parameters for advanced diagnostics

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- LiveCharts.Wpf for real-time charting capabilities
- .NET team for the robust WPF framework
- Battery research community for modeling insights

## 📞 Support

For questions, issues, or feature requests:
- Open an [issue](../../issues)
- Check the [documentation](../../wiki)
- Review [existing discussions](../../discussions)

---

**Made with ❤️ for the battery management and simulation community**
