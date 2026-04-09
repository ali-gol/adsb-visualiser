import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID, ChangeDetectorRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RadarSignalrService, Aircraft, AircraftDetailsDto } from '../../services/radar-signalr.service';
import { AircraftDetailsSidebarComponent } from '../aircraft-details-sidebar/aircraft-details-sidebar.component';
import { Subscription } from 'rxjs';
import * as L from 'leaflet';

interface AircraftTrailState {
    marker: L.Marker;
    polyLine: L.Polyline;
    coordinates: L.LatLng[];
}

@Component({
    selector: 'app-radar-map',
    standalone: true,
    imports: [CommonModule, AircraftDetailsSidebarComponent],
    templateUrl: './radar-map.component.html',
    styleUrls: ['./radar-map.component.css']
})
export class RadarMapComponent implements OnInit, OnDestroy {
    private map!: L.Map;
    private sub!: Subscription;
    private aircraftState = new Map<string, AircraftTrailState>();

    public activeFlights: Aircraft[] = [];
    public activeCount = 0;

    public selectedFlight: Aircraft | null = null;
    public selectedDetails: AircraftDetailsDto | null = null;
    public isLoadingDetails = false;

    private isBrowser: boolean;

    // Custom Neon Icon
    private aircraftIcon = L.divIcon({
        className: 'custom-aircraft-icon',
        html: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="#00ffcc" class="w-6 h-6 drop-shadow-[0_0_8px_rgba(0,255,204,0.8)]"><path d="M3.1 11.2l9.9-5.1V2.5c0-.8.7-1.5 1.5-1.5s1.5.7 1.5 1.5v3.6l9.9 5.1c.5.3.8.8.8 1.4v1.5c0 .4-.4.6-.7.5l-10-3.3v6.3l2.8 2.1c.3.2.4.5.4.8v1.3c0 .3-.3.5-.6.4l-4.1-1.2-4.1 1.2c-.3.1-.6-.1-.6-.4v-1.3c0-.3.2-.6.4-.8l2.8-2.1v-6.3l-10 3.3c-.3.1-.7-.1-.7-.5v-1.5c0-.6.3-1.1.8-1.4z"/></svg>`,
        iconSize: [24, 24],
        iconAnchor: [12, 12]
    });

    constructor(
        private signalR: RadarSignalrService,
        private cdr: ChangeDetectorRef,
        @Inject(PLATFORM_ID) platformId: Object
    ) {
        this.isBrowser = isPlatformBrowser(platformId);
    }

    ngOnInit(): void {
        if (this.isBrowser) {
            this.initMap();
            this.signalR.startConnection();

            this.sub = this.signalR.aircraft$.subscribe(data => {
                this.activeFlights = data;
                this.activeCount = data.length;

                // Track realtime updates on the currently selected plane
                if (this.selectedFlight) {
                    this.selectedFlight = this.activeFlights.find(a => a.hex === this.selectedFlight!.hex) || null;
                    if (!this.selectedFlight) {
                        this.selectedDetails = null; // Lost tracking
                    }
                }

                this.updateMap(data);
                this.cdr.detectChanges(); // Force Angular to recognize SignalR updates
            });
        }
    }

    private initMap(): void {
        // Center on Aegean Region as requested
        this.map = L.map('radar-map', {
            zoomControl: false // clean UI
        }).setView([38.4, 27.1], 7);

        // Dark Matter CartoDB BaseMap
        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://carto.com/">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        }).addTo(this.map);
    }

    private updateMap(aircrafts: Aircraft[]): void {
        const currentHexes = new Set<string>();

        for (const ac of aircrafts) {
            if (!ac.latitude || !ac.longitude) continue;

            currentHexes.add(ac.hex);
            const pos = new L.LatLng(ac.latitude, ac.longitude);

            if (this.aircraftState.has(ac.hex)) {
                // Update existing
                const state = this.aircraftState.get(ac.hex)!;
                state.marker.setLatLng(pos);

                // Update popup content silently
                const content = this.buildTooltip(ac);
                state.marker.setPopupContent(content);

                // Update trail
                state.coordinates.push(pos);
                if (state.coordinates.length > 50) {
                    state.coordinates.shift(); // Bound memory limit to 50 nodes
                }
                state.polyLine.setLatLngs(state.coordinates);

            } else {
                // Create new
                const marker = L.marker(pos, { icon: this.aircraftIcon }).addTo(this.map);
                marker.bindPopup(this.buildTooltip(ac), { className: 'radar-tooltip' });

                marker.on('click', () => {
                    this.selectAircraft(ac.hex);
                });

                const polyLine = L.polyline([pos], {
                    color: '#00ffcc', // Neon Cyan
                    weight: 3,
                    opacity: 0.7,
                    className: 'neon-trail'
                }).addTo(this.map);

                this.aircraftState.set(ac.hex, {
                    marker,
                    polyLine,
                    coordinates: [pos]
                });
            }
        }

        // GC: Remove old aircraft that vanished from the signal
        this.aircraftState.forEach((state, hex) => {
            if (!currentHexes.has(hex)) {
                this.map.removeLayer(state.marker);
                this.map.removeLayer(state.polyLine);
                this.aircraftState.delete(hex);
            }
        });
    }

    private buildTooltip(ac: Aircraft): string {
        return `
      <div style="text-align:center;">
        <b>${ac.callsign || ac.hex}</b><br>
        <span style="color:#00ffcc;">${ac.altitude || 0} ft</span> | ${ac.speed || 0} kts<br>
        <small>${ac.registration || 'Tracking...'}</small>
      </div>
    `;
    }

    ngOnDestroy(): void {
        if (this.sub) this.sub.unsubscribe();
    }

    public async selectAircraft(hex: string): Promise<void> {
        this.selectedFlight = this.activeFlights.find(a => a.hex === hex) || null;
        if (!this.selectedFlight) return;

        this.isLoadingDetails = true;
        this.selectedDetails = null;
        this.cdr.detectChanges(); // Display skeleton

        this.selectedDetails = await this.signalR.getAircraftDetails(hex);
        this.isLoadingDetails = false;
        this.cdr.detectChanges(); // Render photo and data
    }

    public deselectAircraft(): void {
        this.selectedFlight = null;
        this.selectedDetails = null;
        this.cdr.detectChanges();
    }
}
