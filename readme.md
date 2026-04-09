# 📡 ADSB Visualiser - ModernRadar - Air-Gapped ADS-B Virtual Radar

ModernRadar is a modern Virtual Radar application that tracks air traffic using RTL-SDR hardware and `dump1090`. It is designed to be **100% offline with zero external API dependencies**. 

The system resolves aircraft metadata, flight routes, and airline information in milliseconds via local SQLite databases (`BaseStation.sqb` and `StandingData.sqb`) using pure ADO.NET. It is built to operate at full capacity even in remote, off-grid locations without internet access.

---

## ✨ Key Features

* **100% Offline Enrichment:** Does not require external APIs (like AirLabs or FlightAware). All data resolution is handled strictly via local SQLite files.
* **Unified Full-Stack Host:** The Angular frontend is compiled directly into the .NET `wwwroot` folder. The application runs as a single process—no need for a separate Node.js frontend server.
* **Real-Time Tracking:** BaseStation data (TCP Port 30003) from `dump1090` is processed by a .NET background service and broadcasted instantly to the UI via SignalR.
* **Smart Hex Decoding:** Mathematically calculates the aircraft's registration country using bitmask operations on the 24-bit Mode-S Hex code, ensuring the correct flag is displayed even if the aircraft is not explicitly listed in the database.
* **Rich User Interface:** A modern UI built with Angular and Tailwind CSS, featuring an interactive aircraft photo carousel, airline logos, and dynamic country flags.
* **High Performance:** Built on the .NET 10 architecture utilizing `IMemoryCache` and pure ADO.NET (`Microsoft.Data.Sqlite`) queries for a minimal CPU/RAM footprint.

---

## 🛠️ Tech Stack

### Backend (.NET 10)
* **Framework:** ASP.NET Core 10 (Web API, Background Services & Static File Hosting)
* **Data Access:** Pure ADO.NET (`Microsoft.Data.Sqlite`) - *No ORMs (like Dapper or EF Core) are used, prioritizing raw speed.*
* **Real-Time Communication:** SignalR
* **TCP Client:** Custom Asynchronous TCP Socket Listener (Port 30003)

### Frontend (Angular)
* **Framework:** Angular 17+
* **Styling:** Tailwind CSS
* **Map Engine:** Leaflet.js / OpenStreetMap
* **Icons & Flags:** Flagpedia CDN, AVS Logos

### Hardware & Signal Processing
* RTL-SDR (Any RTL2832U based SDR receiver)
* `dump1090` (or `readsb` forks)

---

## 🚀 Installation & Setup

### 1. Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/)
* [Node.js and npm](https://nodejs.org/) (Only for building the frontend)
* An RTL-SDR dongle.
* **CRITICAL:** A background instance of `dump1090` (or a similar tool like `readsb`) must be running and actively broadcasting BaseStation formatted data on TCP Port 30003.
* Virtual Radar Server (VRS) database files: `BaseStation.sqb` and `StandingData.sqb`.

### 2. Database Preparation
Place the required `.sqb` files into a local directory on your machine (e.g., `C:\RadarDB\`).

Update the `appsettings.json` file in the Backend project with your specific file paths:

```json
{
  "ConnectionStrings": {
    "BaseStationDb": "Data Source=C:\\RadarDB\\BaseStation.sqb;Mode=ReadOnly",
    "StandingDataDb": "Data Source=C:\\RadarDB\\StandingData.sqb;Mode=ReadOnly"
  },
  "RadarSettings": {
    "Dump1090Host": "127.0.0.1",
    "Dump1090Port": 30003
  }
}
```

### 3. Build the Frontend (One-time Setup)
You don't have to build it manually. Once, host project is built, front-end will be built automatically.
The frontend app is configured to output its build files directly into the .NET project's `wwwroot` directory.
Open a terminal in the Angular directory and run:
```bash
cd ModernRadar.WebUI
npm install
npm run build
```

### 4. Running the Application
Since the frontend is now hosted by .NET, you only need to run the API project. Ensure your `dump1090` background service is running, then open a terminal in the API directory:
```bash
cd ../ModernRadar.Api
dotnet restore
dotnet run
```

By default, the application runs on port `5000`. Open your browser and navigate to:
👉 `http://localhost:5000`

*Optional Custom Port:* You can override the default port by passing the `--port` argument:
```bash
dotnet run --port 8080
```

---

## 📂 Data Architecture (How it Works)

The system parses raw TCP lines (e.g., `MSG,3,