import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Aircraft, AircraftDetailsDto } from '../../services/radar-signalr.service';



@Component({
  selector: 'app-aircraft-details-sidebar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Sidebar Container -->
    <div 
      class="fixed top-0 right-0 h-full w-96 bg-slate-900/80 backdrop-blur-2xl border-l border-white/10 shadow-[-10px_0_30px_rgba(0,0,0,0.5)] transform transition-transform duration-300 ease-in-out z-50 flex flex-col"
      [class.translate-x-0]="isOpen"
      [class.translate-x-full]="!isOpen"
    >
      
      <!-- Close Button -->
      <button 
        (click)="onClose()" 
        class="absolute top-4 right-4 p-2 rounded-full bg-black/50 hover:bg-red-500/80 text-white transition-colors z-[60] shadow-lg"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>

      <!-- Content wrapper (scrollable) -->
      <div class="flex-1 overflow-y-auto custom-scrollbar" *ngIf="aircraft">
        
        <!-- Header Image Carousel Section -->
        <div class="relative w-full h-64 bg-slate-800 flex items-center justify-center overflow-hidden border-b border-white/5 group">
          <!-- Skeleton Loader -->
          <div *ngIf="isLoadingDetails" class="absolute inset-0 animate-pulse bg-slate-700"></div>
          
          <ng-container *ngIf="!isLoadingDetails && details?.imageUrls && details!.imageUrls.length > 0">
            <img 
              [src]="details!.imageUrls[currentImageIndex]" 
              alt="Aircraft Photo" 
              class="w-full h-full object-cover transition-opacity duration-500" 
            />
            
            <!-- Carousel Controls -->
            <button 
              *ngIf="details!.imageUrls.length > 1"
              (click)="prevImage()" 
              class="absolute left-2 top-1/2 -translate-y-1/2 p-2 rounded-full bg-black/30 backdrop-blur-md text-white border border-white/10 hover:bg-black/50 transition-all opacity-0 group-hover:opacity-100"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
            </button>
            <button 
              *ngIf="details!.imageUrls.length > 1"
              (click)="nextImage()" 
              class="absolute right-2 top-1/2 -translate-y-1/2 p-2 rounded-full bg-black/30 backdrop-blur-md text-white border border-white/10 hover:bg-black/50 transition-all opacity-0 group-hover:opacity-100"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/></svg>
            </button>
            
            <!-- Counter -->
            <div class="absolute bottom-2 right-2 px-2 py-1 rounded bg-black/40 backdrop-blur text-[10px] text-white/70 border border-white/5">
              {{currentImageIndex + 1}} / {{details!.imageUrls.length}}
            </div>
          </ng-container>
          
          <div *ngIf="!isLoadingDetails && (!details?.imageUrls || details!.imageUrls.length === 0)" class="text-slate-500 flex flex-col items-center">
            <svg class="w-12 h-12 mb-2 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
            <span>No Imagery Available</span>
          </div>
        </div>

        <!-- Identity Section -->
        <div class="p-6 pb-2 border-b border-white/5">
           <div class="flex items-center space-x-2 mb-1">
             <img *ngIf="details?.countryIsoCode" [src]="'https://flagcdn.com/24x18/' + details?.countryIsoCode + '.png'" class="w-5 h-auto rounded-sm shadow-sm" alt="Flag">
             <span class="text-xs font-semibold text-slate-500 tracking-widest uppercase">{{ details?.registrationCountry || 'International' }}</span>
           </div>
           <h2 class="text-4xl font-extrabold text-white tracking-tight leading-none mb-1">{{ aircraft.callsign || aircraft.hex }}</h2>
           <p class="text-xl font-medium text-cyan-400">{{ details?.registration || aircraft.registration || 'Tracking...' }}</p>
           <p class="text-sm text-slate-400 mt-3 leading-relaxed">
             <span class="text-slate-500 font-medium">Aircraft:</span> {{ details?.aircraftModelName || details?.modelName || aircraft.model || 'Unknown Model' }}
           </p>
        </div>

        <!-- Live Route Card -->
        <div class="p-6 pb-2 border-b border-white/5" *ngIf="details?.originIata || details?.destinationIata || details?.airlineName">
            <div class="flex items-center justify-between mb-4">
                <div class="flex items-center space-x-3" *ngIf="details?.airlineIata || details?.airlineLogo || details?.airlineName">
                    <img
                      #airlineLogo
                      *ngIf="details?.airlineIata"
                      [src]="'https://pics.avs.io/200/200/' + details!.airlineIata + '.png'"
                      (error)="airlineLogo.style.display='none'"
                      alt="Airline Logo"
                      class="w-8 h-8 rounded-md bg-white object-contain" />
                    <span class="text-sm font-bold text-cyan-300">{{ details?.airlineName || 'Unknown Airline' }}</span>
                </div>
            </div>
            <div class="bg-black/20 p-5 rounded-2xl border border-white/10 flex items-center justify-between relative shadow-inner">
                <!-- Origin -->
                <div class="flex flex-col text-left z-10 w-1/3">
                    <span class="text-3xl font-black tracking-tighter text-white drop-shadow-md">{{ details?.originIata || '?' }}</span>
                    <span class="text-xs text-slate-400 mt-1 truncate max-w-full" [title]="details?.originCity">{{ details?.originCity || 'Unknown Origin' }}</span>
                </div>
                
                <!-- Arrow/Plane -->
                <div class="absolute inset-0 flex items-center justify-center opacity-70">
                    <div class="w-full border-t-2 border-dashed border-cyan-500/30 absolute"></div>
                    <svg class="w-8 h-8 text-cyan-400 rotate-90 z-20 bg-slate-900/80 rounded-full p-1" fill="currentColor" viewBox="0 0 24 24"><path d="M21 16v-2l-8-5V3.5c0-.83-.67-1.5-1.5-1.5S10 2.67 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z"/></svg>
                </div>

                <!-- Destination -->
                <div class="flex flex-col text-right z-10 w-1/3">
                    <span class="text-3xl font-black tracking-tighter text-white drop-shadow-md">{{ details?.destinationIata || '?' }}</span>
                    <span class="text-xs text-slate-400 mt-1 truncate max-w-full" [title]="details?.destinationCity">{{ details?.destinationCity || 'Unknown Desitnation' }}</span>
                </div>
            </div>
        </div>

        <!-- Live Telemetry Grid -->
        <div class="p-6 grid grid-cols-2 gap-4">
            
            <div class="bg-white/5 p-4 rounded-xl border border-white/5">
               <span class="text-xs font-semibold text-slate-500 uppercase tracking-widest">Altitude</span>
               <div class="text-2xl font-bold text-white mt-1 leading-tight">{{ aircraft.altitude || 0 | number }} <span class="text-sm font-normal text-slate-400 ml-1">ft</span></div>
               <div class="text-[10px] text-slate-500 mt-1 flex items-center" *ngIf="aircraft.verticalSpeed">
                 <svg [class.rotate-180]="(aircraft.verticalSpeed || 0) < 0" class="w-2 h-2 mr-1 text-cyan-400" fill="currentColor" viewBox="0 0 24 24"><path d="M12 4l-8 8h16l-8-8z"/></svg>
                 {{ aircraft.verticalSpeed | number }} fpm
               </div>
            </div>

            <div class="bg-white/5 p-4 rounded-xl border border-white/5">
               <span class="text-xs font-semibold text-slate-500 uppercase tracking-widest">Ground Speed</span>
               <div class="text-2xl font-bold text-white mt-1 leading-tight">{{ aircraft.speed || 0 | number }} <span class="text-sm font-normal text-slate-400 ml-1">kts</span></div>
               <div class="text-[10px] text-slate-500 mt-1" *ngIf="aircraft.track">
                 HDG: {{ aircraft.track }}°
               </div>
            </div>

            <div class="bg-white/5 p-4 rounded-xl border border-white/5">
               <span class="text-xs font-semibold text-slate-500 uppercase tracking-widest">Latitude</span>
               <div class="text-lg font-bold text-white mt-1 tracking-tight">{{ aircraft.latitude | number:'1.4-4' }}</div>
            </div>

            <div class="bg-white/5 p-4 rounded-xl border border-white/5">
               <span class="text-xs font-semibold text-slate-500 uppercase tracking-widest">Longitude</span>
               <div class="text-lg font-bold text-white mt-1 tracking-tight">{{ aircraft.longitude | number:'1.4-4' }}</div>
            </div>

        </div>
        
        <div class="p-6 pt-0 space-y-4">
          <div class="bg-cyan-500/10 border border-cyan-500/20 rounded-xl p-4 flex items-center space-x-3" *ngIf="details?.scheduledDeparture">
            <svg class="w-5 h-5 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            <div>
              <p class="text-[10px] font-bold text-cyan-400 uppercase tracking-wider">Scheduled Departure</p>
              <p class="text-sm text-white font-medium">{{ details?.scheduledDeparture }}</p>
            </div>
          </div>
        </div>

      </div>
    </div>
  `
})
export class AircraftDetailsSidebarComponent implements OnChanges {
  @Input() aircraft: Aircraft | null = null;
  @Input() details: AircraftDetailsDto | null = null;
  @Input() isLoadingDetails: boolean = false;

  @Output() close = new EventEmitter<void>();

  currentImageIndex = 0;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['aircraft'] && changes['aircraft'].currentValue !== changes['aircraft'].previousValue) {
      this.currentImageIndex = 0;
    }
  }

  get isOpen(): boolean {
    return this.aircraft !== null;
  }

  nextImage(): void {
    if (this.details?.imageUrls && this.details.imageUrls.length > 0) {
      this.currentImageIndex = (this.currentImageIndex + 1) % this.details.imageUrls.length;
    }
  }

  prevImage(): void {
    if (this.details?.imageUrls && this.details.imageUrls.length > 0) {
      this.currentImageIndex = (this.currentImageIndex - 1 + this.details.imageUrls.length) % this.details.imageUrls.length;
    }
  }

  onClose(): void {
    this.close.emit();
  }
}
