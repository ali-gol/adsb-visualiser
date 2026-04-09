# 📡 ADSB Visualiser - ModernRadar - Air-Gapped ADS-B Virtual Radar

ModernRadar is a modern Virtual Radar application that tracks air traffic using RTL-SDR hardware and `dump1090`. It is designed to be **100% offline with zero external API dependencies**. 

The system resolves aircraft metadata, flight routes, and airline information in milliseconds via local SQLite databases (`BaseStation.sqb` and `StandingData.sqb`) using pure ADO.NET. It is built to operate at full capacity even in remote, off-grid locations without internet access.

---

## ✨ Key Features

* **100% Offline Enrichment:** Does not require external APIs (like AirLabs or FlightAware). All data resolution is handled strictly via local SQLite files.
* **Real-Time Tracking:** BaseStation data (TCP Port 30003) from `dump1090` is processed by a .NET background service and broadcasted instantly to the UI via SignalR.
* **Smart Hex Decoding:** Mathematically calculates the aircraft's registration country using bitmask operations on the 24-bit Mode-S Hex code, ensuring the correct flag is displayed even if the aircraft is not explicitly listed in the database.
* **Rich User Interface:** A modern UI built with Angular and Tailwind CSS, featuring an interactive aircraft photo carousel, airline logos, and dynamic country flags.
* **High Performance:** Built on the .NET 10 architecture utilizing `IMemoryCache` and pure ADO.NET (`Microsoft.Data.Sqlite`) queries for a minimal CPU/RAM footprint.

---

## 🛠️ Tech Stack

### Backend (.NET 10)
* **Framework:** ASP.NET Core 10 (Web API & Background Services)
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
* [Node.js and npm](https://nodejs.org/)
* An RTL-SDR dongle and a running `dump1090` service.
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

### 3. Starting the Backend
Open a terminal, navigate to the API directory, and run the project:
```bash
cd ModernRadar.Api
dotnet restore
dotnet run
```
*Once started, the background service will connect to the dump1090 TCP port and spin up the SignalR Hub.*

### 4. Starting the Frontend
Open a new terminal and navigate to the Angular directory:
```bash
cd ModernRadar.WebUI
npm install
npm start
```
Open your browser and navigate to `http://localhost:4200` to view the radar.

---

## 📂 Data Architecture (How it Works)

The system parses raw TCP lines (e.g., `MSG,3,111,111,4B8429,10000,38.5,27.1,,,`) and enriches them in the following order:

1. **Country Detection:** The Hex code (`4B8429`) is matched against the `CodeBlock` table in `StandingData.sqb` using bitmasking to mathematically determine the registration country (e.g., Turkey).
2. **Aircraft Metadata:** The Hex code is queried in `BaseStation.sqb` to fetch the Registration (e.g., `TC-AAI`), Aircraft Type (`B738`), and Owner (`Pegasus Airlines`).
3. **Full Model Name:** The short type (`B738`) is joined with tables in `StandingData.sqb` (`AircraftType`, `Model`, `Manufacturer`) to resolve the full commercial name: "Boeing 737-800".
4. **Route Details:** If the aircraft broadcasts a Callsign (e.g., `PGT2816`), `StandingData.sqb` resolves the origin (`ADB`) and destination (`ISL`) airports.

---

## 👨‍💻 Developer
**Ali (TB3ARY)** - Amateur Radio Operator & Software Developer.  
*Passionate about off-grid communications, RF technologies, and full-stack engineering.*