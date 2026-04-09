import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Aircraft {
    hex: string;
    callsign: string | null;
    altitude: number | null;
    speed: number | null;
    latitude: number | null;
    longitude: number | null;
    track: number | null;
    verticalSpeed: number | null;
    lastSeen: string;
    registration: string | null;
    model: string | null;
}

export interface AircraftDetailsDto {
    hex: string;
    registration: string | null;
    modelName: string | null;
    aircraftModelName: string | null;
    manufacturerName: string | null;
    owner: string | null;
    registrationCountry: string | null;
    countryIsoCode: string | null;
    imageUrl: string | null;
    imageUrls: string[];
    country: string | null;
    originIata: string | null;
    destinationIata: string | null;
    originCity: string | null;
    destinationCity: string | null;
    airlineName: string | null;
    airlineIata: string | null;
    airlineLogo: string | null;
    track: number | null;
    verticalSpeed: number | null;
    countryName: string | null;
    countryCode: string | null;
    scheduledDeparture: string | null;
    estimatedDeparture: string | null;
}

@Injectable({
    providedIn: 'root'
})
export class RadarSignalrService {
    private hubConnection: HubConnection | undefined;

    private aircraftSubject = new BehaviorSubject<Aircraft[]>([]);
    public aircraft$: Observable<Aircraft[]> = this.aircraftSubject.asObservable();

    constructor() { }

    public startConnection(): void {
        this.hubConnection = new HubConnectionBuilder()
            .withUrl('/radarhub')  // Ensure .NET host binds here
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        this.hubConnection
            .start()
            .then(() => console.log('Radar SignalR connection successfully started.'))
            .catch(err => console.error('Error starting SignalR connection: ' + err));

        this.listenToRadarUpdates();

        this.hubConnection.onreconnecting(() => {
            console.warn('Reconnecting SignalR...');
            this.aircraftSubject.next([]); // Clear traces to avoid ghosts
        });
    }

    private listenToRadarUpdates(): void {
        if (!this.hubConnection) return;

        this.hubConnection.on('UpdateAircraftList', (aircrafts: Aircraft[]) => {
            this.aircraftSubject.next(aircrafts);
        });
    }

    public async getAircraftDetails(hex: string): Promise<AircraftDetailsDto | null> {
        if (!this.hubConnection) return null;
        try {
            return await this.hubConnection.invoke<AircraftDetailsDto>('GetAircraftDetails', hex);
        } catch (err) {
            console.error('Failed to get aircraft details', err);
            return null;
        }
    }
}
